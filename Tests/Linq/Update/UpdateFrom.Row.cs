using System.Linq;

using LinqToDB;
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5413")]
		public void UpdateFromSubqueryRowShouldBeOptimized([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllOracle, TestProvName.AllPostgreSQL, TestProvName.AllInformix, TestProvName.AllFirebird5Plus)] string context)
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

		[Test(Description = "Row-setter with mixed literal + correlated values (regression for ProcessUpdateItemsWithRows column/value pairing)")]
		public void UpdateFromSubqueryRowMixedIndependentAndDependent([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllOracle, TestProvName.AllPostgreSQL, TestProvName.AllFirebird5Plus)] string context)
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5413")]
		public void UpdateFromSubqueryRowShouldRemainSimple([IncludeDataSources(TestProvName.AllOracle)] string context)
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

			LastQuery!.ShouldContain("\"NewEntities\"",     Exactly.Once());
			LastQuery!.ShouldContain("\"UpdatedEntities\"", Exactly.Once());
			LastQuery!.ShouldContain("\"UpdateRelation\"",  Exactly.Once());
			LastQuery!.ShouldNotContain("EXISTS");
		}

		[Test(Description = "#5413 — .SingleOrDefault() variant (OuterApply source path)")]
		public void UpdateFromSubqueryRowSingleOrDefault([IncludeDataSources(TestProvName.AllOracle)] string context)
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

			LastQuery!.ShouldContain("\"NewEntities\"",     Exactly.Once());
			LastQuery!.ShouldContain("\"UpdatedEntities\"", Exactly.Once());
			LastQuery!.ShouldContain("\"UpdateRelation\"",  Exactly.Once());
			LastQuery!.ShouldNotContain("EXISTS");
		}

		[Test(Description = "#5413 — .First() variant (non-weak, CrossApply source path)")]
		public void UpdateFromSubqueryRowFirst([IncludeDataSources(TestProvName.AllOracle)] string context)
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

			LastQuery!.ShouldContain("\"NewEntities\"",     Exactly.Once());
			LastQuery!.ShouldContain("\"UpdatedEntities\"", Exactly.Once());
			LastQuery!.ShouldContain("\"UpdateRelation\"",  Exactly.Once());
			LastQuery!.ShouldNotContain("EXISTS");
		}

		[Test(Description = "#5413 — .FirstOrDefault() variant")]
		public void UpdateFromSubqueryRowFirstOrDefault([IncludeDataSources(TestProvName.AllOracle)] string context)
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

			LastQuery!.ShouldContain("\"NewEntities\"",     Exactly.Once());
			LastQuery!.ShouldContain("\"UpdatedEntities\"", Exactly.Once());
			LastQuery!.ShouldContain("\"UpdateRelation\"",  Exactly.Once());
			LastQuery!.ShouldNotContain("EXISTS");
		}

		[Test(Description = "#5413 — two row-expression subquery setters in a single UPDATE, both lifted")]
		public void UpdateFromSubqueryMultipleRowSetters([IncludeDataSources(TestProvName.AllOracle)] string context)
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

		[Test(Description = "#5413 — mix of row-expression subquery setter and scalar setter in one UPDATE")]
		public void UpdateFromSubqueryMixedRowAndScalar([IncludeDataSources(TestProvName.AllOracle)] string context)
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
			LastQuery!.ShouldContain("\"Value3\"", AtLeast.Once());
		}

		[Test(Description = "#5413 — row-expression subquery setter on PostgreSQL (provider has RowFeature.Update, native subquery form)")]
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

		[Test(Description = "#5413 — row-expression subquery setter on providers without any Row support: the row constructor must be eliminated from the emitted SQL (flattened into individual setters).")]
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
