using System;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	using Common.Internal;
	using LinqToDB.Extensions;
	using SqlQuery;

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

		public SqlPlaceholderExpression MakeNotNullable()
		{
			if (Type.IsNullable())
			{
				var type = Type.GetGenericArguments()[0];
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

		protected bool Equals(SqlPlaceholderExpression other)
		{
			return SelectQuery.Equals(other.SelectQuery)                        &&
			       ExpressionEqualityComparer.Instance.Equals(Path, other.Path) &&
			       Index       == other.Index                                   &&
			       ConvertType == other.ConvertType;
		}

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((SqlPlaceholderExpression)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = SelectQuery.GetHashCode();
				hashCode = (hashCode * 397) ^ ExpressionEqualityComparer.Instance.GetHashCode(Path);
				hashCode = (hashCode * 397) ^ Index.GetHashCode();
				hashCode = (hashCode * 397) ^ (ConvertType != null ? ConvertType.GetHashCode() : 0);
				return hashCode;
			}
		}
	}

}
