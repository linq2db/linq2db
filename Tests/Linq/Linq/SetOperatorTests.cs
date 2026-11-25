using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class SetOperatorTests : TestBase
	{
		[Table]
		sealed class SampleData
		{
			[PrimaryKey]
			[Column] public int Id     { get; set; }
			[Column] public int Value1 { get; set; }
			[Column] public int Value2 { get; set; }
			[Column] public int Value3 { get; set; }
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void TestExcept([DataSources] string context)
		{
			var isDistinct = !context.IsAnyOf(TestProvName.AllClickHouse);

			var testData = GenerateTestData();
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);
			var rs1 = table.Where(t => t.Id % 2 == 0);
			rs1 = rs1.Concat(rs1);
			var rs2 = table.Where(t => t.Id % 4 == 0);
			var query = rs1.Except(rs2);

			var e1 = testData.Where(t => t.Id % 2 == 0);
			e1 = e1.Concat(e1);
			var e2 = testData.Where(t => t.Id % 4 == 0);
			var expected = e1.Except(e2).ToArray();

			if (!isDistinct)
				query = query.Distinct();

			var actual = query.ToArray();

			AreEqual(expected, actual, ComparerBuilder.GetEqualityComparer<SampleData>());
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void TestExceptAll([DataSources(TestProvName.AllClickHouse)] string context)
		{
			var testData = GenerateTestData();
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);
			var rs1 = table.Where(t => t.Id % 2 == 0);
			rs1 = rs1.Concat(rs1);
			var rs2 = table.Where(t => t.Id % 4 == 0);
			var query = rs1.ExceptAll(rs2);

			var e1 = testData.Where(t => t.Id % 2 == 0);
			e1 = e1.Concat(e1);
			var e2 = testData.Where(t => t.Id % 4 == 0);
			var expected = e1.Where(e => !e2.Contains(e, ComparerBuilder.GetEqualityComparer<SampleData>())).ToArray();
			var actual = query.ToArray();

			// TODO: emulation is not correct, but pgsql and mysql native implementation working properly
			if (!context.IsAnyOf(TestProvName.AllPostgreSQL, TestProvName.AllMySql8Plus, TestProvName.AllDuckDB))
				AreEqual(expected, actual, ComparerBuilder.GetEqualityComparer<SampleData>());
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void TestIntersectAll([DataSources(TestProvName.AllClickHouse)] string context)
		{
			var testData = GenerateTestData();
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);
			var rs1 = table.Where(t => t.Id % 2 == 0);
			rs1 = rs1.Concat(rs1);
			var rs2 = table.Where(t => t.Id % 4 == 0);
			var query = rs1.IntersectAll(rs2);

			var e1 = testData.Where(t => t.Id % 2 == 0);
			e1 = e1.Concat(e1);
			var e2 = testData.Where(t => t.Id % 4 == 0);
			var expected = e1.Where(e => e2.Contains(e, ComparerBuilder.GetEqualityComparer<SampleData>())).ToArray();
			var actual = query.ToArray();

			// TODO: emulation is not correct, but pgsql and mysql native implementation working properly
			if (!context.IsAnyOf(TestProvName.AllPostgreSQL, TestProvName.AllMySql8Plus, TestProvName.AllDuckDB))
				AreEqual(expected, actual, ComparerBuilder.GetEqualityComparer<SampleData>());
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void TestExceptProjection([DataSources] string context)
		{
			var testData = GenerateTestData();
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);
			var rs1 = table.Select(r => new { r.Id, Value = r.Value1 }).Where(t => t.Id % 2 == 0);
			var rs2 = table.Select(r => new { r.Id, Value = r.Value2 / 10 }).Where(t => t.Id % 4 == 0);
			var rs3 = table.Select(r => new { r.Id, Value = r.Value1 }).Where(t => t.Id % 6 == 0);
			var query = rs1.Except(rs2).Except(rs3);

			var e1 = testData.Select(r => new { r.Id, Value = r.Value1 }).Where(t => t.Id % 2 == 0);
			var e2 = testData.Select(r => new { r.Id, Value = r.Value2 / 10 }).Where(t => t.Id % 4 == 0);
			var e3 = testData.Select(r => new { r.Id, Value = r.Value1 }).Where(t => t.Id % 6 == 0);
			var expectedQuery = e1.Except(e2).Except(e3);

			var actual   = query.Select(r => new { r.Value }).ToArray();
			var expected = expectedQuery.Select(r => new { r.Value }).ToArray();

			AreEqual(expected, actual);
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void TestIntersect([DataSources] string context)
		{
			var isDistinct = !context.IsAnyOf(TestProvName.AllClickHouse);

			var testData = GenerateTestData();
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);
			var rs1 = table.Where(t => t.Id % 2 == 0);
			rs1 = rs1.Concat(rs1);
			var rs2 = table.Where(t => t.Id % 4 == 0);
			var query = rs1.Intersect(rs2);

			var e1 = testData.Where(t => t.Id % 2 == 0);
			e1 = e1.Concat(e1);
			var e2 = testData.Where(t => t.Id % 4 == 0);
			var expected = e1.Intersect(e2).ToArray();

			if (!isDistinct)
				query = query.Distinct();

			var actual = query.ToArray();

			AreEqual(expected, actual, ComparerBuilder.GetEqualityComparer<SampleData>());
		}

		[Test]
		public void TestUnionAll([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var testData = GenerateTestData();
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);
			var rs1 = table.Where(t => t.Id % 2 == 0);
			var rs2 = table.Where(t => t.Id % 4 == 0);
			var query = rs1.UnionAll(rs2);

			var e1 = testData.AsQueryable().Where(t => t.Id % 2 == 0);
			var e2 = testData.AsQueryable().Where(t => t.Id % 4 == 0);
			var expected = e1.UnionAll(e2).ToArray();
			var actual = query.ToArray();

			AreEqual(expected, actual, ComparerBuilder.GetEqualityComparer<SampleData>());
		}

		[Test]
		public void TestUnionAllExpr([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var testData = GenerateTestData();
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);
			var query = from s in table
						from u in table.Where(t => t.Id % 2 == 0).UnionAll(table.Where(t => t.Id % 4 == 0))
						.InnerJoin(u => u.Id == s.Id)
						select u;

			var e1 = testData.AsQueryable().Where(t => t.Id % 2 == 0);
			var e2 = testData.AsQueryable().Where(t => t.Id % 4 == 0);
			var expected = e1.UnionAll(e2).ToArray();
			var actual = query.ToArray();

			AreEqual(expected, actual, ComparerBuilder.GetEqualityComparer<SampleData>());
		}

		private SampleData[] GenerateTestData()
		{
			return Enumerable.Range(1, 10)
				.Select(i => new SampleData
				{
					Id = i,
					Value1 = i * 10,
					Value2 = i * 100,
					Value3 = i * 1000
				})
				.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3132")]
		public async Task ManyUnionAllOfGroupedQuery([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var query1 = db.Person
					.Where(x => x.MiddleName != null)
					.GroupBy(x => x.MiddleName)
					.Select(grp => new
					{
						grp.Key,
						Count = grp.Count()
					});

			var unionResult = await query1
					.UnionAll(query1).UnionAll(query1).UnionAll(query1).UnionAll(query1).UnionAll(query1)
					.UnionAll(query1).UnionAll(query1).UnionAll(query1).UnionAll(query1).UnionAll(query1)
					.UnionAll(query1).UnionAll(query1).UnionAll(query1).UnionAll(query1).UnionAll(query1)
					.UnionAll(query1).UnionAll(query1).UnionAll(query1).UnionAll(query1).UnionAll(query1)
					.ToArrayAsync();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1412")]
		public void UnionOfTableNamesGroupJoinedToTypeMatchesClientResult([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var typeId = TestData.Guid1;

			var resources = new[]{ new UnionedResourceType{Depth = 256, Height = 110, Id = typeId, Name = "Resource Name", ShortName = "RN", Width = 333 } };

			var carriersA = new[] { new UnionedLoadCarrier { Id = TestData.Guid2, TypeID = typeId } };
			var carriersB = new[] { new UnionedLoadCarrier { Id = TestData.Guid3, TypeID = typeId } };

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<UnionedResourceType>(resources))
			using (db.CreateLocalTable<UnionedLoadCarrier>(carriersA))
			using (db.CreateLocalTable<UnionedLoadCarrier>("UnionedLoadCarrierB", carriersB))
			{
				var qryUnion = from res in db.GetTable<UnionedLoadCarrier>().Union(db.GetTable<UnionedLoadCarrier>().TableName("UnionedLoadCarrierB"))
					join type in db.GetTable<UnionedResourceType>() on res.TypeID equals type.Id into tpList
					from tp in tpList.DefaultIfEmpty()
					select new UnionedLoadCarrierWithType { LoadCarrier = res, ResourceType = tp };

				var staticResult = from res in carriersA.Union(carriersB)
					join type in resources on res.TypeID equals type.Id into tpList
					from tp in tpList.DefaultIfEmpty()
					select new UnionedLoadCarrierWithType { LoadCarrier = res, ResourceType = tp };

				var actual   = qryUnion.ToArray();
				var expected = staticResult.ToArray();

				AreEqualWithComparer(expected, actual);
			}
		}

		public class CarrierDtoWithId : CarrierDtoRoot
		{
			public Guid Id { get; set; }
		}

		public class CarrierDtoWithExtension : CarrierDtoWithId;

		public class CarrierDtoRoot;

		public abstract class CarrierDtoWms<T> : CarrierDtoNoCustomFields<T>;

		public abstract class CarrierDtoNoCustomFields<T> : CarrierDtoWithExtension;

		public enum CarrierResourceStatus
		{
			Undefined = 0,
			Captured = 10
		}

		public class UnionedLoadCarrier : CarrierDtoWms<UnionedLoadCarrier>
		{
			public CarrierResourceStatus Status { get; set; }

			public string? ResourceLabel { get; set; }

			public string? ResourceLabelNVE { get; set; }

			public Guid? ParentResourceID { get; set; }

			public Guid? TypeID { get; set; }

			public int? HeightClass { get; set; }

			public Decimal? CurrentWeightOfResource { get; set; }

			public int? WidthClass { get; set; }

			public int? LengthClass { get; set; }

			public Guid? OriginalResourceID { get; set; }

			public Guid? LastGlobalTaskID { get; set; }

			public DateTime? WashingDate { get; set; }

			public Guid? ResourcePointID { get; set; }

			public decimal? Height { get; set; }

			public decimal? Width { get; set; }

			public decimal? Length { get; set; }

			public string? TechnicalValues { get; set; }

			public int RearrangementCount { get; set; }

			public bool IsVirtual { get; set; }

			public string? ErrorMessage { get; set; }

			public decimal? FillingDegree { get; set; }

			public DateTime? LastInventoryCheckTimeStamp { get; set; }

			public string? Segmentation { get; set; }

			public bool DontTouch { get; set; }

			protected bool Equals(UnionedLoadCarrier other)
			{
				return Id.Equals(other.Id) && TypeID.Equals(other.TypeID);
			}

			public override bool Equals(object? obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != GetType()) return false;
				return Equals((UnionedLoadCarrier)obj);
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(Id, TypeID);
			}
		}

		public class UnionedLoadCarrierWithType 
		{
			public UnionedLoadCarrier? LoadCarrier { get; set; }

			public UnionedResourceType? ResourceType { get; set; }
		}

		public class UnionedResourceType : CarrierDtoWithExtension
		{
			public string? Name { get; set; }

			public string? ShortName { get; set; }

			public int Height { get; set; }

			public int Depth { get; set; }

			public int Width { get; set; }

			protected bool Equals(UnionedResourceType other)
			{
				return Id.Equals(other.Id) && string.Equals(Name, other.Name) && string.Equals(ShortName, other.ShortName) && Height == other.Height && Depth == other.Depth && Width == other.Width;
			}

			public override bool Equals(object? obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != GetType()) return false;
				return Equals((UnionedResourceType)obj);
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(
					Id,
					Name,
					ShortName,
					Height,
					Depth,
					Width
				);
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2619")]
		public void OrderByDroppedInUnion([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var persons = db.Person
				.OrderBy (c => c.LastName);

			var union = persons
				.Union (persons);

			var sql = union.ToSqlQuery().Sql;

			sql.ShouldNotContain("ORDER");

			union.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2619")]
		public void OrderByKeptWithTakeInUnion([DataSources(TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			var persons = db.Person
				.OrderBy (c => c.LastName).Take(1);

			var union = persons
				.Union (persons);

			var sql = union.ToSqlQuery().Sql;

			sql.ShouldContain("ORDER", Exactly.Twice());

			union.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2619")]
		public void OrderByKeptInConcat([DataSources(ProviderName.SqlCe, TestProvName.AllSqlServer, TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			var persons = db.Person
				.OrderBy (c => c.LastName);

			var concat = persons
				.Concat(persons);

			var sql = concat.ToSqlQuery().Sql;

			sql.ShouldContain("ORDER", Exactly.Twice());

			concat.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2619")]
		public void OrderByKeptWithTakeInConcat([DataSources(TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			var persons = db.Person
				.OrderBy (c => c.LastName).Take(1);

			var concat = persons
				.Concat(persons);

			var sql = concat.ToSqlQuery().Sql;

			sql.ShouldContain("ORDER", Exactly.Twice());

			concat.ToArray();
		}

		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2619")]
		public void OrderByDroppedInExcept([DataSources(TestProvName.AllSybase, TestProvName.AllSqlServer, TestProvName.AllNativeAccess)] string context)
		{
			using var db = GetDataContext(context);
			var persons = db.Person
				.OrderBy (c => c.LastName);

			var concat = persons
				.Except(persons);

			var sql = concat.ToSqlQuery().Sql;

			if (!sql.Contains("EXISTS"))
				sql.ShouldNotContain("ORDER");

			concat.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2619")]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void OrderByKeptWithTakeInExcept([DataSources(TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			var persons = db.Person
				.OrderBy (c => c.LastName)
				.Take(1);

			var except = persons
				.Except(persons);

			var sql = except.ToSqlQuery().Sql;

			sql.ShouldContain("ORDER");

			except.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/395")]
		public void UnionedProjectionsGroupedBySum([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var first  = db.Order.Where(x => x.ShipVia == 1).Select(x => new {x.ShipCountry, Via1 = x.Freight, Via2 = 0M,        Via3 = 0M});
			var second = db.Order.Where(x => x.ShipVia == 2).Select(x => new {x.ShipCountry, Via1 = 0M,        Via2 = x.Freight, Via3 = 0M});
			var third  = db.Order.Where(x => x.ShipVia == 3).Select(x => new {x.ShipCountry, Via1 = 0M,        Via2 = 0M,        Via3 = x.Freight});

			var allData = first.Union(second).Union(third)
					.GroupBy(x => x.ShipCountry)
					.Select(x => new
					{
						ShipCountry = x.Key,
						Via1 = x.Select(y => y.Via1).Sum(),
						Via2 = x.Select(y => y.Via2).Sum(),
						Via3 = x.Select(y => y.Via3).Sum(),
					});

			var data = allData.First();
			using (Assert.EnterMultipleScope())
			{
				// ok
				Assert.That(data.Via1,
							Is.EqualTo(first.Where(x => x.ShipCountry == data.ShipCountry).Sum(x => x.Via1)),
							"first aggregation sum mismatch");

				// ok
				Assert.That(data.Via2,
							Is.EqualTo(second.Where(x => x.ShipCountry == data.ShipCountry).Sum(x => x.Via2)),
							"second aggregation sum mismatch");

				// asserts
				Assert.That(data.Via3,
							Is.EqualTo(third.Where(x => x.ShipCountry == data.ShipCountry).Sum(x => x.Via3)),
							"third aggregation sum mismatch");
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5684")]
		public void NestedUnionAllWithOrderBy([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(NestedUnionRow.Data);

			var query1 = table.Where(x => x.IsVisible);
			var query2 = table.Where(x => !x.IsVisible);
			var query3 = table.Where(x => x.Name.Length > 10);
			var query4 = table.Where(x => x.Name.Length < 1000);

			var union1 = query1.UnionAll(query2);
			var union2 = query3.UnionAll(query4);

			var unionAll = union1.UnionAll(union2);

			var ordered = unionAll.OrderByDescending(x => x.Name);

			var result = ordered.ToList();

			// Expected multiset via LINQ to Objects (order-independent: OrderBy(Name) is not a stable key with duplicates).
			var expected = NestedUnionRow.Data.Where(x => x.IsVisible)
				.Concat(NestedUnionRow.Data.Where(x => !x.IsVisible))
				.Concat(NestedUnionRow.Data.Where(x => x.Name.Length > 10))
				.Concat(NestedUnionRow.Data.Where(x => x.Name.Length < 1000))
				.Select(x => (x.Id, x.Name, x.IsVisible))
				.OrderBy(x => x.Id)
				.ToList();

			result.Select(x => (x.Id, x.Name, x.IsVisible))
				.OrderBy(x => x.Id)
				.ShouldBe(expected);
		}

		[Table]
		sealed class NestedUnionRow
		{
			[Column, PrimaryKey] public int    Id        { get; set; }
			[Column]             public string Name      { get; set; } = null!;
			[Column]             public bool   IsVisible { get; set; }

			public static readonly NestedUnionRow[] Data =
			{
				new() { Id = 1, Name = "Alpha", IsVisible = true  },
				new() { Id = 2, Name = "Beta",  IsVisible = false },
				new() { Id = 3, Name = "Gamma", IsVisible = true  },
			};
		}
	}
}
