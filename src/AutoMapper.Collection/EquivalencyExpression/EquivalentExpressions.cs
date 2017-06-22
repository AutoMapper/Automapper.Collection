using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AutoMapper.Collection;
using AutoMapper.Mappers;

namespace AutoMapper.EquivalencyExpression
{
    public static class EquivalentExpressions
    {
        private static readonly
            IDictionary<IConfigurationProvider, ConcurrentDictionary<TypePair, IEquivalentExpression>>
            EquivalentExpressionDictionary =
                new Dictionary<IConfigurationProvider, ConcurrentDictionary<TypePair, IEquivalentExpression>>();

        private static ConcurrentDictionary<TypePair, IEquivalentExpression> _equalityComparisonCache = new ConcurrentDictionary<TypePair, IEquivalentExpression>();

        private static readonly IDictionary<IConfigurationProvider, IList<IGeneratePropertyMaps>> GeneratePropertyMapsDictionary = new Dictionary<IConfigurationProvider, IList<IGeneratePropertyMaps>>();
        private static IList<IGeneratePropertyMaps> _generatePropertyMapsCache = new List<IGeneratePropertyMaps>();

        public static void AddCollectionMappers(this IMapperConfigurationExpression cfg)
        {
            cfg.InsertBefore<ReadOnlyCollectionMapper>(
                new ObjectToEquivalencyExpressionByEquivalencyExistingMapper(),
                new EquivalentExpressionAddRemoveCollectionMapper());
        }

        private static void InsertBefore<TObjectMapper>(this IMapperConfigurationExpression cfg, params IConfigurationObjectMapper[] adds)
            where TObjectMapper : IObjectMapper
        {
            var mappers = cfg.Mappers;
            var targetMapper = mappers.FirstOrDefault(om => om is TObjectMapper);
            var index = targetMapper == null ? 0 : mappers.IndexOf(targetMapper);
            foreach (var mapper in adds.Reverse())
                mappers.Insert(index, mapper);
            cfg.Advanced.BeforeSeal(c =>
            {
                foreach (var configurationObjectMapper in adds)
                    configurationObjectMapper.ConfigurationProvider = c;

                EquivalentExpressionDictionary.Add(c, _equalityComparisonCache);
                _equalityComparisonCache = new ConcurrentDictionary<TypePair, IEquivalentExpression>();

                GeneratePropertyMapsDictionary.Add(c, _generatePropertyMapsCache);
                _generatePropertyMapsCache = new List<IGeneratePropertyMaps>();
            });
        }

        internal static TypeMap GetTypeMap(this IConfigurationObjectMapper mapper, Type sourceType, Type destinationType)
        {
            return mapper.ConfigurationProvider.ResolveTypeMap(sourceType, destinationType);
        }

        internal static IEquivalentExpression GetEquivalentExpression(this IConfigurationObjectMapper mapper, Type sourceType, Type destinationType)
        {
            var typeMap = mapper.GetTypeMap(sourceType, destinationType);
            return typeMap == null ? null : mapper.ConfigurationProvider.GetEquivalentExpression(typeMap);
        }

        internal static IEquivalentExpression GetEquivalentExpression(this IConfigurationObjectMapper mapper, TypeMap typeMap)
            => mapper.ConfigurationProvider.GetEquivalentExpression(typeMap);

        internal static IEquivalentExpression GetEquivalentExpression(this IConfigurationProvider configurationProvider, TypeMap typeMap)
        {
            return EquivalentExpressionDictionary[configurationProvider].GetOrAdd(typeMap.Types,
                tp =>
                    GeneratePropertyMapsDictionary[configurationProvider].Select(_ =>_.GeneratePropertyMaps(typeMap).CreateEquivalentExpression()).FirstOrDefault(_ => _ != null));
        }

