using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	using Common.Internal;
	using Extensions;
	using SqlQuery;

	sealed class SqlPlaceholderExpression : Expression
	{
		public SqlPlaceholderExpression(SelectQuery? selectQuery, ISqlExpression sql, Expression path, Type? convertType = null, string? alias = null, int? index = null, Expression? trackingPath = null)
		{
			#if DEBUG
			if (sql is SqlColumn column && column.Parent == selectQuery)
				throw new InvalidOperationException();
			if (path is SqlPlaceholderExpression)
				throw new InvalidOperationException();
			if (trackingPath is SqlPlaceholderExpression)
				throw new InvalidOperationException();
			#endif

			SelectQuery  = selectQuery;
			Path         = path;
			ConvertType  = convertType ?? path.Type;
			Alias        = alias;
			Index        = index;
			Sql          = sql;
			TrackingPath = trackingPath;
		}

		public SelectQuery?   SelectQuery  { get; }
		public Expression     Path         { get; }
		public Expression?    TrackingPath { get; }
		public int?           Index        { get; }
		public string?        Alias        { get; set; }
		public ISqlExpression Sql          { get; }
		public Type           ConvertType  { get; }


		public override ExpressionType NodeType => ExpressionType.Extension;
		public override Type           Type     => ConvertType ?? Path.Type;


		public SqlPlaceholderExpression MakeNullable()
		{
			if (!Type.IsNullableType())
			{
				var type = Type.AsNullable();
				return new SqlPlaceholderExpression(SelectQuery, Sql, Path, type, Alias, Index, TrackingPath);
			}

			return this;
		}

		public SqlPlaceholderExpression MakeNotNullable()
		{
			if (Type.IsNullable())
			{
				var type = Type.GetGenericArguments()[0];
				return new SqlPlaceholderExpression(SelectQuery, Sql, Path, type, Alias, Index, TrackingPath);
			}

			return this;
		}

		public SqlPlaceholderExpression WithPath(Expression path)
		{
			if (ExpressionEqualityComparer.Instance.Equals(path, Path))
				return this;

			return new SqlPlaceholderExpression(SelectQuery, Sql, path, Type, Alias, Index, TrackingPath);
		}

		public SqlPlaceholderExpression WithSql(ISqlExpression sqlExpression)
		{
			if (Sql.Equals(sqlExpression))
				return this;

			return new SqlPlaceholderExpression(SelectQuery, sqlExpression, Path, Type, Alias, Index, TrackingPath);
		}

		public SqlPlaceholderExpression WithSelectQuery(SelectQuery selectQuery)
		{
			if (SelectQuery != null && SelectQuery.Equals(selectQuery))
				return this;

			return new SqlPlaceholderExpression(selectQuery, Sql, Path, Type, Alias, Index, TrackingPath);
		}

		public SqlPlaceholderExpression WithTrackingPath(Expression trackingPath)
		{
			if (ExpressionEqualityComparer.Instance.Equals(trackingPath, TrackingPath))
				return this;

			return new SqlPlaceholderExpression(SelectQuery, Sql, Path, Type, Alias, Index, trackingPath);
		}

		public Expression WithAlias(string? alias)
		{
			if (Equals(Alias, alias))
				return this;

			return new SqlPlaceholderExpression(SelectQuery, Sql, Path, Type, alias, Index, TrackingPath);
		}

		public override string ToString()
		{
			var pathStr = "#" + ExpressionEqualityComparer.Instance.GetHashCode(Path)/* + " " + Path*/;

			string result;
			if (SelectQuery == null)
			{
				if (Sql is SqlColumn column)
				{
					var sourceId = column.Parent!.SourceID;
					result = $"SQL[{Index}]({sourceId})";
				}
				else
					result = $"SQL";
			}
			else
				result = $"SQL({SelectQuery.SourceID})";

			var sqlStr = $"{{{Sql}}}";
			if (Sql.CanBeNull && Sql is not SqlColumn)
				sqlStr += "?";
			result += $": {sqlStr} ({pathStr})";

			return result;
		}

		protected bool Equals(SqlPlaceholderExpression other)
		{
			return Equals(SelectQuery, other.SelectQuery)                       &&
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
				var hashCode = SelectQuery != null ? SelectQuery.GetHashCode() : 0;
				hashCode = (hashCode * 397) ^ ExpressionEqualityComparer.Instance.GetHashCode(Path);
				hashCode = (hashCode * 397) ^ Index.GetHashCode();
				hashCode = (hashCode * 397) ^ ConvertType.GetHashCode();
				return hashCode;
			}
		}

		public SqlPlaceholderExpression WithType(Type type)
		{
			if (Type != type)
				return new SqlPlaceholderExpression(SelectQuery, Sql, Path, type, Alias, Index, TrackingPath);
			return this;
		}
	}

}
