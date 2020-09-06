using System;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoMapper.EquivalencyExpression
{
    internal class EquivalentExpression : IEquivalentComparer
    {
        internal static IEquivalentComparer BadValue { get; }

        static EquivalentExpression() => BadValue = new EquivalentExpression();

        public int GetHashCode(object obj) => throw new Exception("How'd you get here");

        public bool IsEquivalent(object source, object destination) => false;
    }

    internal class EquivalentExpression<TSource, TDestination> : IEquivalentComparer<TSource, TDestination>
    {
        private readonly Expression<Func<TSource, TDestination, bool>> _equivalentExpression;
        private readonly Func<TSource, TDestination, bool> _equivalentFunc;
        private readonly Func<TSource, int> _sourceHashCodeFunc;
        private readonly Func<TDestination, int> _destinationHashCodeFunc;

        public EquivalentExpression(Expression<Func<TSource, TDestination, bool>> equivalentExpression)
        {
            _equivalentExpression = equivalentExpression;
            _equivalentFunc = _equivalentExpression.Compile();

            var sourceParameter = equivalentExpression.Parameters[0];
            var destinationParameter = equivalentExpression.Parameters[1];

            var members = HashableExpressionsVisitor.Expand(sourceParameter, destinationParameter, equivalentExpression);

            _sourceHashCodeFunc = members.Item1.GetHashCodeExpression<TSource>(sourceParameter).Compile();
            _destinationHashCodeFunc = members.Item2.GetHashCodeExpression<TDestination>(destinationParameter).Compile();
        }

        public bool IsEquivalent(object source, object destination)
        {
            if (source == null && destination == null) return true;

            return source == null || destination == null
                ? false
                : source is TSource src && destination is TDestination dest && _equivalentFunc(src, dest);
        }

        public Expression<Func<TDestination, bool>> ToSingleSourceExpression(TSource source)
        {
            if (source == null) throw new ArgumentNullException("Invalid somehow");

            var expression = new ParametersToConstantVisitor<TSource>(source).Visit(_equivalentExpression) as LambdaExpression;
            return Expression.Lambda<Func<TDestination, bool>>(expression.Body, _equivalentExpression.Parameters[1]);
        }

        public int GetHashCode(object obj)
        {
            if (obj is TSource src) return _sourceHashCodeFunc(src);

            return obj is TDestination dest
                ? _destinationHashCodeFunc(dest)
                : default;
        }
    }

    internal class ParametersToConstantVisitor<T> : ExpressionVisitor
    {
        private readonly T _value;

        public ParametersToConstantVisitor(T value) => _value = value;

        protected override Expression VisitParameter(ParameterExpression node) => node;

        protected override Expression VisitMember(MemberExpression node)
        {
            return (node.Member is PropertyInfo pi && pi.DeclaringType.GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo()))
            ? Expression.Constant(pi.GetValue(_value, null))
            : base.VisitMember(node);
        }
    }
}