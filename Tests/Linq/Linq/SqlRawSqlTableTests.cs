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

		// Regression: the SqlRawSqlTable copy ctor (used by QueryElementVisitor.VisitSqlRawSqlTable in Transform
		// mode when a raw table is rebuilt during a clone/convert pass) must carry over IsScalar. Losing it drops
		// the scalar `value` column-alias list at render time, producing a dangling `t1.value` reference. Not known
		// to be reachable from a LINQ query today, but the clone ctor must preserve the flag regardless.
		[Test]
		public void CopyCtor_PreservesIsScalar()
		{
			var ed    = MappingSchema.Default.GetEntityDescriptor(typeof(RawScalarRow));
			var table = new SqlRawSqlTable(ed, "SELECT 1", isScalar: true, Array.Empty<ISqlExpression>());

			table.IsScalar.ShouldBeTrue();

			// Deep clone runs QueryElementVisitor.VisitSqlRawSqlTable in Transform mode, which rebuilds the table
			// through the copy ctor.
			var clone = table.Clone(static _ => true)!;

			clone.IsScalar.ShouldBe(table.IsScalar);
		}
	}
}
