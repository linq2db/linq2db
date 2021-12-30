using System;
using System.Linq.Expressions;
using LinqToDB.Common.Internal;
using LinqToDB.Extensions;
using LinqToDB.Linq.Builder;
using LinqToDB.SqlQuery;

namespace LinqToDB.Expressions
{
	class SqlPlaceholderExpression : Expression
	{
		public SqlPlaceholderExpression(SelectQuery selectQuery, ISqlExpression sql, Expression path, Type? convertType = null, string? alias = null, int? index = null)
		{
			SelectQuery = selectQuery;
			Path        = path;
			ConvertType = convertType;
			Alias       = alias;
			Index       = index;
			Sql         = sql;
		}

		public SelectQuery    SelectQuery { get; }
		public Expression     Path        { get; }
		public int?           Index       { get; }
		public string?        Alias       { get; set; }
		public ISqlExpression Sql         { get; }
		public Type?          ConvertType { get; }


		public override ExpressionType NodeType => ExpressionType.Extension;
		public override Type           Type     => ConvertType ?? Path.Type;


		public SqlPlaceholderExpression MakeNullable()
		{
			if (!Type.IsNullableType())
			{
				var type = Type.AsNullable();
				return new SqlPlaceholderExpression(SelectQuery, Sql, Path, type, Alias, Index);
			}

			return this;
		}

		public override string ToString()
		{
			if (Index != null)
				return $"SQL[{Index}]: {{{Sql}}}";
			return $"SQL: {{{Sql}}}";
		}
	}

}
