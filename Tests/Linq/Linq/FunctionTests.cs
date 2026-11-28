using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

// ReSharper disable UnusedMember.Local

namespace Tests.Linq
{
	[TestFixture]
	public class FunctionTests : TestBase
	{
		[Test]
		public void Contains1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent where new[] { 1, 2 }.Contains(p.ParentID) select p,
				from p in db.Parent where new[] { 1, 2 }.Contains(p.ParentID) select p);
		}

		[Test]
		public void Contains2([DataSources] string context)
		{
			var arr = new[] { 1, 2 };

			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent where arr.Contains(p.ParentID) select p,
				from p in db.Parent where arr.Contains(p.ParentID) select p);
		}

		[Test]
		public void Contains3([DataSources] string context)
		{
			var n = 2;

			using var data = GetDataContext(context);
			AreEqual(
				from p in Parent
				where new[] { 1, n }.Contains(p.ParentID)
				select p,
				CompiledQuery.Compile<ITestDataContext, IQueryable<Parent>>(db =>
					from p in db.Parent
					where new[] { 1, n }.Contains(p.ParentID)
					select p)(data));
		}

		[Test]
		public void Contains4([DataSources] string context)
		{
			var arr = new[] { 1, 2 };

			using var data = GetDataContext(context);
			AreEqual(
				from p in Parent
				where arr.Contains(p.ParentID)
				select p,
				CompiledQuery.Compile<ITestDataContext, IQueryable<Parent>>(db =>
					from p in db.Parent
					where arr.Contains(p.ParentID)
					select p)(data));
		}

		[Test]
		public void Contains5([DataSources] string context)
		{
			var arr1 = new[] { 1, 2 };
			var arr2 = new[] { 1, 2, 4 };

			var expected1 = from p in Parent where arr1.Contains(p.ParentID) select p;
			var expected2 = from p in Parent where arr2.Contains(p.ParentID) select p;

			using var data = GetDataContext(context);
			var cq = CompiledQuery.Compile<ITestDataContext,int[],IQueryable<Parent>>((db,a) =>
					from p in db.Parent
					where a.Contains(p.ParentID)
					select p);

			AreEqual(expected1, cq(data, arr1));
			AreEqual(expected2, cq(data, arr2));
		}

		[Test]
		public void Contains6([DataSources] string context)
		{
			var arr = new List<int> { 1, 2 };

			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent where arr.Contains(p.ParentID) select p,
				from p in db.Parent where arr.Contains(p.ParentID) select p);
		}

		[Test]
		public void Contains7([DataSources] string context)
		{
			IEnumerable<int> arr = new[] { 1, 2 };

			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent where arr.Contains(p.ParentID) select p,
				from p in db.Parent where arr.Contains(p.ParentID) select p);
		}

		[Test]
		public void ContainsKey1([DataSources] string context)
		{
			var arr = new Dictionary<int,int>
			{
				{ 1, 1 },
				{ 2, 2 },
			};

			using var db = GetDataContext(context);
			AreEqual(
#pragma warning disable CA1841 // Prefer Dictionary.Contains methods : suppressed as we test this method
				from p in Parent where arr.Keys.Contains(p.ParentID) select p,
				from p in db.Parent where arr.Keys.Contains(p.ParentID) select p);
#pragma warning restore CA1841 // Prefer Dictionary.Contains methods
		}

		[Test]
		public void ContainsKey2([DataSources] string context)
		{
			var arr = new Dictionary<int,int>
			{
				{ 1, 1 },
				{ 2, 2 },
			};

			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent where arr.ContainsKey(p.ParentID) select p,
				from p in db.Parent where arr.ContainsKey(p.ParentID) select p);
		}

		[Test]
		public void ContainsValue1([DataSources] string context)
		{
			var arr = new Dictionary<int,int>
			{
				{ 1, 1 },
				{ 2, 2 },
			};

			using var db = GetDataContext(context);
			AreEqual(
#pragma warning disable CA1841 // Prefer Dictionary.Contains methods : suppressed as we test this method
				from p in Parent where arr.Values.Contains(p.ParentID) select p,
				from p in db.Parent where arr.Values.Contains(p.ParentID) select p);
#pragma warning restore CA1841 // Prefer Dictionary.Contains methods
		}

		[Test]
		public void ContainsValue2([DataSources] string context)
		{
			var arr = new Dictionary<int,int>
			{
				{ 1, 1 },
				{ 2, 2 },
			};

			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent where arr.ContainsValue(p.ParentID) select p,
				from p in db.Parent where arr.ContainsValue(p.ParentID) select p);
		}

		[Test]
		public void ContainsKey21([DataSources] string context)
		{
			IDictionary<int,int> arr = new Dictionary<int,int>
			{
				{ 1, 1 },
				{ 2, 2 },
			};

			using var db = GetDataContext(context);
			AreEqual(
#pragma warning disable CA1841 // Prefer Dictionary.Contains methods : suppressed as we test this method
				from p in Parent where arr.Keys.Contains(p.ParentID) select p,
				from p in db.Parent where arr.Keys.Contains(p.ParentID) select p);
#pragma warning restore CA1841 // Prefer Dictionary.Contains methods
		}

		[Test]
		public void ContainsKey22([DataSources] string context)
		{
			IDictionary<int,int> arr = new Dictionary<int,int>
			{
				{ 1, 1 },
				{ 2, 2 },
			};

			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent where arr.ContainsKey(p.ParentID) select p,
				from p in db.Parent where arr.ContainsKey(p.ParentID) select p);
		}

		[Test]
		public void ContainsValue21([DataSources] string context)
		{
			IDictionary<int,int> arr = new Dictionary<int,int>
			{
				{ 1, 1 },
				{ 2, 2 },
			};

			using var db = GetDataContext(context);
			AreEqual(
#pragma warning disable CA1841 // Prefer Dictionary.Contains methods : suppressed as we test this method
				from p in Parent where arr.Values.Contains(p.ParentID) select p,
				from p in db.Parent where arr.Values.Contains(p.ParentID) select p);
#pragma warning restore CA1841 // Prefer Dictionary.Contains methods
		}

		[Test]
		public void ContainsKey31([DataSources] string context)
		{
			IReadOnlyDictionary<int,int> arr = new Dictionary<int,int>
			{
				{ 1, 1 },
				{ 2, 2 },
			};

			using var db = GetDataContext(context);
			AreEqual(
#pragma warning disable CA1841 // Prefer Dictionary.Contains methods : suppressed as we test this method
				from p in Parent where arr.Keys.Contains(p.ParentID) select p,
				from p in db.Parent where arr.Keys.Contains(p.ParentID) select p);
#pragma warning restore CA1841 // Prefer Dictionary.Contains methods
		}

		[Test]
		public void ContainsKey32([DataSources] string context)
		{
			IReadOnlyDictionary<int,int> arr = new Dictionary<int,int>
			{
				{ 1, 1 },
				{ 2, 2 },
			};

			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent where arr.ContainsKey(p.ParentID) select p,
				from p in db.Parent where arr.ContainsKey(p.ParentID) select p);
		}

		[Test]
		public void ContainsValue31([DataSources] string context)
		{
			IReadOnlyDictionary<int,int> arr = new Dictionary<int,int>
			{
				{ 1, 1 },
				{ 2, 2 },
			};

			using var db = GetDataContext(context);
			AreEqual(
#pragma warning disable CA1841 // Prefer Dictionary.Contains methods : suppressed as we test this method
				from p in Parent where arr.Values.Contains(p.ParentID) select p,
				from p in db.Parent where arr.Values.Contains(p.ParentID) select p);
#pragma warning restore CA1841 // Prefer Dictionary.Contains methods
		}

		[Test]
		public void ContainsHashSet1([DataSources] string context)
		{
			var arr = new HashSet<int> { 1, 2 };

			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent where arr.Contains(p.ParentID) select p,
				from p in db.Parent where arr.Contains(p.ParentID) select p);
		}