        /// <summary>
        /// Make Comparison between <typeparamref name="TSource"/> and <typeparamref name="TDestination"/>
        /// </summary>
        /// <typeparam name="TSource">Compared type</typeparam>
        /// <typeparam name="TDestination">Type being compared to</typeparam>
        /// <param name="mappingExpression">Base Mapping Expression</param>
        /// <param name="EquivalentExpression">Equivalent Expression between <typeparamref name="TSource"/> and <typeparamref name="TDestination"/></param>
        /// <returns></returns>
        public static IMappingExpression<TSource, TDestination> EqualityComparison<TSource, TDestination>(this IMappingExpression<TSource, TDestination> mappingExpression, Expression<Func<TSource, TDestination, bool>> EquivalentExpression) 
            where TSource : class 
            where TDestination : class
        {
            var typePair = new TypePair(typeof(TSource), typeof(TDestination));
            _equalityComparisonCache.AddOrUpdate(typePair,
                new EquivalentExpressionProperty<TSource, TDestination>(EquivalentExpression),
                (type, old) => new EquivalentExpressionProperty<TSource, TDestination>(EquivalentExpression));
            return mappingExpression;
        }

        /// <summary>
        /// Make Comparison between <typeparamref name="TSource"/> and <typeparamref name="TDestination"/> based on the return object.
        /// </summary>
        /// <typeparam name="TSource">Compared type</typeparam>
        /// <typeparam name="TDestination">Type being compared to</typeparam>
        /// <param name="mappingExpression">Base Mapping Expression</param>
        /// <param name="sourceProperty">Source property that should be used for mapping. if property is object the property on the object is used for mapping.</param>
        /// <param name="destinationProperty">Destination property that should be used for mapping. if property is object the property on the object is used for mapping.</param>
        /// <returns></returns>
        public static IMappingExpression<TSource, TDestination> EqualityComparison<TSource, TDestination>(this IMappingExpression<TSource, TDestination> mappingExpression, Expression<Func<TSource, object>> sourceProperty, Expression<Func<TDestination, object>> destinationProperty) 
            where TSource : class 
            where TDestination : class
        {
            var typePair = new TypePair(typeof(TSource), typeof(TDestination));
            var expression = new EquivalentExpressionProperty<TSource, TDestination>(sourceProperty, destinationProperty);
            _equalityComparisonCache.AddOrUpdate(typePair,
                expression,
                (type, old) => expression);
            return mappingExpression;
        }

        public static void SetGeneratePropertyMaps<TGeneratePropertyMaps>(this IMapperConfigurationExpression cfg)
            where TGeneratePropertyMaps : IGeneratePropertyMaps, new()
        {
            cfg.SetGeneratePropertyMaps(new TGeneratePropertyMaps());
        }

        public static void SetGeneratePropertyMaps(this IMapperConfigurationExpression cfg, IGeneratePropertyMaps generatePropertyMaps)
        {
            _generatePropertyMapsCache.Add(generatePropertyMaps);
        }
        
        private static IEquivalentExpression CreateEquivalentExpression(this IEnumerable<PropertyMap> propertyMaps)
        {
            var properties = propertyMaps as IList<PropertyMap> ?? propertyMaps.ToList();
            if (!properties.Any() || properties.Any(pm => pm.DestinationProperty.GetMemberType() != pm.SourceMember.GetMemberType()))
                return null;
            var typeMap = properties.First().TypeMap;
            var srcType = typeMap.SourceType;
            var destType = typeMap.DestinationType;
            var srcExpr = Expression.Parameter(srcType, "src");
            var destExpr = Expression.Parameter(destType, "dest");

            var equalExpr = properties.Select(pm => SourceEqualsDestinationExpression(pm, srcExpr, destExpr)).ToList();
            if (!equalExpr.Any())
                return EquivalentExpression.BadValue;
            var finalExpression = equalExpr.Skip(1).Aggregate(equalExpr.First(), Expression.And);

            var expr = Expression.Lambda(finalExpression, srcExpr, destExpr);
            var genericExpressionType = typeof(EquivalentExpressionProperty<,>).MakeGenericType(srcType, destType);
            var equivilientExpression = Activator.CreateInstance(genericExpressionType, expr) as IEquivalentExpression;
            return equivilientExpression;
        }

        private static BinaryExpression SourceEqualsDestinationExpression(PropertyMap propertyMap, Expression srcExpr, Expression destExpr)
        {
            var srcPropExpr = Expression.Property(srcExpr, propertyMap.SourceMember as PropertyInfo);
            var destPropExpr = Expression.Property(destExpr, propertyMap.DestinationProperty as PropertyInfo);
            return Expression.Equal(srcPropExpr, destPropExpr);
        }
    }
}