using System;

using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class SqlRawSqlTableTests : TestBase
	{
		sealed class RawScalarRow
		{
			public int Value;
		}

		// Regression: rebuilding a SqlRawSqlTable (QueryElementVisitor.VisitSqlRawSqlTable, Transform mode, hit on
		// every clone/convert pass) must carry over IsScalar. Losing it drops the scalar `value` column-alias list
		// at render time, producing a dangling `t1.value` reference. Not known to be reachable from a LINQ query
		// today, but the rebuild must preserve the flag regardless. The visitor now rebuilds through the explicit
		// SqlRawSqlTable ctor, which also fixes SqlTableType to RawSql.
		[Test]
		public void Clone_PreservesRawSqlTableState()
		{
			var ed    = MappingSchema.Default.GetEntityDescriptor(typeof(RawScalarRow));
			var table = new SqlRawSqlTable(ed, "SELECT 1", isScalar: true, Array.Empty<ISqlExpression>());

			table.IsScalar.ShouldBeTrue();

			// Deep clone runs QueryElementVisitor.VisitSqlRawSqlTable in Transform mode, which rebuilds the table.
			var clone = table.Clone(static _ => true)!;

			clone.IsScalar.ShouldBe(table.IsScalar);
			clone.SQL.ShouldBe(table.SQL);
			clone.SqlTableType.ShouldBe(SqlTableType.RawSql);
		}
	}
}