#if NET8_0_OR_GREATER
		[Test]
		public void ContainsReadOnlySet([DataSources] string context)
		{
			IReadOnlySet<int> arr = new HashSet<int> { 1, 2 };

			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent where arr.Contains(p.ParentID) select p,
				from p in db.Parent where arr.Contains(p.ParentID) select p);
		}
#endif

		[Test]
		public void EmptyContains1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent
				where Array.Empty<int>().Contains(p.ParentID) || p.ParentID == 2
				select p,
				from p in db.Parent
				where Array.Empty<int>().Contains(p.ParentID) || p.ParentID == 2
				select p);
		}

		[Test]
		public void ContainsString11([DataSources] string context)
		{
			var arr = new List<string> { "John" };

			using var db = GetDataContext(context);
			AreEqual(
				from p in Person where arr.Contains(p.FirstName) select p,
				from p in db.Person where arr.Contains(p.FirstName) select p);
		}

		[Test]
		public void ContainsString12([DataSources] string context)
		{
			var nm = "John";

			using var db = GetDataContext(context);
			AreEqual(
				from p in Person where new List<string> { nm }.Contains(p.FirstName) select p,
				from p in db.Person where new List<string> { nm }.Contains(p.FirstName) select p);
		}

		[Test]
		public void ContainsString13([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Person where new List<string> { "John" }.Contains(p.FirstName) select p,
				from p in db.Person where new List<string> { "John" }.Contains(p.FirstName) select p);
		}

		[Test]
		public void ContainsString21([DataSources] string context)
		{
			var arr = new[] { "John" };

			using var db = GetDataContext(context);
			AreEqual(
				from p in Person where arr.Contains(p.FirstName) select p,
				from p in db.Person where arr.Contains(p.FirstName) select p);
		}

		[Test]
		public void ContainsString22([DataSources] string context)
		{
			var nm = "John";

			using var db = GetDataContext(context);
			AreEqual(
				from p in Person where new[] { nm }.Contains(p.FirstName) select p,
				from p in db.Person where new[] { nm }.Contains(p.FirstName) select p);
		}

		[Test]
		public void ContainsString23([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Person where new[] { "John" }.Contains(p.FirstName) select p,
				from p in db.Person where new[] { "John" }.Contains(p.FirstName) select p);
		}

		[Test]
		public void Equals1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent where p.ParentID.Equals(2) select p,
				from p in db.Parent where p.ParentID.Equals(2) select p);
		}

		[Test]
		public void Equals2([DataSources] string context)
		{
			var child = (from ch in Child where ch.ParentID == 2 select ch).First();

			using var db = GetDataContext(context);
			AreEqual(
				from ch in Child where !ch.Equals(child) select ch,
				from ch in db.Child where !ch.Equals(child) select ch);
		}

		[Test]
		public void Equals3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent where p.Value1.Equals(null) select p,
				from p in db.Parent where p.Value1.Equals(null) select p);
		}

		[Test]
		public void Equals4([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var dd = GetNorthwindAsList(context);
			AreEqual(
				dd.Customer.Where(c => !c.Address!.Equals(null)),
				db.Customer.Where(c => !c.Address!.Equals(null)));
		}

		[Test]
		[NoBaselines("Client-side Guid generation", Configurations = [TestProvName.AllAccessLibRed])]
		public void NewGuid1(
			[DataSources(
				ProviderName.DB2,
				TestProvName.AllYdb,
				TestProvName.AllInformix,
				TestProvName.AllPostgreSQL12Minus,
				TestProvName.AllSQLite,
				TestProvName.AllSapHana,
				TestProvName.AllNativeAccess)]
			string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Types where p.GuidValue != Sql.NewGuid() select p.GuidValue,
				from p in db.Types where p.GuidValue != Sql.NewGuid() select p.GuidValue);
		}

		[Test]
		public void NewGuid2(
			[DataSources(
				ProviderName.DB2,
				TestProvName.AllInformix,
				TestProvName.AllSQLite,
				TestProvName.AllNativeAccess)]
			string context)
		{
			using var db = GetDataContext(context);
			Assert.That((from p in db.Types select Sql.NewGuid()).First(), Is.Not.EqualTo(Guid.Empty));
		}

		[Test]
		public void NewGuidOrder(
			[DataSources(false,
				ProviderName.DB2,
				TestProvName.AllYdb,
				TestProvName.AllInformix,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				TestProvName.AllSapHana,
				TestProvName.AllAccess)]
			string context)
		{
			using var db = (TestDataConnection)GetDataContext(context);
			var query =
				from p in db.Types
				orderby Sql.NewGuid()
				select p.GuidValue;

			_ = query.ToArray();

			Assert.That(db.LastQuery, Does.Contain("ORDER"));
		}

		// Version nibble of a GUID is the first hex digit of its third group (canonical layout),
		// i.e. index 12 of the dash-less "N" form. Works on every TFM (Guid.Version is net9+ only).
		static int GuidVersion(Guid guid) => Convert.ToInt32(guid.ToString("N").Substring(12, 1), 16);

		// Providers with a native server-side UUIDv7 generator. Sql.AsSql forces the value into SQL,
		// so the emitted function call is deterministic and the baseline captures it — that baseline
		// is the SQL-generation assertion that the right server function is used. The returned value
		// itself is non-deterministic, so only its version is asserted.
		[Test]
		public void NewGuid7_Native([IncludeDataSources(true, TestProvName.AllClickHouse, TestProvName.AllDuckDB, TestProvName.AllMariaDB, TestProvName.AllPostgreSQL18Plus)] string context)
		{
			using var db = GetDataContext(context);

			var guid = (from p in db.Types select Sql.AsSql(Sql.NewGuid7())).First();

			Assert.That(guid, Is.Not.EqualTo(Guid.Empty));
			Assert.That(GuidVersion(guid), Is.EqualTo(7));
		}

		// Providers without a native generator fall back to client-side generation, embedding a
		// random literal in the SQL — non-deterministic, so the baseline is disabled.
		[Test]
		[NoBaselines("Non-deterministic UUIDv7 value")]
		public void NewGuid7_Emulated([DataSources(TestProvName.AllClickHouse, TestProvName.AllDuckDB, TestProvName.AllMariaDB, TestProvName.AllPostgreSQL18Plus)] string context)
		{
			using var db = GetDataContext(context);
			var guid = (from p in db.Types select Sql.AsSql(Sql.NewGuid7())).First();

			Assert.That(guid, Is.Not.EqualTo(Guid.Empty));
			Assert.That(GuidVersion(guid), Is.EqualTo(7));
		}

