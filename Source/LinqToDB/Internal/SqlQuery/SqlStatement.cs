using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LinqToDB.Internal.SqlQuery
{
	[DebuggerDisplay("SQL = {" + nameof(DebugSqlText) + "}")]
	public abstract class SqlStatement : QueryElement
	{
		public string SqlText => this.ToDebugString(SelectQuery);

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		protected string DebugSqlText => Internal.Common.Tools.ToDebugDisplay(SqlText);

		public abstract QueryType QueryType { get; }

		public abstract bool IsParameterDependent { get; set; }

		/// <summary>
		/// Used internally for SQL Builder
		/// </summary>
		public SqlStatement? ParentStatement { get; set; }

		public abstract SelectQuery? SelectQuery { get; set; }

		public SqlComment?              Tag                { get; internal set; }
		public List<SqlQueryExtension>? SqlQueryExtensions { get; set; }

		public abstract ISqlTableSource? GetTableSource(ISqlTableSource table, out bool noAlias);

		/// <summary>
		/// Indicates when optimizer can not remove reference for particular table
		/// </summary>
		/// <param name="table"></param>
		/// <returns></returns>
		public virtual bool IsDependedOn(SqlTable table)
		{
			return false;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(ElementType);
			hash.Add(QueryType);
			hash.Add(SelectQuery?.GetElementHashCode());
			hash.Add(IsParameterDependent);
			hash.Add(Tag?.GetElementHashCode());
			if (SqlQueryExtensions != null)
			{
				foreach (var extension in SqlQueryExtensions)
					hash.Add(extension.GetElementHashCode());
			}

			return hash.ToHashCode();
		}
	}
}
