using System;
using System.Linq.Expressions;
using LinqToDB.Common.Internal;
using LinqToDB.Extensions;
using LinqToDB.Linq.Builder;

namespace LinqToDB.Expressions
{
	class SqlPlaceholderExpression : Expression
	{
		public SqlPlaceholderExpression(IBuildContext? buildContext, SqlInfo sql, Expression memberExpression, Type? convertType = null, string? alias = null)
		{
			BuildContext     = buildContext;
			MemberExpression = memberExpression;
			ConvertType      = convertType;
			Alias            = alias;
			Sql              = sql;
		}

		public IBuildContext? BuildContext     { get; }
		public Expression     MemberExpression { get; }
		public Type?          ConvertType      { get; }
		public string?        Alias            { get; set; }
		public SqlInfo        Sql              { get; }


		public override ExpressionType NodeType => ExpressionType.Extension;
		public override Type           Type     => ConvertType ?? MemberExpression.Type;


		public SqlPlaceholderExpression MakeNullable()
		{
			if (!Type.IsNullableType())
			{
				var type = Type.AsNullable();
				return new SqlPlaceholderExpression(BuildContext, Sql, MemberExpression, type);
			}

			return this;
		}

		public override string ToString()
		{
			if (Sql?.Index >= 0)
				return $"SQL[{Sql.Index}]: {Sql?.Sql}";
			return $"SQL: {Sql?.Sql}";
		}
	}

}