#if NET9_0_OR_GREATER
		[Test]
		public void CreateVersion7_Native([IncludeDataSources(true, TestProvName.AllClickHouse, TestProvName.AllDuckDB, TestProvName.AllMariaDB, TestProvName.AllPostgreSQL18Plus)] string context)
		{
			using var db = GetDataContext(context);

			var guid = (from p in db.Types select Sql.AsSql(Guid.CreateVersion7())).First();

			Assert.That(guid, Is.Not.EqualTo(Guid.Empty));
			Assert.That(guid.Version, Is.EqualTo(7));
		}

		[Test]
		[NoBaselines("Non-deterministic UUIDv7 value")]
		public void CreateVersion7_Emulated([DataSources(TestProvName.AllClickHouse, TestProvName.AllDuckDB, TestProvName.AllMariaDB, TestProvName.AllPostgreSQL18Plus)] string context)
		{
			using var db = GetDataContext(context);
			var guid = (from p in db.Types select Sql.AsSql(Guid.CreateVersion7())).First();

			Assert.That(guid, Is.Not.EqualTo(Guid.Empty));
			Assert.That(guid.Version, Is.EqualTo(7));
		}
#endif

		[Test]
		public void CustomFunc([DataSources] string context)
		{
			Expressions.MapMember<Person>(nameof(CustomFunc), p => p.FullName(), (Expression<Func<Person,string>>)(p => p.LastName + ", " + p.FirstName));

			using var db = GetDataContext(context, new MappingSchema(nameof(CustomFunc)));
			AreEqual(
				from p in Person where p.FullName() == "Pupkin, John" select p.FullName(),
				from p in db.Person where p.FullName() == "Pupkin, John" select p.FullName());
		}

		[Test]
		public void Count1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			Assert.That(
				db.Child.Count(c => c.ParentID == 1), Is.EqualTo(Child.Count(c => c.ParentID == 1)));
		}

		[Test]
		public void Sum1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			Assert.That(
				db.Child.Sum(c => c.ParentID), Is.EqualTo(Child.Sum(c => c.ParentID)));
		}

		[ExpressionMethod("ChildCountExpression")]
		private static int ChildCount(Parent parent)
		{
			throw new ServerSideOnlyException(nameof(ChildCount));
		}

		static Expression ChildCountExpression()
		{
			return
				(Expression<Func<Parent, int>>)
				(p => p.Children.Where(c => c.ParentID > 2).Sum(c => c.ParentID * c.ChildID));
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void Sum2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Parent.Select(p => p.Children.Where(c => c.ParentID > 2).Sum(c => c.ParentID * c.ChildID)),
				db.Parent.Select(p => ChildCount(p)));
		}

		[Test]
		public void CustomAggregate([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent
				group p by p.ParentID into g
				select new
				{
					sum1 = g.Sum(i => i.Value1) ?? 0,
					sum2 = g.Sum(i => i.Value1) ?? 0,
				},
				from p in db.Parent
				group p by p.ParentID into g
				select new
				{
					sum1 = g.Sum(i => i.Value1) ?? 0,
					sum2 = g.MySum(i => i.Value1) ?? 0,
				});
		}

		[Test]
		public void GetValueOrDefault([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent where p.Value1.GetValueOrDefault() > 0 select new { Value = p.Value1.GetValueOrDefault() },
				from p in db.Parent where p.Value1.GetValueOrDefault() > 0 select new { Value = p.Value1.GetValueOrDefault() });
		}

		[Test]
		public void AsNullTest([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p1 in Parent
				from p2 in Parent
				where p1.Value1 == p2.Value1
				select p1,
				from p1 in db.Parent
				from p2 in db.Parent
				where p1.Value1 == p2.Value1
				select p1);
		}

		[Test]
		public void AsNotNullTest([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p1 in Parent
				from p2 in Parent
				where p1.Value1 != null && p1.Value1 == p2.Value1
				select p1,
				from p1 in db.Parent
				from p2 in db.Parent
				where Sql.AsNotNull(p1.Value1) == Sql.AsNotNull(p2.Value1)
				select p1);
		}

		[Test]
		public void Between1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent
				where p.Value1.Between(1, 10)
				select p,
				from p in db.Parent
				where p.Value1.Between(1, 10)
				select p);
		}

		[Test]
		public void Between2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent
				where p.ParentID.Between(1, 10)
				select p,
				from p in db.Parent
				where p.ParentID.Between(1, 10)
				select p);
		}

		[Test]
		public void BetweenOfNullableValueProjection([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				select new
				{
					p.ParentID,
					InRange    = Sql.Between(p.Value1, 1, 3),
					OutOfRange = Sql.NotBetween(p.Value1, 1, 3),
				};

			AssertQuery(query);

			var selectQuery = query.GetSelectQuery();

			selectQuery
				.Find(e => e is SqlPredicate.Between { IsNot: false })
				.ShouldNotBeNull();
			selectQuery
				.Find(e => e is SqlPredicate.Between { IsNot: true })
				.ShouldNotBeNull();
		}

		[Test]
		public void BetweenOfOptionalRowValue([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID into cs
				from c in cs.DefaultIfEmpty()
				where p.ParentID == 5
				select new
				{
					c.ChildID,
					InRange         = Sql.Between(c.ChildID, 0, 100),
					OutOfRange      = Sql.NotBetween(c.ChildID, 0, 100),
					NullableInRange = Sql.Between((int?)c.ChildID, 0, 100),
				};

			// Parent 5 has no children. The non-nullable operand is read as the projection reads ChildID, as 0, the same as
			// `c.ChildID >= 0 && c.ChildID <= 100`; the nullable one keeps the NULL. AssertQuery reads both as 0, so the
			// values are asserted directly.
			var row = query.Single();

			row
				.ChildID
				.ShouldBe(0);
			row
				.InRange
				.ShouldBeTrue();
			row
				.OutOfRange
				.ShouldBeFalse();
			row
				.NullableInRange
				.ShouldBeFalse();
		}

		// Where the call is built is engine behaviour, the same on every provider.
		[Test]
		public void BetweenOfClientValues([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			int  value   = 2;
			int? noValue = null;

			var query =
				from p in db.Parent
				where p.ParentID == 1
				select new
				{
					p.ParentID,
					InRange = Sql.Between(value, 1, 3),
					Negated = !Sql.Between(noValue, 1, 3) ? "a" : "b",
				};

			// Between is server-side only: over client values it is still SQL BETWEEN, where a NULL operand is unknown, so
			// the negation is unknown too and the conditional takes its else branch.
			var row = query.Single();

			row
				.InRange
				.ShouldBeTrue();
			row
				.Negated
				.ShouldBe("b");

			query
				.GetSelectQuery()
				.Find(e => e is SqlPredicate.Between)
				.ShouldNotBeNull();
		}

		[Table]
		sealed class BetweenTargetRow
		{
			[PrimaryKey] public int  Id    { get; set; }
			[Column]     public int  Value { get; set; }
			[Column]     public bool Flag  { get; set; }

			[Column(Length = 1), ValueConverter(ConverterType = typeof(YesNoConverter))]
			public bool YesNo { get; set; }

			[Column(Length = 1), ValueConverter(ConverterType = typeof(YesNoConverter))]
			public bool Marked { get; set; }

			public static readonly BetweenTargetRow[] Data =
			[
				new() { Id = 1, Value = 2 },
				new() { Id = 2, Value = 5 },
			];
		}

		sealed class YesNoConverter() : ValueConverter<bool, string>(b => b ? "Y" : "N", s => s == "Y", false);

		// Which descriptor types the bounds is engine behaviour, and the AST check decides it on any provider.
		[Test]
		public void BetweenProjectedIntoConvertedColumn([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(BetweenTargetRow.Data);

			var low  = 1;
			var high = 3;

			var query =
				from r in table
				orderby r.Id
				select new BetweenTargetRow
				{
					Id     = r.Id,
					Value  = r.Value,
					Flag   = Sql.Between(r.Value, low, high),
					YesNo  = Sql.Between(r.Value, low, high),
					Marked = Sql.NotBetween(r.Value, 1, 3),
				};

			query
				.ToArray()
				.Select(r => new { r.Id, r.Flag, r.YesNo, r.Marked })
				.ShouldBe(new[]
				{
					new { Id = 1, Flag = true,  YesNo = true,  Marked = false },
					new { Id = 2, Flag = false, YesNo = false, Marked = true  },
				});

			// The column the result is assigned to types that result, not the bounds: a constant bound stays a literal rather
			// than becoming a value of the converted column.
			query
				.GetSelectQuery()
				.Find(e => e is SqlPredicate.Between { IsNot: true, Expr2: SqlValue, Expr3: SqlValue })
				.ShouldNotBeNull();
		}

		// Which descriptor types the bounds is engine behaviour, and the SQL check decides it on any provider.
		[Test]
		public void BetweenSetIntoConvertedColumn([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(BetweenTargetRow.Data);

			table
				.Set(r => r.Marked, r => Sql.Between(r.Value, 4, 6))
				.Update();

			// A constant bound stays a literal rather than becoming a value of the column the result is set to.
			GetCurrentBaselines()
				.ShouldContain("BETWEEN 4 AND 6");

			table
				.OrderBy(r => r.Id)
				.Select(r => new { r.Id, r.Marked })
				.ToArray()
				.ShouldBe(new[]
				{
					new { Id = 1, Marked = false },
					new { Id = 2, Marked = true  },
				});
		}

		[Table]
		sealed class BetweenConvertedRow
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(Length = 5), ValueConverter(ConverterType = typeof(PaddedConverter))]
			public int Padded { get; set; }

			[Column(Length = 5), ValueConverter(ConverterType = typeof(PaddedConverter))]
			public int Low { get; set; }

			[Column(Length = 5), ValueConverter(ConverterType = typeof(PaddedConverter))]
			public int High { get; set; }

			// Access has no 64-bit integer and keeps a duration as money.
			[Column(DataType = DataType.Int64)]
			[Column(Configuration = ProviderName.Access, DataType = DataType.Money)]
			[Duration(DurationUnit.Second)]
			public TimeSpan Elapsed { get; set; }

			public static readonly BetweenConvertedRow[] Data =
			[
				new() { Id = 1, Padded = 3,   Low = 1,   High = 4,   Elapsed = TimeSpan.FromSeconds(3)   },
				new() { Id = 2, Padded = 7,   Low = 8,   High = 9,   Elapsed = TimeSpan.FromSeconds(7)   },
				new() { Id = 3, Padded = 40,  Low = 10,  High = 50,  Elapsed = TimeSpan.FromSeconds(40)  },
				new() { Id = 4, Padded = 120, Low = 100, High = 200, Elapsed = TimeSpan.FromSeconds(120) },
			];
		}

		// Stored as a zero-padded string, a value compares in the order of the number only against values in the same form.
		sealed class PaddedConverter() : ValueConverter<int, string>(
			v => v.ToString("D5", CultureInfo.InvariantCulture),
			s => int.Parse(s, CultureInfo.InvariantCulture),
			false);

		[Test]
		public void BetweenOfConvertedColumn([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(BetweenConvertedRow.Data);

			var low     = 5;
			var high    = 50;
			var value   = 42;
			var minimum = TimeSpan.FromSeconds(5);
			var maximum = TimeSpan.FromSeconds(50);

			AssertQuery(
				from r in table
				where Sql.NotBetween(r.Padded, 100, 200)
				select new
				{
					r.Id,
					ConstantBounds  = Sql.Between(r.Padded, 5, 50),
					ParameterBounds = Sql.NotBetween(r.Padded, low, high),
					ColumnBounds    = Sql.Between(value, r.Low, r.High),
					DurationBounds  = Sql.Between(r.Elapsed, minimum, maximum),
				});

			AssertQuery(
				from r in table
				group r by r.Id into g
				where Sql.NotBetween(g.Max(x => x.Padded), 100, 200)
				select new
				{
					g.Key,
					InRange = Sql.Between(g.Max(x => x.Padded), 1, 10),
				});
		}

		// The value is written once, in the form of the one bound that is a converted column.
		[Test]
		public void BetweenOfValueAndOneConvertedBound([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(BetweenConvertedRow.Data);

			var value   = 42;
			var elapsed = TimeSpan.FromSeconds(42);
			// Access sends a Money parameter as text, so two of them compare as text: 10 and 42 order alike either way.
			var minimum = TimeSpan.FromSeconds(10);
			var maximum = TimeSpan.FromSeconds(10);

			int?      nullableValue = 42;
			TimeSpan? longer        = TimeSpan.FromSeconds(150);
			TimeSpan? limit         = TimeSpan.FromSeconds(130);
			TimeSpan? none          = null;

			// Access reads the bounds of a BETWEEN in either order, so only rows whose bounds are in order are asked.
			table
				.Where(r => r.Id <= 2)
				.Where(r => Sql.NotBetween(elapsed, r.Elapsed, maximum))
				.OrderBy(r => r.Id)
				.Select(r => r.Id)
				.ToArray()
				.ShouldBe([1, 2]);

			table
				.Where(r => Sql.NotBetween(longer, r.Elapsed, limit))
				.OrderBy(r => r.Id)
				.Select(r => r.Id)
				.ToArray()
				.ShouldBe([1, 2, 3, 4]);

			table
				.Where(r => Sql.Between(nullableValue, 5, r.High))
				.OrderBy(r => r.Id)
				.Select(r => r.Id)
				.ToArray()
				.ShouldBe([3, 4]);

			// A NULL value leaves each of them unknown. Asked one by one: `A || !A` is reduced to true before it is run.
			table
				.Where(r => Sql.Between(none, r.Elapsed, limit))
				.Select(r => r.Id)
				.ToArray()
				.ShouldBeEmpty();

			table
				.Where(r => Sql.NotBetween(none, r.Elapsed, limit))
				.Select(r => r.Id)
				.ToArray()
				.ShouldBeEmpty();

			table
				.Where(r => !Sql.Between(none, r.Elapsed, limit))
				.Select(r => r.Id)
				.ToArray()
				.ShouldBeEmpty();

			table
				.Where(r => Sql.Between(elapsed, minimum, r.Elapsed))
				.OrderBy(r => r.Id)
				.Select(r => r.Id)
				.ToArray()
				.ShouldBe([4]);

			table
				.Where(r => Sql.Between(value, r.Low, 500))
				.OrderBy(r => r.Id)
				.Select(r => r.Id)
				.ToArray()
				.ShouldBe([1, 2, 3]);

			table
				.Where(r => Sql.Between(value, 5, r.High))
				.OrderBy(r => r.Id)
				.Select(r => r.Id)
				.ToArray()
				.ShouldBe([3, 4]);

			table
				.Where(r => Sql.NotBetween(value, 5, r.High))
				.OrderBy(r => r.Id)
				.Select(r => r.Id)
				.ToArray()
				.ShouldBe([1, 2]);
		}

		[Table]
		sealed class BetweenTextRow
		{
			[PrimaryKey]                                       public int   Id    { get; set; }
			[Column(DataType = DataType.VarChar, Length = 36)] public Guid? Value { get; set; }

			public static readonly BetweenTextRow[] Data =
			[
				new() { Id = 1, Value = new Guid("00000000-0000-0000-0000-000000000001") },
				new() { Id = 2, Value = new Guid("00000000-0000-0000-0000-000000000002") },
				new() { Id = 3, Value = new Guid("00000000-0000-0000-0000-000000000003") },
			];
		}

		// Elsewhere a Guid parameter beside a text column needs a registered conversion, as for `==`; SQLite takes the column's
		// declared type alone, so it is where that type decides the form the bounds are written in.
		[Test]
		public void BetweenOfColumnStoredAsText([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(BetweenTextRow.Data);

			Guid? low  = new Guid("00000000-0000-0000-0000-000000000001");
			Guid? high = new Guid("00000000-0000-0000-0000-000000000002");

			AssertQuery(
				from r in table
				where Sql.NotBetween(r.Value, high, high)
				select new
				{
					r.Id,
					InRange = Sql.Between(r.Value, low, high),
				});
		}

		// With the conversion a Guid stored as text needs as a parameter registered, the bounds are written as text everywhere.
		[Test]
		public void BetweenOfColumnStoredAsTextWithConversion([DataSources] string context)
		{
			var ms = new MappingSchema();
			ms.SetConvertExpression<Guid?, DataParameter>(g => DataParameter.VarChar(null, g.ToString()));
			ms.SetConvertExpression<string, Guid?>(s => Guid.Parse(s));

			using var db    = GetDataContext(context, ms);
			using var table = db.CreateLocalTable(BetweenTextRow.Data);

			Guid? low  = new Guid("00000000-0000-0000-0000-000000000001");
			Guid? high = new Guid("00000000-0000-0000-0000-000000000002");

			AssertQuery(
				from r in table
				where Sql.NotBetween(r.Value, high, high)
				select new
				{
					r.Id,
					InRange = Sql.Between(r.Value, low, high),
				});
		}

		[Table]
		sealed class BetweenDayRow
		{
			[PrimaryKey]                       public int       Id    { get; set; }
			[Column(DataType = DataType.Date)] public DateTime  Day   { get; set; }
			[Column(DataType = DataType.Date)] public DateTime? Other { get; set; }
			[Column]                           public DateTime  Stamp { get; set; }

			public static readonly BetweenDayRow[] Data =
			[
				new() { Id = 1, Day = new DateTime(2020, 1, 1), Stamp = new DateTime(2020, 1, 1, 0, 15, 0) },
				new() { Id = 2, Day = new DateTime(2020, 1, 2), Stamp = new DateTime(2020, 1, 2, 12, 0, 0), Other = new DateTime(2020, 1, 1) },
			];
		}

		// Static fields, so the bounds are written as constants.
		static readonly DateTime BetweenDayLow  = new(2020, 1, 1, 0, 30, 0);
		static readonly DateTime BetweenDayHigh = new(2020, 1, 3, 0, 30, 0);

		// Whatever a provider makes of a bound beside a date column, `Between` answers as `>=` / `<=` do in the same query: beside
		// a stored column, a value computed from one, an optional row's column and a timestamp column, with constant and
		// parameter bounds, in a projection and in a filter.
		[Test]
		public void BetweenOfDateColumn([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(BetweenDayRow.Data);

			var low  = BetweenDayLow;
			var high = BetweenDayHigh;

			var rows =
				(
					from r in table
					from d in table.Where(d => d.Id == r.Id - 1).DefaultIfEmpty()
					select new
					{
						Stored                  = Sql.Between(r.Day, BetweenDayLow, BetweenDayHigh),
						StoredCompared          = r.Day >= BetweenDayLow && r.Day <= BetweenDayHigh,
						Computed                = Sql.Between(r.Other ?? r.Day, BetweenDayLow, BetweenDayHigh),
						ComputedCompared        = (r.Other ?? r.Day) >= BetweenDayLow && (r.Other ?? r.Day) <= BetweenDayHigh,
						Parameter               = Sql.Between(r.Day, low, high),
						ParameterCompared       = r.Day >= low && r.Day <= high,
						Optional                = Sql.Between(d!.Day, BetweenDayLow, BetweenDayHigh),
						OptionalCompared        = d!.Day >= BetweenDayLow && d!.Day <= BetweenDayHigh,
						Outside                 = Sql.NotBetween(r.Day, low, high),
						OutsideCompared         = r.Day < low || r.Day > high,
						OutsideConstant         = Sql.NotBetween(r.Day, BetweenDayLow, BetweenDayHigh),
						OutsideConstantCompared = r.Day < BetweenDayLow || r.Day > BetweenDayHigh,
						OutsideOptional         = Sql.NotBetween(d!.Day, BetweenDayLow, BetweenDayHigh),
						OutsideOptionalCompared = d!.Day < BetweenDayLow || d!.Day > BetweenDayHigh,
						Stamp                   = Sql.Between(r.Stamp, low, high),
						StampCompared           = r.Stamp >= low && r.Stamp <= high,
					}
				)
				.ToArray();

			rows.Length.ShouldBe(2);
			rows.ShouldAllBe(r =>
				r.Stored             == r.StoredCompared
				&& r.Computed        == r.ComputedCompared
				&& r.Parameter       == r.ParameterCompared
				&& r.Optional        == r.OptionalCompared
				&& r.Outside         == r.OutsideCompared
				&& r.OutsideConstant == r.OutsideConstantCompared
				&& r.OutsideOptional == r.OutsideOptionalCompared
				&& r.Stamp           == r.StampCompared);

			table
				.Where(r => Sql.Between(r.Day, low, high))
				.Select(r => r.Id)
				.OrderBy(id => id)
				.ToArray()
				.ShouldBe(
					table
						.Where(r => r.Day >= low && r.Day <= high)
						.Select(r => r.Id)
						.OrderBy(id => id)
						.ToArray());

			table
				.Where(r => Sql.NotBetween(r.Day, BetweenDayLow, BetweenDayHigh))
				.Select(r => r.Id)
				.OrderBy(id => id)
				.ToArray()
				.ShouldBe(
					table
						.Where(r => r.Day < BetweenDayLow || r.Day > BetweenDayHigh)
						.Select(r => r.Id)
						.OrderBy(id => id)
						.ToArray());
		}

		// A value written beside a date column as `low` answers as its `>=` / `<=` do; beside an optional row's missing column the
		// negation stays unknown, as SQL's NOT BETWEEN.
		[Test]
		public void BetweenOfValueBesideDateColumn([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(BetweenDayRow.Data);

			var value = new DateTime(2020, 1, 2, 12, 0, 0);
			var high  = new DateTime(2020, 1, 2, 6, 0, 0);
			var later = new DateTime(2020, 1, 3);

			var rows =
				(
					from r in table
					from d in table.Where(d => d.Id == r.Id - 1).DefaultIfEmpty()
					orderby r.Id
					select new
					{
						Low             = Sql.Between(value, r.Day, high),
						LowCompared     = value >= r.Day && value <= high,
						Outside         = Sql.NotBetween(value, r.Day, high),
						OutsideCompared = value < r.Day || value > high,
						Columns         = Sql.Between(value, r.Day, r.Stamp),
						ColumnsCompared = value >= r.Day && value <= r.Stamp,
						Optional        = !Sql.Between(value, d!.Day, later) ? "a" : "b",
					}
				)
				.ToArray();

			rows.Length.ShouldBe(2);
			rows.ShouldAllBe(r =>
				r.Low        == r.LowCompared
				&& r.Outside == r.OutsideCompared
				&& r.Columns == r.ColumnsCompared);

			rows
				.Select(r => r.Optional)
				.ShouldBe(["b", "b"]);

			var optional =
				from r in table
				from d in table.Where(d => d.Id == r.Id - 1).DefaultIfEmpty()
				where !Sql.Between(value, d!.Day, later)
				select r.Id;

			optional
				.ToArray()
				.ShouldBeEmpty();

			// The same value compared beside a date the query computes, in one statement.
			table
				.Where(r => Sql.Between(value, r.Day, high) || value >= r.Stamp.Date)
				.Select(r => r.Id)
				.OrderBy(id => id)
				.ToArray()
				.ShouldBe([1, 2]);
		}

		// A NULL client value or bound leaves Between unknown, and its negation too, as SQL's BETWEEN.
		[Test]
		public void BetweenOfNullValueBesideDateColumn([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(BetweenDayRow.Data);

			DateTime? none  = null;
			DateTime? value = new DateTime(2020, 1, 2, 12, 0, 0);
			DateTime? later = new DateTime(2020, 1, 3);
			var       high  = new DateTime(2020, 1, 3);
			var       some  = new DateTime(2020, 1, 2, 12, 0, 0);

			// Asked one by one: `A || !A` is reduced to true before it is run.
			table
				.Where(r => Sql.Between(none, r.Day, later))
				.Select(r => r.Id)
				.ToArray()
				.ShouldBeEmpty();

			table
				.Where(r => Sql.NotBetween(none, r.Day, later))
				.Select(r => r.Id)
				.ToArray()
				.ShouldBeEmpty();

			table
				.Where(r => Sql.NotBetween(none, r.Stamp, later))
				.Select(r => r.Id)
				.ToArray()
				.ShouldBeEmpty();

			table
				.Where(r => !Sql.Between(none, r.Day, later))
				.Select(r => r.Id)
				.ToArray()
				.ShouldBeEmpty();

			table
				.Where(r => !Sql.Between(none, r.Stamp, later))
				.Select(r => r.Id)
				.ToArray()
				.ShouldBeEmpty();

			// MySQL 5.7 answers `x NOT BETWEEN dateColumn AND NULL` TRUE where x is not below the column; MySQL 8 does not.
			if (!context.IsAnyOf(TestProvName.AllMySql57))
			{
				table
					.Where(r => !Sql.Between(value, r.Day, none))
					.Select(r => r.Id)
					.ToArray()
					.ShouldBeEmpty();
			}

			table
				.Where(r => !Sql.Between(none!.Value, r.Day, high))
				.Select(r => r.Id)
				.ToArray()
				.ShouldBeEmpty();

			if (!context.IsAnyOf(TestProvName.AllMySql57))
			{
				table
					.Where(r => !Sql.Between(some, r.Day, none!.Value))
					.Select(r => r.Id)
					.ToArray()
					.ShouldBeEmpty();
			}

			table
				.OrderBy(r => r.Id)
				.Select(r => !Sql.Between(none, r.Day, later) ? "a" : "b")
				.ToArray()
				.ShouldBe(["b", "b"]);
		}

		// Which type the value of a Between is given is engine behaviour, the same on every provider.
		[Test]
		public void BetweenValueTyping([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db      = GetDataContext(context);
			using var days    = db.CreateLocalTable(BetweenDayRow.Data);
			using var elapsed = db.CreateLocalTable(BetweenConvertedRow.Data);

			var value   = new DateTime(2020, 1, 2, 12, 0, 0);
			var low     = new DateTime(2020, 1, 1);
			var seconds = TimeSpan.FromSeconds(42);
			var maximum = TimeSpan.FromSeconds(100);

			// Both pairs type the value alike: as the date column.
			ParameterOf(days.Where(r => Sql.Between(value, r.Day, r.Day)), nameof(value))
				.DataType
				.ShouldBe(DataType.Date);

			// One bound is converted: the value and the other client bound are written in its form, as seconds.
			var converted = elapsed.Where(r => Sql.Between(seconds, r.Elapsed, maximum));

			ParameterOf(converted, nameof(seconds))
				.Value
				.ShouldBe(42L);
			ParameterOf(converted, nameof(maximum))
				.Value
				.ShouldBe(100L);

			// The pairs disagree and no bound is converted: the value is written untyped, not as the date column.
			ParameterOf(days.Where(r => Sql.Between(value, low, r.Day)), nameof(value))
				.DataType
				.ShouldNotBe(DataType.Date);

			static DataParameter ParameterOf<T>(IQueryable<T> query, string name)
			{
				return query
					.ToSqlQuery()
					.Parameters
					.Single(p => p.Name == name);
			}
		}

		[Table]
		sealed class BetweenParameterRow
		{
			[PrimaryKey]                         public int     Id     { get; set; }
			[Column(Length = 10)]                public string  Name   { get; set; } = null!;
			[Column(Precision = 10, Scale = 4)]  public decimal Amount { get; set; }

			public static readonly BetweenParameterRow[] Data =
			[
				new() { Id = 1, Name = "a", Amount = 1.5m    },
				new() { Id = 2, Name = "b", Amount = 2.25m   },
				new() { Id = 3, Name = "c", Amount = 3.125m  },
				new() { Id = 4, Name = "d", Amount = 4.0625m },
			];
		}

		// A value and a low bound that are both parameters, beside a column high bound; the bounds are in order on every row.
		[Test]
		public void BetweenOfParameterValueAndBound([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var value = 3;
			var low   = 0;

			AssertQuery(
				db.Parent
					.Where(p => Sql.Between(value, low, p.ParentID))
					.Select(p => p.ParentID));

			AssertQuery(
				db.Parent
					.Where(p => Sql.NotBetween(value, low, p.ParentID))
					.Select(p => p.ParentID));
		}

		// Firebird types such a value by a cast built from the value itself, so a longer string or a finer decimal in the next
		// execution of the same query must not be cut to the first one's type.
		[Test]
		public void BetweenOfParameterValueAndBoundRepeated([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(BetweenParameterRow.Data);

			NamesFrom(table, "c")
				.ShouldBe([3, 4]);
			NamesFrom(table, "cc")
				.ShouldBe([4]);

			AmountsFrom(table, 2.5m)
				.ShouldBe([3, 4]);
			AmountsFrom(table, 3.1251m)
				.ShouldBe([4]);

			static int[] NamesFrom(ITable<BetweenParameterRow> table, string value)
			{
				var low = "a";

				return table
					.Where(r => Sql.Between(value, low, r.Name))
					.OrderBy(r => r.Id)
					.Select(r => r.Id)
					.ToArray();
			}

			static int[] AmountsFrom(ITable<BetweenParameterRow> table, decimal value)
			{
				var low = 0m;

				return table
					.Where(r => Sql.Between(value, low, r.Amount))
					.OrderBy(r => r.Id)
					.Select(r => r.Id)
					.ToArray();
			}
		}

		[Table]
		sealed class BetweenTimeRow
		{
			[PrimaryKey] public int      Id    { get; set; }
			[Column]     public TimeSpan Value { get; set; }

			public static readonly BetweenTimeRow[] Data =
			[
				new() { Id = 1, Value = TimeSpan.FromHours(1) },
				new() { Id = 2, Value = TimeSpan.FromHours(2) },
				new() { Id = 3, Value = TimeSpan.FromHours(3) },
				new() { Id = 4, Value = TimeSpan.FromHours(4) },
			];
		}

		// System.Data.SQLite cannot bind a TimeSpan parameter, SQL CE and SQL Server 2005 have no type for one, Oracle none for the column.
		[Test]
		public void BetweenOfTimeColumn([DataSources(TestProvName.AllSQLiteClassic, TestProvName.AllSqlCe, TestProvName.AllSqlServer2005, TestProvName.AllOracle)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(BetweenTimeRow.Data);

			var low  = TimeSpan.FromMinutes(90);
			var high = TimeSpan.FromHours(3);

			table
				.Where(r => Sql.Between(r.Value, low, high))
				.OrderBy(r => r.Id)
				.Select(r => r.Id)
				.ToArray()
				.ShouldBe([2, 3]);

			table
				.Where(r => Sql.NotBetween(r.Value, low, high))
				.OrderBy(r => r.Id)
				.Select(r => r.Id)
				.ToArray()
				.ShouldBe([1, 4]);
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void BetweenOfDateSubquery([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(BetweenDayRow.Data);

			var low  = BetweenDayLow;
			var high = BetweenDayHigh;

			var rows =
				(
					from r in table
					let maxDay = table
						.Where(x => x.Id == r.Id)
						.Max(x => x.Day)
					select new
					{
						InRange         = Sql.Between(maxDay, low, high),
						InRangeCompared = maxDay >= low && maxDay <= high,
						Outside         = Sql.NotBetween(maxDay, low, high),
						OutsideCompared = maxDay < low || maxDay > high,
					}
				)
				.ToArray();

			rows.Length.ShouldBe(2);
			rows.ShouldAllBe(r =>
				r.InRange    == r.InRangeCompared
				&& r.Outside == r.OutsideCompared);
		}

		static int ClientOnlyDouble(int value) => value * 2;

		// No SQL is built for a call with an operand that has no SQL form, so the provider does not matter.
		[Test]
		public void BetweenOfClientOnlyValue([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				select new
				{
					p.ParentID,
					InRange = Sql.Between(ClientOnlyDouble(p.ParentID), 3, 8),
				};

			AssertQuery(query);

			var filtered =
				from p in db.Parent
				where Sql.Between(ClientOnlyDouble(p.ParentID), 3, 8)
				select p.ParentID;

			Assert.That(
				() => filtered.ToList(),
				Throws.TypeOf<LinqToDBException>().With.Message.Contains("could not be converted to SQL"));
		}

		[Test]
		public void MatchFtsTest([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			var q = from c in db.Types
					where SqlLite.MatchFts(c, "some*")
					select c;

			// FTS5 required
			//q.ToArray();

			var str = q.ToSqlQuery().Sql;
			Assert.That(str, Does.Contain(" MATCH "));
		}

		[Table]
		sealed class TagsTable
		{
			[Column] public string? Name { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3543")]
		public void SubstringWithNestedIndexOfPositions([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var tags = db.CreateLocalTable<TagsTable>();

			(from tag in tags
			 select new
			 {
				 Name = tag.Name!.Substring(tag.Name.IndexOf(".") + 1, tag.Name.IndexOf(".", 5) - tag.Name.IndexOf(".") - 1)
			 }).ToList();
		}

		sealed class DefaultFunctionNullabiityTable
		{
			public int Id     { get; set; }
			public int? Value { get; set; }

			public static readonly DefaultFunctionNullabiityTable[] Data =
			[
				new DefaultFunctionNullabiityTable() { Id = 1, Value = null },
				new DefaultFunctionNullabiityTable() { Id = 2, Value = 0 },
				new DefaultFunctionNullabiityTable() { Id = 3, Value = 1 },
			];
		}

		[Test]
		public void TestDefaultFunctionNullability([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(DefaultFunctionNullabiityTable.Data);

			var ids1 = tb.Where(r => Coalesce(r.Value, 0) != 0).Select(r => r.Id).ToArray();
			var ids2 = tb.Where(r => Coalesce(r.Value, 0) != 1).Select(r => r.Id).ToArray();
			var ids3 = tb.Where(r => Coalesce(r.Value, 0) == 0).Select(r => r.Id).ToArray();
			var ids4 = tb.Where(r => Coalesce(r.Value, 0) == 1).Select(r => r.Id).ToArray();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(ids1, Has.Length.EqualTo(1));
				Assert.That(ids1, Does.Contain(3));

				Assert.That(ids2, Has.Length.EqualTo(2));
				Assert.That(ids2, Does.Contain(1));
				Assert.That(ids2, Does.Contain(2));

				Assert.That(ids3, Has.Length.EqualTo(2));
				Assert.That(ids3, Does.Contain(1));
				Assert.That(ids3, Does.Contain(2));

				Assert.That(ids4, Has.Length.EqualTo(1));
				Assert.That(ids4, Does.Contain(3));
			}
		}

		[Sql.Function("COALESCE", ServerSideOnly = true)]
		static int Coalesce(int? value, int defaultValue) => throw new ServerSideOnlyException(nameof(Coalesce));
	}

	public static class SqlLite
	{
		sealed class MatchBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				var srcExpr = builder.GetExpression("src");
				if (srcExpr == null)
				{
					builder.IsConvertible = false;
					return;
				}

				var newField = new SqlAnchor(srcExpr, SqlAnchor.AnchorKindEnum.TableName);

				builder.AddParameter("table_field", newField);
			}
		}

		[Sql.Extension("{table_field} MATCH {match}", BuilderType = typeof(MatchBuilder), IsPredicate = true)]
		public static bool MatchFts<TEntity>(TEntity src, [ExprParameter]string match)
		{
			throw new ServerSideOnlyException(nameof(MatchFts));
		}
	}

	public static class FunctionExtension
	{
		public static string FullName(this Person person)
		{
			return person.LastName + ", " + person.FirstName;
		}

		[Sql.Function("SUM", ServerSideOnly = true, IsAggregate = true, ArgIndices = new[]{1})]
		public static TItem MySum<TSource,TItem>(this IEnumerable<TSource> src, Expression<Func<TSource,TItem>> value)
		{
			throw new ServerSideOnlyException(nameof(MySum));
		}

	}

}
