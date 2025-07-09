using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Expressions
{
	public sealed class SqlPlaceholderExpression : Expression
	{
#if DEBUG
		static int _placeholderCounter;
		public int Id { get; }
#endif

		public SqlPlaceholderExpression(SelectQuery? selectQuery, ISqlExpression sql, Expression path, Type? convertType = null, string? alias = null, int? index = null, Expression? trackingPath = null)
		{
#if BUGCHECK

			if (sql is SqlColumn column && column.Parent == selectQuery)
				throw new InvalidOperationException();
			if (path is SqlPlaceholderExpression)
				throw new InvalidOperationException();
			if (trackingPath is SqlPlaceholderExpression)
				throw new InvalidOperationException();

			if (null != sql.Find(e => e is SelectQuery sc && ReferenceEquals(sc, selectQuery)))
			{
				throw new InvalidOperationException($"Wrong select query.");
			}

#endif

			SelectQuery = selectQuery;
			Path         = path;
			ConvertType  = convertType ?? path.Type;
			Alias        = alias;
			Index        = index;
			Sql          = sql;
			TrackingPath = trackingPath;

#if DEBUG
			Id = System.Threading.Interlocked.Increment(ref _placeholderCounter);

			if (Id == 0)
			{

			}
#endif
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

#if DEBUG
		public List<SqlPlaceholderExpression>? History { get; private set; }

		void AppendHistory(SqlPlaceholderExpression ancestor)
		{
			History ??= new List<SqlPlaceholderExpression>();
			if (ancestor.History != null)
				History.AddRange(ancestor.History);

			History.Add(ancestor);
		}
#endif

		public SqlPlaceholderExpression MakeNullable()
		{
			if (!Type.IsNullableType())
			{
				var type           = Type.AsNullable();
				var newPlaceholder = new SqlPlaceholderExpression(SelectQuery, Sql, Path, type, Alias, Index, TrackingPath);
#if DEBUG
				newPlaceholder.AppendHistory(this);
#endif
				return newPlaceholder;
			}

			return this;
		}

		public SqlPlaceholderExpression MakeNotNullable()
		{
			if (Type.IsNullable())
			{
				var type           = Type.GetGenericArguments()[0];
				var newPlaceholder = new SqlPlaceholderExpression(SelectQuery, Sql, Path, type, Alias, Index, TrackingPath);
#if DEBUG
				newPlaceholder.AppendHistory(this);
#endif
				return newPlaceholder;
			}

			return this;
		}

		public SqlPlaceholderExpression WithPath(Expression path)
		{
			if (ExpressionEqualityComparer.Instance.Equals(path, Path))
				return this;

			var newPlaceholder = new SqlPlaceholderExpression(SelectQuery, Sql, path, Type, Alias, Index, TrackingPath);
#if DEBUG
			newPlaceholder.AppendHistory(this);
#endif
			return newPlaceholder;
		}

		public SqlPlaceholderExpression WithSql(ISqlExpression sqlExpression)
		{
			if (Sql.Equals(sqlExpression))
				return this;

			var newPlaceholder = new SqlPlaceholderExpression(SelectQuery, sqlExpression, Path, Type, Alias, Index, TrackingPath);
#if DEBUG
			newPlaceholder.AppendHistory(this);
#endif
			return newPlaceholder;
		}

		public SqlPlaceholderExpression WithSelectQuery(SelectQuery selectQuery)
		{
			if (ReferenceEquals(SelectQuery, selectQuery))
				return this;

			var newPlaceholder = new SqlPlaceholderExpression(selectQuery, Sql, Path, Type, Alias, Index, TrackingPath);
#if DEBUG
			newPlaceholder.AppendHistory(this);
#endif
			return newPlaceholder;
		}

		public SqlPlaceholderExpression WithTrackingPath(Expression trackingPath)
		{
			if (ReferenceEquals(trackingPath, TrackingPath) || ExpressionEqualityComparer.Instance.Equals(trackingPath, TrackingPath))
				return this;

			var newPlaceholder = new SqlPlaceholderExpression(SelectQuery, Sql, Path, Type, Alias, Index, trackingPath);
#if DEBUG
			newPlaceholder.AppendHistory(this);
#endif
			return newPlaceholder;
		}

		public SqlPlaceholderExpression WithAlias(string? alias)
		{
			if (Equals(Alias, alias))
				return this;

			var newPlaceholder = new SqlPlaceholderExpression(SelectQuery, Sql, Path, Type, alias, Index, TrackingPath);
#if DEBUG
			newPlaceholder.AppendHistory(this);
#endif
			return newPlaceholder;
		}

		public override string ToString()
		{
			var pathStr = "#" + ExpressionEqualityComparer.Instance.GetHashCode(Path)/* + " " + Path*/;

			var startStr = "SQL";
#if DEBUG
			startStr += $"[ID:{Id}][{Type.Name}]";
#endif
			string result;
			if (SelectQuery == null)
			{
				if (Sql is SqlColumn column)
				{
					var sourceId = column.Parent!.SourceID;
					result = $"{startStr}[S:{Index}]({sourceId})";
				}
				else
					result = $"{startStr}";
			}
			else
				result = $"{startStr}(S:{SelectQuery.SourceID})";

			var sqlStr = $"{{{Sql}}}";
			if (Sql.CanBeNullable(NullabilityContext.NonQuery) && Sql is not SqlColumn)
				sqlStr += "?";
			result += $": {sqlStr} ({pathStr})";

			/*
			if (TrackingPath != null)
			{
				result += " TP: " + TrackingPath;
			}
			*/

			return result;
		}

		public bool Equals(SqlPlaceholderExpression other)
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
			{
				var newPlaceholder = new SqlPlaceholderExpression(SelectQuery, Sql, Path, type, Alias, Index, TrackingPath);
#if DEBUG
				newPlaceholder.AppendHistory(this);
#endif
				return newPlaceholder;
			}

			return this;
		}

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitSqlPlaceholderExpression(this);
			return base.Accept(visitor);
		}

	}

}
