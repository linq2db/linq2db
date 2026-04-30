using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.xUpdate
{
	public partial class UpdateFromTests
	{
		[Table]
		sealed class UpdateSubquerySourceTable
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public string? FirstName { get; set; }
			[Column] public string? LastName { get; set; }
		}

		[Test]
		public void UpdateFromSubqueryRowCorrelatedValues([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllOracle, TestProvName.AllPostgreSQL, TestProvName.AllInformix, TestProvName.AllFirebird5Plus)] string context)
		{
			using var db = GetDataContext(context);

			using var _ = new DeletePerson(db);

			using var sourceTable = db.CreateLocalTable(
			[
				new UpdateSubquerySourceTable { Id = 1, FirstName = "FirstTooth", LastName = "FirstFairy" },
				new UpdateSubquerySourceTable { Id = 2, FirstName = "SecondTooth", LastName = "SecondFairy" },
				new UpdateSubquerySourceTable { Id = 3, FirstName = "ThirdTooth", LastName = "ThirdFairy" }
			]);

			var affectedCount = sourceTable
				.Where(x => x.Id == 1)
				.Set(
					x => Sql.Row(x.FirstName, x.LastName),
					x => (
						from s in db.SelectQuery(() => 1)
						from canChange in sourceTable.Where(t => t.Id == x.Id + 1).DefaultIfEmpty()
						select Sql.Row(
							canChange != null ? canChange.FirstName! : x.FirstName,
							canChange != null ? canChange.LastName!  : x.LastName
						)
					).Single()
				)
				.Update();

			affectedCount.ShouldBe(1);

			var records = sourceTable.OrderBy(x => x.Id).ToList();

			records[0].FirstName.ShouldBe("SecondTooth");
			records[0].LastName.ShouldBe("SecondFairy");

			records[1].FirstName.ShouldBe("SecondTooth");
			records[1].LastName.ShouldBe("SecondFairy");

			records[2].FirstName.ShouldBe("ThirdTooth");
			records[2].LastName.ShouldBe("ThirdFairy");
		}

		[Test]
		public void UpdateFromSubqueryRowMixedIndependentAndDependent([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllOracle, TestProvName.AllPostgreSQL, TestProvName.AllInformix, TestProvName.AllFirebird5Plus)] string context)
		{
			using var db = GetDataContext(context);

			using var sourceTable = db.CreateLocalTable(
			[
				new UpdateSubquerySourceTable { Id = 1, FirstName = "FirstTooth",  LastName  = "FirstFairy"  },
				new UpdateSubquerySourceTable { Id = 2, FirstName = "SecondTooth", LastName  = "SecondFairy" },
				new UpdateSubquerySourceTable { Id = 3, FirstName = "ThirdTooth",  LastName  = "ThirdFairy"  }
			]);

			// (FirstName, LastName) = ("literal", (subquery on next row).LastName)
			// FirstName side is independent of other tables; LastName side depends on a
			// correlated subquery. Exercises the independent/dependent split in
			// ProcessUpdateItemsWithRows — earlier code emitted (col, col) = (col, col)
			// for the independent slot, dropping the literal value.
			var affectedCount = sourceTable
				.Where(x => x.Id == 1)
				.Set(
					x => Sql.Row(x.FirstName, x.LastName),
					x => Sql.Row(
						(string?)"literalFirst",
						sourceTable.Where(t => t.Id == x.Id + 1).Select(t => t.LastName).First()))
				.Update();

			affectedCount.ShouldBe(1);

			var records = sourceTable.OrderBy(x => x.Id).ToList();

			records[0].FirstName.ShouldBe("literalFirst");
			records[0].LastName .ShouldBe("SecondFairy");

			records[1].FirstName.ShouldBe("SecondTooth");
			records[1].LastName .ShouldBe("SecondFairy");

			records[2].FirstName.ShouldBe("ThirdTooth");
			records[2].LastName .ShouldBe("ThirdFairy");
		}

		[Test]
		public void UpdateFromSubqueryRowSingle([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllOracle12Plus, TestProvName.AllPostgreSQL, TestProvName.AllInformix, TestProvName.AllFirebird5Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var table1 = db.CreateLocalTable<NewEntities>();
			using var table2 = db.CreateLocalTable<UpdatedEntities>();
			using var table3 = db.CreateLocalTable<UpdateRelation>();

			table1
				.Where(u1 => u1.id == 7)
				.Set(
					u1 => Sql.Row(u1.Value1, u1.Value2),
					u1 => (
						from c in db.SelectQuery(() => u1.Value3 + 10)
						from n2 in table2.LeftJoin(n2 => n2.id == c)
						from n3 in table3.LeftJoin(n3 => n2.RelationId == n3.id)
						where n3.RelatedValue3 < 1000
						select Sql.Row(n2.Value1, n3.RelatedValue2))
						.Single()
				)
				.Update();

			AssertRowUpdateOptimized(context);
		}

		[Test]
		public void UpdateFromSubqueryRowSingleOrDefault([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllOracle12Plus, TestProvName.AllPostgreSQL, TestProvName.AllInformix, TestProvName.AllFirebird5Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var table1 = db.CreateLocalTable<NewEntities>();
			using var table2 = db.CreateLocalTable<UpdatedEntities>();
			using var table3 = db.CreateLocalTable<UpdateRelation>();

			table1
				.Where(u1 => u1.id == 7)
				.Set(
					u1 => Sql.Row(u1.Value1, u1.Value2),
					u1 => (
						from c in db.SelectQuery(() => u1.Value3 + 10)
						from n2 in table2.LeftJoin(n2 => n2.id == c)
						from n3 in table3.LeftJoin(n3 => n2.RelationId == n3.id)
						where n3.RelatedValue3 < 1000
						select Sql.Row(n2.Value1, n3.RelatedValue2))
						.SingleOrDefault()
				)
				.Update();

			AssertRowUpdateOptimized(context);
		}

		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllInformix, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		[Test]
		public void UpdateFromSubqueryRowFirst([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllOracle12Plus, TestProvName.AllPostgreSQL, TestProvName.AllInformix, TestProvName.AllFirebird5Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var table1 = db.CreateLocalTable<NewEntities>();
			using var table2 = db.CreateLocalTable<UpdatedEntities>();
			using var table3 = db.CreateLocalTable<UpdateRelation>();

			table1
				.Where(u1 => u1.id == 7)
				.Set(
					u1 => Sql.Row(u1.Value1, u1.Value2),
					u1 => (
						from c in db.SelectQuery(() => u1.Value3 + 10)
						from n2 in table2.LeftJoin(n2 => n2.id == c)
						from n3 in table3.LeftJoin(n3 => n2.RelationId == n3.id)
						where n3.RelatedValue3 < 1000
						select Sql.Row(n2.Value1, n3.RelatedValue2))
						.First()
				)
				.Update();

			AssertRowUpdateOptimized(context);
		}

		[Test]
		public void UpdateFromScalarSettersSharingSubquery([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllOracle12Plus, TestProvName.AllPostgreSQL, TestProvName.AllInformix, TestProvName.AllFirebird5Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var table1 = db.CreateLocalTable<NewEntities>();
			using var table2 = db.CreateLocalTable<UpdatedEntities>();
			using var table3 = db.CreateLocalTable<UpdateRelation>();

			var query =
				from u1 in table1
				let row = (from c in db.SelectQuery(() => u1.Value3 + 10)
						from n2 in table2.LeftJoin(n2 => n2.id         == c)
						from n3 in table3.LeftJoin(n3 => n2.RelationId == n3.id)
						where n3.RelatedValue3 < 1000
						select new { A = n2.Value1, B = n3.RelatedValue2 })
					.Single()
				where u1.id == 7
				select new { Data = u1, row };

			query
				.Set(x => x.Data.Value1, x => x.row.A)
				.Set(x => x.Data.Value2, x => x.row.B)
				.Update();

			AssertRowUpdateOptimized(context);
		}

		[Test]
		public void UpdateFromScalarSettersTwoSharedSubqueries([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllOracle, TestProvName.AllPostgreSQL, TestProvName.AllInformix, TestProvName.AllFirebird5Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var table1 = db.CreateLocalTable<UpdatedEntities>();
			using var table2 = db.CreateLocalTable<UpdateRelation>();

			var query =
				from u in table1
				let row1 = (from r in table2 where r.id == u.RelationId select new { A = r.RelatedValue1, B = r.RelatedValue2 }).Single()
				let row2 = (from r in table2 where r.id == u.RelationId select new { A = r.RelatedValue3, B = (int?)r.id    }).Single()
				where u.id == 1
				select new { Data = u, row1, row2 };

			query
				.Set(x => x.Data.Value1,     x => x.row1.A)
				.Set(x => x.Data.Value2,     x => x.row1.B)
				.Set(x => x.Data.Value3,     x => x.row2.A)
				.Set(x => x.Data.RelationId, x => x.row2.B)
				.Update();

			LastQuery!.ShouldNotContain("EXISTS");
		}

		[Test]
		public void UpdateFromScalarSettersSharedSubqueryAndPlainScalar([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllOracle, TestProvName.AllPostgreSQL, TestProvName.AllInformix, TestProvName.AllFirebird5Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var table1 = db.CreateLocalTable<UpdatedEntities>();
			using var table2 = db.CreateLocalTable<UpdateRelation>();

			var query =
				from u in table1
				let row = (from r in table2 where r.id == u.RelationId select new { A = r.RelatedValue1, B = r.RelatedValue2 }).Single()
				where u.id == 1
				select new { Data = u, row };

			query
				.Set(x => x.Data.Value1, x => x.row.A)
				.Set(x => x.Data.Value2, x => x.row.B)
				.Set(x => x.Data.Value3, 42)
				.Update();

			LastQuery!.ShouldNotContain("EXISTS");
			LastQuery!.ShouldContain("Value3", AtLeast.Once());
		}

		[Test]
		public void UpdateFromScalarSettersSharingSubqueryPostgreSql([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			using var table1 = db.CreateLocalTable<NewEntities>();
			using var table2 = db.CreateLocalTable<UpdatedEntities>();
			using var table3 = db.CreateLocalTable<UpdateRelation>();

			var query =
				from u1 in table1
				let row = (from c in db.SelectQuery(() => u1.Value3 + 10)
						from n2 in table2.LeftJoin(n2 => n2.id         == c)
						from n3 in table3.LeftJoin(n3 => n2.RelationId == n3.id)
						where n3.RelatedValue3 < 1000
						select new { A = n2.Value1, B = n3.RelatedValue2 })
					.Single()
				where u1.id == 7
				select new { Data = u1, row };

			query
				.Set(x => x.Data.Value1, x => x.row.A)
				.Set(x => x.Data.Value2, x => x.row.B)
				.Update();

			LastQuery!.ShouldContain("(\"Value1\", \"Value2\")", AtLeast.Once());
		}

		[Test]
		public void UpdateFromScalarSettersSharingSubqueryNoRowFlattened([IncludeDataSources(TestProvName.AllFirebird5Plus, TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			using var sourceTable = db.CreateLocalTable<UpdateSubquerySourceTable>();

			var query =
				from x in sourceTable
				let row = (from t in sourceTable where t.Id == x.Id + 1 select new { A = t.FirstName, B = t.LastName }).Single()
				where x.Id == 1
				select new { Data = x, row };

			query
				.Set(x => x.Data.FirstName, x => x.row.A)
				.Set(x => x.Data.LastName,  x => x.row.B)
				.Update();

			LastQuery!.ShouldNotContain(") =");
		}

		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllInformix, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		[Test]
		public void UpdateFromSubqueryRowFirstOrDefault([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllOracle12Plus, TestProvName.AllPostgreSQL, TestProvName.AllInformix, TestProvName.AllFirebird5Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var table1 = db.CreateLocalTable<NewEntities>();
			using var table2 = db.CreateLocalTable<UpdatedEntities>();
			using var table3 = db.CreateLocalTable<UpdateRelation>();

			table1
				.Where(u1 => u1.id == 7)
				.Set(
					u1 => Sql.Row(u1.Value1, u1.Value2),
					u1 => (
						from c in db.SelectQuery(() => u1.Value3 + 10)
						from n2 in table2.LeftJoin(n2 => n2.id == c)
						from n3 in table3.LeftJoin(n3 => n2.RelationId == n3.id)
						where n3.RelatedValue3 < 1000
						select Sql.Row(n2.Value1, n3.RelatedValue2))
						.FirstOrDefault()
				)
				.Update();

			AssertRowUpdateOptimized(context);
		}

		// Per-provider shape assertions for the #5413 row-subquery UPDATE: verifies the query is
		// "reasonably optimized" for each provider family the shape lift reaches.
		//
		// Common invariant across all supported providers: the two inner tables (UpdatedEntities,
		// UpdateRelation) appear exactly once each — the #5413 bug added a duplicate `FROM
		// NewEntities x_1` reference inside the subquery; absence of that is the core regression
		// check. The UPDATE target (NewEntities) count varies because Oracle aliases the target
		// (u1) while PostgreSQL emits the bare identifier everywhere.
		void AssertRowUpdateOptimized(string context)
		{
			switch (context)
			{
				case string when context.IsAnyOf(TestProvName.AllOracle):
					// Oracle aliases the UPDATE target — NewEntities itself appears only once
					// (in the UPDATE clause). Strictest form of the assertion.
					LastQuery!.ShouldContain("NewEntities",     Exactly.Once());
					LastQuery!.ShouldContain("UpdatedEntities", Exactly.Once());
					LastQuery!.ShouldContain("UpdateRelation",  Exactly.Once());
					LastQuery!.ShouldNotContain("EXISTS");
					break;

				case string when context.IsAnyOf(TestProvName.AllPostgreSQL):
					// PostgreSQL emits the UPDATE target unaliased, so NewEntities appears
					// multiple times by name (UPDATE, inner correlation, outer WHERE). Only the
					// inner tables must be exactly once each.
					LastQuery!.ShouldContain("UpdatedEntities", Exactly.Once());
					LastQuery!.ShouldContain("UpdateRelation",  Exactly.Once());
					LastQuery!.ShouldNotContain("EXISTS");
					break;

				case string when context.IsAnyOf(TestProvName.AllSQLite):
					// SQLite routes through GetAlternativeUpdatePostgreSqlite — different shape,
					// but still no EXISTS fallback.
					LastQuery!.ShouldNotContain("EXISTS");
					break;

				// Informix/Firebird5 fall through: Informix throws (see ThrowsForProvider on the
				// First/FirstOrDefault methods); Firebird5 no longer throws thanks to the
				// projection-column fix in FlattenRowConstructors, but its SQL shape differs
				// enough that a shape assertion isn't meaningful here.
			}
		}

		[Test]
		public void UpdateFromSubqueryMultipleRowSetters([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllOracle, TestProvName.AllPostgreSQL, TestProvName.AllInformix, TestProvName.AllFirebird5Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var table1 = db.CreateLocalTable<UpdatedEntities>();
			using var table2 = db.CreateLocalTable<UpdateRelation>();

			table1
				.Where(u => u.id == 1)
				.Set(
					u => Sql.Row(u.Value1, u.Value2),
					u => (from r in table2 where r.id == u.RelationId select Sql.Row(r.RelatedValue1, r.RelatedValue2)).Single())
				.Set(
					u => Sql.Row(u.Value3, u.RelationId),
					u => (from r in table2 where r.id == u.RelationId select Sql.Row(r.RelatedValue3, (int?)r.id)).Single())
				.Update();

			LastQuery!.ShouldNotContain("EXISTS");
		}

		[Test]
		public void UpdateFromSubqueryMixedRowAndScalar([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllOracle, TestProvName.AllPostgreSQL, TestProvName.AllInformix, TestProvName.AllFirebird5Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var table1 = db.CreateLocalTable<UpdatedEntities>();
			using var table2 = db.CreateLocalTable<UpdateRelation>();

			table1
				.Where(u => u.id == 1)
				.Set(
					u => Sql.Row(u.Value1, u.Value2),
					u => (from r in table2 where r.id == u.RelationId select Sql.Row(r.RelatedValue1, r.RelatedValue2)).Single())
				.Set(
					u => u.Value3,
					42)
				.Update();

			LastQuery!.ShouldNotContain("EXISTS");
			LastQuery!.ShouldContain("Value3", AtLeast.Once());
		}

		[Test]
		public void UpdateFromSubqueryRowOnPostgreSQL([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			using var table1 = db.CreateLocalTable<NewEntities>();
			using var table2 = db.CreateLocalTable<UpdatedEntities>();
			using var table3 = db.CreateLocalTable<UpdateRelation>();

			table1
				.Where(u1 => u1.id == 7)
				.Set(
					u1 => Sql.Row(u1.Value1, u1.Value2),
					u1 => (
						from c in db.SelectQuery(() => u1.Value3 + 10)
						from n2 in table2.LeftJoin(n2 => n2.id == c)
						from n3 in table3.LeftJoin(n3 => n2.RelationId == n3.id)
						where n3.RelatedValue3 < 1000
						select Sql.Row(n2.Value1, n3.RelatedValue2))
						.Single()
				)
				.Update();

			// PG supports subquery-row natively — the lifted SelectQuery stays as
			// `SET ("Value1", "Value2") = (SELECT ...)`.
			LastQuery!.ShouldContain("(\"Value1\", \"Value2\")", AtLeast.Once());
		}

		[Test]
		public void UpdateFromSubqueryRowFlattened([IncludeDataSources(TestProvName.AllFirebird5Plus, TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			using var sourceTable = db.CreateLocalTable<UpdateSubquerySourceTable>();

			sourceTable
				.Where(x => x.Id == 1)
				.Set(
					x => Sql.Row(x.FirstName, x.LastName),
					x => (from t in sourceTable where t.Id == x.Id + 1 select Sql.Row(t.FirstName, t.LastName)).Single()
				)
				.Update();

			// Firebird5 / Sybase lack RowFeature.Update and RowFeature.UpdateLiteral — the lift
			// output must be flattened into individual `<col> = <value>` setters. The row-form
			// signature `(<cols>) = (<rhs>)` has a distinctive `) =` segment between the column
			// list and the rhs — that must not appear.
			LastQuery!.ShouldNotContain(") =");
		}
	}
}
