using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AutoMapper.EquivalencyExpression
{
    internal class HashableExpressionsVisitor : ExpressionVisitor
    {
        private readonly List<Expression> _destinationMembers = new List<Expression>();
        private readonly ParameterExpression _destinationParameter;
        private readonly List<Expression> _sourceMembers = new List<Expression>();
        private readonly ParameterExpression _sourceParameter;

        private readonly ParameterFinderVisitor _paramFinder = new ParameterFinderVisitor();

        internal HashableExpressionsVisitor(ParameterExpression sourceParameter, ParameterExpression destinationParameter)
        {
            _sourceParameter = sourceParameter;
            _destinationParameter = destinationParameter;
        }

        internal static Tuple<List<Expression>, List<Expression>> Expand(ParameterExpression sourceParameter, ParameterExpression destinationParameter, Expression expression)
        {
            var visitor = new HashableExpressionsVisitor(sourceParameter, destinationParameter);
            visitor.Visit(expression);
            return Tuple.Create(visitor._sourceMembers, visitor._destinationMembers);
        }

        protected override Expression VisitConditional(ConditionalExpression node) => node;

        protected override Expression VisitBinary(BinaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                    VisitCompare(node.Left, node.Right);
                    break;
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return base.VisitBinary(node);
                //case ExpressionType.Or:
                //case ExpressionType.OrElse:
                //    return node; // Maybe compare 0r's for expression matching on side
            }

            return node;
        }

        private void VisitCompare(Expression leftNode, Expression rightNode)
        {
            _paramFinder.Visit(leftNode);
            var left = _paramFinder.Parameters;
            _paramFinder.Visit(rightNode);
            var right = _paramFinder.Parameters;

            if (left.All(p => p == _destinationParameter) && right.All(p => p == _sourceParameter))
            {
                _sourceMembers.Add(rightNode);
                _destinationMembers.Add(leftNode);
            }
            if (left.All(p => p == _sourceParameter) && right.All(p => p == _destinationParameter))
            {
                _sourceMembers.Add(leftNode);
                _destinationMembers.Add(rightNode);
            }
        }
    }

    internal class ParameterFinderVisitor : ExpressionVisitor
    {
        public IList<ParameterExpression> Parameters { get; private set; }

        public override Expression Visit(Expression node)
        {
            Parameters = new List<ParameterExpression>();
            return base.Visit(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            Parameters.Add(node);
            return node;
        }
    }
}