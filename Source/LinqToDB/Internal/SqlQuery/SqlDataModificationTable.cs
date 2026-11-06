using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using LinqToDB.Internal.SqlQuery.Visitors;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// Rows returned by a data-modifying statement (<c>INSERT ... RETURNING/OUTPUT</c>), used as a table source.
	/// Its fields are copies of the target table's fields. A query reads it only from the body of a CTE that selects
	/// its fields, and the provider renders that body as the statement with an output list of the selected fields.
	/// </summary>
	public sealed class SqlDataModificationTable : SqlTable
	{
		public SqlDataModificationTable(SqlTable target, SqlStatementWithQueryBase statement, object? identity)
			: base(target.ObjectType, null, target.TableName)
		{
			Statement    = statement;
			Identity     = identity;
			SqlTableType = SqlTableType.DataModification;

			foreach (var field in target.Fields)
				Add(new SqlField(field));
		}

		public SqlDataModificationTable(SqlDataModificationTable table, SqlStatementWithQueryBase statement)
			: base(table.ObjectType, null, table.TableName)
		{
			Alias              = table.Alias;
			Statement          = statement;
			Identity           = table.Identity;
			SqlTableType       = SqlTableType.DataModification;
			SqlQueryExtensions = table.SqlQueryExtensions;

			foreach (var field in table.Fields)
				Add(new SqlField(field));
		}

		internal SqlDataModificationTable(int id, string alias, Type objectType, SqlObjectName tableName, SqlField[] fields, SqlStatementWithQueryBase statement)
			: base(objectType, id, tableName)
		{
			Alias        = alias;
			Statement    = statement;
			SqlTableType = SqlTableType.DataModification;

			AddRange(fields);
		}

		/// <summary>
		/// The data-modifying statement whose output rows this table returns.
		/// </summary>
		public SqlStatementWithQueryBase Statement { get; set; }

		/// <summary>
		/// Identity of the query call that produced the statement: references to one call share it.
		/// <see langword="null"/> after remote transfer, where it is no longer needed.
		/// </summary>
		public object? Identity { get; }

		public override QueryElementType ElementType => QueryElementType.SqlDataModificationTable;

		public override bool Equals(ISqlExpression? other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			return ReferenceEquals(this, other);
		}

		public override int GetElementHashCode()
		{
			return RuntimeHelpers.GetHashCode(this);
		}

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.DebugAppendUniqueId(this)
				.AppendLine("DataModification(");

			using (writer.IndentScope())
				writer.AppendElement(Statement);

			writer
				.AppendLine()
				.Append(')');

			return writer;
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlDataModificationTable(this);
	}
}
