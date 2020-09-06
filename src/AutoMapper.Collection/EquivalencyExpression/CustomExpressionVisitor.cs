using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoMapper.EquivalencyExpression
{
    internal class CustomExpressionVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _parameter;
        private readonly IEnumerable<PropertyMap> _propertyMaps;

        internal CustomExpressionVisitor(ParameterExpression parameter, IEnumerable<PropertyMap> propertyMaps)
        {
            _parameter = parameter;
            _propertyMaps = propertyMaps;
        }

        protected override Expression VisitParameter(ParameterExpression node) => _parameter;

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member is PropertyInfo pi)
            {
                var matchPM = _propertyMaps.FirstOrDefault(pm => pm.DestinationMember == pi)
                        ?? throw new ArgumentException("No matching PropertyMap");
                var memberGetters = matchPM.SourceMembers;

                var memberExpression = Expression.Property(Visit(node.Expression), memberGetters.First() as PropertyInfo);

                foreach (var memberGetter in memberGetters.Skip(1))
                    memberExpression = Expression.Property(memberExpression, memberGetter as PropertyInfo);
                return memberExpression;
            }
            return base.VisitMember(node);
        }
    }
}