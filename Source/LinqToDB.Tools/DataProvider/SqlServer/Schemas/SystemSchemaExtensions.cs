using System.Linq;

using JetBrains.Annotations;

using LinqToDB.DataProvider.SqlServer;

#pragma warning disable CA1861

namespace LinqToDB.Tools.DataProvider.SqlServer.Schemas
{
	[PublicAPI]
	public static class SystemSchemaExtensions
	{
		/// <summary>
		/// Represents a row count info for a table.
		/// </summary>
		/// <param name="ObjectID">Table ID. </param>
		/// <param name="SchemaName">The schema name of the table. </param>
		/// <param name="TableName">The table name. The table name is not unique in the database.</param>
		/// <param name="RowCount">The number of rows in the table. The value is not exact, but it is close to the actual number of rows.</param>
		public record TableRowCountInfo(int ObjectID, string SchemaName, string TableName, long RowCount)
		{
		}

		/// <summary>
		/// Returns the row count for all user tables in the database.
		/// This method works very fast and does not require any locks.
		/// It can be used for testing purposes to compare number of rows before and after the tests.
		/// </summary>
		public static IQueryable<TableRowCountInfo> GetTableRowCountInfo(this ISystemSchemaData dataContext)
		{
			return
			(
				from p in dataContext.System.Object.Partitions
				where p.IndexID.In(new[] {0, 1}) && p.Object.Type == "U"
				group p by p.ObjectID into g
				orderby 2, 3
				select new TableRowCountInfo
				(
					g.Key,
					SqlFn.ObjectSchemaName(g.Key)!,
					SqlFn.ObjectName(g.Key)!,
					g.Sum(p => p.Rows ?? 0)
				)
			).InlineParameters();
		}
	}
}
