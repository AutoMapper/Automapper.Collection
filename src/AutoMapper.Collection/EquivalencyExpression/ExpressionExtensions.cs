using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using AutoMapper.Collection;

namespace AutoMapper.EquivalencyExpression
{
    internal static class ExpressionExtensions
    {
        private static readonly ConcurrentDictionary<Type, Type> _singleParameterTypeDictionary = new ConcurrentDictionary<Type, Type>();

        public static Type GetSinglePredicateExpressionArgumentType(this Type type)
        {
            return _singleParameterTypeDictionary.GetOrAdd(type, t =>
            {
                var isExpression = typeof(Expression).GetTypeInfo().IsAssignableFrom(t.GetTypeInfo());
                if (!isExpression) return null;

                var expressionOf = t.GetTypeInfo().GenericTypeArguments[0];
                var isFunction = expressionOf.GetGenericTypeDefinition() == typeof(Func<,>);
                if (!isFunction) return null;

                var isPredicate = expressionOf.GetTypeInfo().GenericTypeArguments[1] == typeof(bool);
                if (!isPredicate) return null;

                var objType = expressionOf.GetTypeInfo().GenericTypeArguments[0];
                return CacheAndReturnType(type, objType);
            });
        }

        private static Type CacheAndReturnType(Type type, Type objType)
        {
            _singleParameterTypeDictionary.AddOrUpdate(type, objType, (_, __) => objType);
            return objType;
        }

        public static Expression<Func<T, int>> GetHashCodeExpression<T>(this List<Expression> members, ParameterExpression sourceParam)
        {
            var hashMultiply = Expression.Constant(397L);

            var hashVariable = Expression.Variable(typeof(long), "hashCode");
            var returnTarget = Expression.Label(typeof(int));
            var returnExpression = Expression.Return(returnTarget, Expression.Convert(hashVariable, typeof(int)), typeof(int));
            var returnLabel = Expression.Label(returnTarget, Expression.Constant(-1));

            var expressions = new List<Expression>();
            foreach (var member in members)
            {
                // Call the GetHashCode method
                var hasCodeExpression = Expression.Convert(Expression.Call(member, member.Type.GetDeclaredMethod(nameof(GetHashCode))), typeof(long));

                // return (((object)x) == null ? 0 : x.GetHashCode())
                var hashCodeReturnTarget = Expression.Label(typeof(long));
                var hashCode = Expression.Block(
                    Expression.IfThenElse(
                        Expression.ReferenceEqual(Expression.Convert(member, typeof(object)), Expression.Constant(null)),
                        Expression.Return(hashCodeReturnTarget, Expression.Constant(0L, typeof(long))),
                        Expression.Return(hashCodeReturnTarget, hasCodeExpression)),
                    Expression.Label(hashCodeReturnTarget, Expression.Constant(0L, typeof(long))));

                if (expressions.Count == 0)
                {
                    expressions.Add(Expression.Assign(hashVariable, hashCode));
                }
                else
                {
                    var oldHashMultiplied = Expression.Multiply(hashVariable, hashMultiply);
                    var xOrHash = Expression.ExclusiveOr(oldHashMultiplied, hashCode);
                    expressions.Add(Expression.Assign(hashVariable, xOrHash));
                }
            }

            expressions.Add(returnExpression);
            expressions.Add(returnLabel);

            var resultBlock = Expression.Block(new[] { hashVariable }, expressions);

            return Expression.Lambda<Func<T, int>>(resultBlock, sourceParam);
        }
    }
}