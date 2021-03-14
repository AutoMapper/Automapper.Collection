using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AutoMapper.Collection;
using AutoMapper.EquivalencyExpression;
using static System.Linq.Expressions.Expression;

namespace AutoMapper.Mappers
{
    public class EquivalentExpressionAddRemoveCollectionMapper : EnumerableMapperBase, IConfigurationObjectMapper
    {
        private readonly CollectionMapper _collectionMapper = new CollectionMapper();

        public IConfigurationProvider ConfigurationProvider { get; set; }

        public static TDestination Map<TSource, TSourceItem, TDestination, TDestinationItem>(TSource source, TDestination destination, ResolutionContext context, IEquivalentComparer equivalentComparer, bool useSourceOrder)
            where TSource : IEnumerable<TSourceItem>
            where TDestination : ICollection<TDestinationItem>
        {
            if (source == null || destination == null)
            {
                return destination;
            }

            var destItemsByHash = destination.GroupBy(x => equivalentComparer.GetHashCode(x)).ToDictionary(x => x.Key, x => x.ToList());

            if (useSourceOrder)
            {
                destination.Clear();

                foreach (var srcItem in source)
                {
                    RetrieveCorrespondingItems(srcItem, out var dstItem, out var _);

                    dstItem = MapItem(srcItem, dstItem);

                    destination.Add(dstItem);
                }
            }
            else
            {
                foreach (var srcItem in source)
                {
                    RetrieveCorrespondingItems(srcItem, out var dstItem, out var itemExistsInDestination);

                    dstItem = MapItem(srcItem, dstItem);
                    if (!itemExistsInDestination)
                    {
                        destination.Add(dstItem);
                    }
                }

                foreach (var removedItem in destItemsByHash.SelectMany(x => x.Value))
                {
                    destination.Remove(removedItem);
                }
            }

            return destination;

            void RetrieveCorrespondingItems(TSourceItem srcItem, out TDestinationItem dstItem, out bool isFound)
            {
                var srcHash = equivalentComparer.GetHashCode(srcItem);
                dstItem = default;
                isFound = false;
                if (destItemsByHash.TryGetValue(srcHash, out var destCandidateItems))
                {
                    foreach (var item in destCandidateItems)
                    {
                        if (equivalentComparer.IsEquivalent(srcItem, item))
                        {
                            dstItem = item;
                            isFound = true;
                            destCandidateItems.Remove(item);
                            return;
                        }
                    }
                }
            }

            TDestinationItem MapItem(TSourceItem srcItem, TDestinationItem dstItem)
            {
                if (dstItem == null)
                {
                    dstItem = (TDestinationItem)context.Mapper.Map(srcItem, null, typeof(TSourceItem), typeof(TDestinationItem));
                }
                else
                {
                    context.Mapper.Map(srcItem, dstItem);
                }

                return dstItem;
            }
        }

        private static readonly MethodInfo _mapMethodInfo = typeof(EquivalentExpressionAddRemoveCollectionMapper).GetRuntimeMethods().Single(x => x.IsStatic && x.Name == nameof(Map));
        private static readonly ConcurrentDictionary<TypePair, IObjectMapper> _objectMapperCache = new ConcurrentDictionary<TypePair, IObjectMapper>();

        public override bool IsMatch(TypePair typePair)
        {
            return typePair.SourceType.IsEnumerableType()
                   && typePair.DestinationType.IsCollectionType();
        }

        public override Expression MapExpression(IConfigurationProvider configurationProvider, ProfileMap profileMap, IMemberMap memberMap,
            Expression sourceExpression, Expression destExpression, Expression contextExpression)
        {
            var sourceType = TypeHelper.GetElementType(sourceExpression.Type);
            var destType = TypeHelper.GetElementType(destExpression.Type);

            var equivalencyExpression = this.GetEquivalentExpression(sourceType, destType);
            if (equivalencyExpression == null)
            {
                var typePair = new TypePair(sourceExpression.Type, destExpression.Type);
                return _objectMapperCache.GetOrAdd(typePair, _ =>
                {
                    var mappers = new List<IObjectMapper>(configurationProvider.GetMappers());
                    for (var i = mappers.IndexOf(this) + 1; i < mappers.Count; i++)
                    {
                        var mapper = mappers[i];
                        if (mapper.IsMatch(typePair))
                        {
                            return mapper;
                        }
                    }
                    return _collectionMapper;
                })
                .MapExpression(configurationProvider, profileMap, memberMap, sourceExpression, destExpression, contextExpression);
            }

            var useSourceOrder = this.GetUseSourceOrder(sourceType, destType);

            var method = _mapMethodInfo.MakeGenericMethod(sourceExpression.Type, sourceType, destExpression.Type, destType);
            var map = Call(null, method, sourceExpression, destExpression, contextExpression, Constant(equivalencyExpression), Constant(useSourceOrder));

            var notNull = NotEqual(destExpression, Constant(null));
            var collectionMapperExpression = _collectionMapper.MapExpression(configurationProvider, profileMap, memberMap, sourceExpression, destExpression, contextExpression);
            return Condition(notNull, map, Convert(collectionMapperExpression, destExpression.Type));
        }
    }
}
