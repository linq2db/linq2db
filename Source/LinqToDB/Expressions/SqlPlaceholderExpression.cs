using System;
using System.Linq.Expressions;
using LinqToDB.Linq.Builder;

namespace LinqToDB.Expressions
{
	class SqlPlaceholderExpression : Expression
	{
		public SqlPlaceholderExpression(IBuildContext? buildContext, Expression memberExpression, Type? convertType = null)
		{
			BuildContext     = buildContext;
			MemberExpression = memberExpression;
			ConvertType      = convertType ?? MemberExpression.Type;
		}

		public IBuildContext? BuildContext     { get; }
		public Expression     MemberExpression { get; }
		public Type           ConvertType      { get; }

		internal SqlInfo Sql { get; set; } = default!;

		public override ExpressionType NodeType => ExpressionType.Extension;
		public override Type           Type     => ConvertType;

		public override string ToString()
		{
			if (Sql?.Index >= 0)
				return $"SQL[{Sql.Index}]: {Sql?.Sql}";
			return $"SQL: {Sql?.Sql}";
		}
	}

}
