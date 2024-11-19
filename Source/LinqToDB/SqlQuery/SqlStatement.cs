using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	using Common;

	using Remote;

	[DebuggerDisplay("SQL = {" + nameof(DebugSqlText) + "}")]
	public abstract class SqlStatement : IQueryElement
	{
		public string SqlText => this.ToDebugString(SelectQuery);

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		protected string DebugSqlText => Tools.ToDebugDisplay(SqlText);

		public abstract QueryType QueryType { get; }

		public abstract bool IsParameterDependent { get; set; }

		/// <summary>
		/// Used internally for SQL Builder
		/// </summary>
		public SqlStatement? ParentStatement { get; set; }

		// TODO: V6: used by tests only -> move to test helpers
		[Obsolete("API will be removed in future versions")]
		public SqlParameter[] CollectParameters()
		{
			var parametersHash = new HashSet<SqlParameter>();

			this.VisitAll(parametersHash, static (parametersHash, expr) =>
			{
				switch (expr.ElementType)
				{
					case QueryElementType.SqlParameter:
					{
						var p = (SqlParameter)expr;
						if (p.IsQueryParameter)
							parametersHash.Add(p);

						break;
					}
				}
			});

			return parametersHash.ToArray();
		}

		public abstract SelectQuery? SelectQuery { get; set; }

		public SqlComment?              Tag                { get; internal set; }
		public List<SqlQueryExtension>? SqlQueryExtensions { get; set; }

		#region IQueryElement

#if DEBUG
		public virtual string DebugText => this.ToDebugString();
#endif

		public abstract QueryElementType       ElementType { get; }
		public abstract QueryElementTextWriter ToString(QueryElementTextWriter writer);

		#endregion

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

#if OVERRIDETOSTRING
		public override string ToString()
		{
			return this.ToDebugString(SelectQuery);
		}
#endif

	}
}
