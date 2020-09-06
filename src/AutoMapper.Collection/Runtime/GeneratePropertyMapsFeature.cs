using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AutoMapper.EquivalencyExpression;
using AutoMapper.Features;

namespace AutoMapper.Collection.Runtime
{
    public class GeneratePropertyMapsFeature : IRuntimeFeature
    {
        private readonly IList<IGeneratePropertyMaps> _generators;
        private readonly ConcurrentDictionary<TypePair, IEquivalentComparer> _comparers = new ConcurrentDictionary<TypePair, IEquivalentComparer>();

        public GeneratePropertyMapsFeature(List<IGeneratePropertyMaps> generators) => _generators = generators.AsReadOnly();

        public IEquivalentComparer Get(TypeMap typeMap)
        {
            return _comparers
                .GetOrAdd(typeMap.Types, _ =>
                    _generators
                    .Select(x => CreateEquivalentExpression(x.GeneratePropertyMaps(typeMap)))
                    .FirstOrDefault(x => x != null));
        }

        void IRuntimeFeature.Seal(IConfigurationProvider configurationProvider)
        {
        }

        private IEquivalentComparer CreateEquivalentExpression(IEnumerable<PropertyMap> propertyMaps)
        {
            if (!propertyMaps.Any() || propertyMaps.Any(pm => pm.DestinationMember.GetMemberType() != pm.SourceMember.GetMemberType()))
            {
                return null;
            }

            var typeMap = propertyMaps.First().TypeMap;
            var srcType = typeMap.SourceType;
            var destType = typeMap.DestinationType;
            var srcExpr = Expression.Parameter(srcType, "src");
            var destExpr = Expression.Parameter(destType, "dest");

            var equalExpr = propertyMaps.Select(pm => SourceEqualsDestinationExpression(pm, srcExpr, destExpr)).ToList();
            if (equalExpr.Count == 0) return EquivalentExpression.BadValue;

            var finalExpression = equalExpr.Skip(1).Aggregate(equalExpr[0], Expression.And);

            var expr = Expression.Lambda(finalExpression, srcExpr, destExpr);
            var genericExpressionType = typeof(EquivalentExpression<,>).MakeGenericType(srcType, destType);
            return Activator.CreateInstance(genericExpressionType, expr) as IEquivalentComparer;
        }

        private BinaryExpression SourceEqualsDestinationExpression(PropertyMap propertyMap, Expression srcExpr, Expression destExpr)
        {
            var srcPropExpr = Expression.Property(srcExpr, propertyMap.SourceMember as PropertyInfo);
            var destPropExpr = Expression.Property(destExpr, propertyMap.DestinationMember as PropertyInfo);
            return Expression.Equal(srcPropExpr, destPropExpr);
        }
    }
}
