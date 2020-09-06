using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AutoMapper.EquivalencyExpression;
using static System.Linq.Expressions.Expression;

namespace AutoMapper.Mappers
{
    public class ObjectToEquivalencyExpressionByEquivalencyExistingMapper : IConfigurationObjectMapper
    {
        public IConfigurationProvider ConfigurationProvider { get; set; }

        public static Expression<Func<TDestination, bool>> Map<TSource, TDestination>(TSource source, IEquivalentComparer<TSource, TDestination> toSourceExpression) => toSourceExpression.ToSingleSourceExpression(source);

        private static readonly MethodInfo MapMethodInfo = typeof(ObjectToEquivalencyExpressionByEquivalencyExistingMapper).GetRuntimeMethods().First(_ => _.IsStatic);

        public bool IsMatch(TypePair typePair)
        {
            var destExpressArgType = typePair.DestinationType.GetSinglePredicateExpressionArgumentType();
            return destExpressArgType == null
                ? false
                : this.GetEquivalentExpression(typePair.SourceType, destExpressArgType) != null;
        }

        public Expression MapExpression(IConfigurationProvider configurationProvider, ProfileMap profileMap, IMemberMap memberMap,
            Expression sourceExpression, Expression destExpression, Expression contextExpression)
        {
            var destExpressArgType = destExpression.Type.GetSinglePredicateExpressionArgumentType();
            var toSourceExpression = this.GetEquivalentExpression(sourceExpression.Type, destExpressArgType);
            return Call(null, MapMethodInfo.MakeGenericMethod(sourceExpression.Type, destExpressArgType), sourceExpression, Constant(toSourceExpression));
        }
    }
}