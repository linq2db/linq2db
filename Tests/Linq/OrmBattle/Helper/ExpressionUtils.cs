using System;
using System.Linq.Expressions;

namespace Tests.OrmBattle.Helper
{
    public static class ExpressionUtils
    {
        public static MemberExpression ExtractMember(Expression expression)
        {
            var current = expression;
            if (current.NodeType == ExpressionType.Lambda)
                current = ((LambdaExpression) expression).Body;
            if (current.NodeType == ExpressionType.Convert)
                current = ((UnaryExpression)current).Operand;

            if (current.NodeType != ExpressionType.MemberAccess)
                throw new Exception("Excpression must be a member access");

            return (MemberExpression) current;
        }
    }
}