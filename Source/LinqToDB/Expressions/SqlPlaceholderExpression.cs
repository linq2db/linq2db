using System;
using System.Linq.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Linq.Builder;

namespace LinqToDB.Expressions
{
	class SqlPlaceholderExpression : Expression
	{
		private readonly Type _valueType;

		public SqlPlaceholderExpression(IBuildContext? buildContext, SqlInfo sql, Expression memberExpression, bool isNullable)
		{
			BuildContext     = buildContext;
			MemberExpression = memberExpression;
			IsNullable       = isNullable;
			Sql              = sql;

			_valueType = memberExpression.Type;
		}

		public IBuildContext? BuildContext     { get; }
		public Expression     MemberExpression { get; }
		public bool           IsNullable       { get; }
		public SqlInfo        Sql              { get; }


		public override ExpressionType NodeType => ExpressionType.Extension;
		public override Type           Type     => _valueType;

		public override string ToString()
		{
			if (Sql?.Index >= 0)
				return $"SQL[{Sql.Index}]: {Sql?.Sql}";
			return $"SQL: {Sql?.Sql}";
		}
	}

}
