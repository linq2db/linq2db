using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq;
using LinqToDB.Mapping;

using NUnit.Framework;

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
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where new[] { 1, 2 }.Contains(p.ParentID) select p,
					from p in db.Parent where new[] { 1, 2 }.Contains(p.ParentID) select p);
		}

		[Test]
		public void Contains2([DataSources] string context)
		{
			var arr = new[] { 1, 2 };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where arr.Contains(p.ParentID) select p,
					from p in db.Parent where arr.Contains(p.ParentID) select p);
		}

		[Test]
		public void Contains3([DataSources] string context)
		{
			var n = 2;

			using (var data = GetDataContext(context))
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

			using (var data = GetDataContext(context))
				AreEqual(
					from p in Parent
					where arr.Contains(p.ParentID)
					select p,
					CompiledQuery.Compile<ITestDataContext,IQueryable<Parent>>(db =>
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

			using (var data = GetDataContext(context))
			{
				var cq = CompiledQuery.Compile<ITestDataContext,int[],IQueryable<Parent>>((db,a) =>
					from p in db.Parent
					where a.Contains(p.ParentID)
					select p);

				AreEqual(expected1, cq(data, arr1));
				AreEqual(expected2, cq(data, arr2));
			}
		}

		[Test]
		public void Contains6([DataSources] string context)
		{
			var arr = new List<int> { 1, 2 };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where arr.Contains(p.ParentID) select p,
					from p in db.Parent where arr.Contains(p.ParentID) select p);
		}

		[Test]
		public void Contains7([DataSources] string context)
		{
			IEnumerable<int> arr = new[] { 1, 2 };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where arr.Contains(p.ParentID) select p,
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

			using (var db = GetDataContext(context))
				AreEqual(
#pragma warning disable CA1841 // Prefer Dictionary.Contains methods : suppressed as we test this method
					from p in    Parent where arr.Keys.Contains(p.ParentID) select p,
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

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where arr.ContainsKey(p.ParentID) select p,
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

			using (var db = GetDataContext(context))
				AreEqual(
#pragma warning disable CA1841 // Prefer Dictionary.Contains methods : suppressed as we test this method
					from p in    Parent where arr.Values.Contains(p.ParentID) select p,
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

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where arr.ContainsValue(p.ParentID) select p,
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

			using (var db = GetDataContext(context))
				AreEqual(
#pragma warning disable CA1841 // Prefer Dictionary.Contains methods : suppressed as we test this method
					from p in    Parent where arr.Keys.Contains(p.ParentID) select p,
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

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where arr.ContainsKey(p.ParentID) select p,
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

			using (var db = GetDataContext(context))
				AreEqual(
#pragma warning disable CA1841 // Prefer Dictionary.Contains methods : suppressed as we test this method
					from p in    Parent where arr.Values.Contains(p.ParentID) select p,
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

			using (var db = GetDataContext(context))
				AreEqual(
#pragma warning disable CA1841 // Prefer Dictionary.Contains methods : suppressed as we test this method
					from p in    Parent where arr.Keys.Contains(p.ParentID) select p,
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

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where arr.ContainsKey(p.ParentID) select p,
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

			using (var db = GetDataContext(context))
				AreEqual(
#pragma warning disable CA1841 // Prefer Dictionary.Contains methods : suppressed as we test this method
					from p in    Parent where arr.Values.Contains(p.ParentID) select p,
					from p in db.Parent where arr.Values.Contains(p.ParentID) select p);
#pragma warning restore CA1841 // Prefer Dictionary.Contains methods
		}

		[Test]
		public void ContainsHashSet1([DataSources] string context)
		{
			var arr = new HashSet<int> { 1, 2 };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where arr.Contains(p.ParentID) select p,
					from p in db.Parent where arr.Contains(p.ParentID) select p);
		}

#if NET8_0_OR_GREATER
		[Test]
		public void ContainsReadOnlySet([DataSources] string context)
		{
			IReadOnlySet<int> arr = new HashSet<int> { 1, 2 };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where arr.Contains(p.ParentID) select p,
					from p in db.Parent where arr.Contains(p.ParentID) select p);
		}
#endif

		[Test]
		public void EmptyContains1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
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

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where arr.Contains(p.FirstName) select p,
					from p in db.Person where arr.Contains(p.FirstName) select p);
		}

		[Test]
		public void ContainsString12([DataSources] string context)
		{
			var nm = "John";

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where new List<string> { nm }.Contains(p.FirstName) select p,
					from p in db.Person where new List<string> { nm }.Contains(p.FirstName) select p);
		}

		[Test]
		public void ContainsString13([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where new List<string> { "John" }.Contains(p.FirstName) select p,
					from p in db.Person where new List<string> { "John" }.Contains(p.FirstName) select p);
		}

		[Test]
		public void ContainsString21([DataSources] string context)
		{
			var arr = new[] { "John" };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where arr.Contains(p.FirstName) select p,
					from p in db.Person where arr.Contains(p.FirstName) select p);
		}

		[Test]
		public void ContainsString22([DataSources] string context)
		{
			var nm = "John";

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where new[] { nm }.Contains(p.FirstName) select p,
					from p in db.Person where new[] { nm }.Contains(p.FirstName) select p);
		}

		[Test]
		public void ContainsString23([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where new[] { "John" }.Contains(p.FirstName) select p,
					from p in db.Person where new[] { "John" }.Contains(p.FirstName) select p);
		}

		[Test]
		public void Equals1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where p.ParentID.Equals(2) select p,
					from p in db.Parent where p.ParentID.Equals(2) select p);
		}

		[Test]
		public void Equals2([DataSources] string context)
		{
			var child = (from ch in Child where ch.ParentID == 2 select ch).First();

			using (var db = GetDataContext(context))
				AreEqual(
					from ch in    Child where !ch.Equals(child) select ch,
					from ch in db.Child where !ch.Equals(child) select ch);
		}

		[Test]
		public void Equals3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where p.Value1.Equals(null) select p,
					from p in db.Parent where p.Value1.Equals(null) select p);
		}

		[Test]
		public void Equals4([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				AreEqual(
					dd.Customer.Where(c => !c.Address!.Equals(null)),
					db.Customer.Where(c => !c.Address!.Equals(null)));
			}
		}

		[Test]
		public void NewGuid1(
			[DataSources(
				ProviderName.DB2,
				TestProvName.AllInformix,
				TestProvName.AllPostgreSQL12Minus,
				TestProvName.AllSQLite,
				TestProvName.AllSapHana,
				TestProvName.AllAccess)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Types where p.GuidValue != Sql.NewGuid() select p.GuidValue,
					from p in db.Types where p.GuidValue != Sql.NewGuid() select p.GuidValue);
		}

		[Test]
		public void NewGuid2(
			[DataSources(
				ProviderName.DB2,
				TestProvName.AllInformix,
				TestProvName.AllSQLite,
				TestProvName.AllAccess)]
			string context)
		{
			using (var db = GetDataContext(context))
				Assert.That((from p in db.Types select Sql.NewGuid()).First(), Is.Not.EqualTo(Guid.Empty));
		}

		[Test]
		public void NewGuidOrder(
			[DataSources(false,
				ProviderName.DB2,
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

		[Test]
		public void CustomFunc([DataSources] string context)
		{
			Expressions.MapMember<Person>(p => p.FullName(), (Expression<Func<Person,string>>)(p => p.LastName + ", " + p.FirstName));

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where p.FullName() == "Pupkin, John" select p.FullName(),
					from p in db.Person where p.FullName() == "Pupkin, John" select p.FullName());
		}

		[Test]
		public void Count1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That(
					db.Child.Count(c => c.ParentID == 1), Is.EqualTo(Child.Count(c => c.ParentID == 1)));
		}

		[Test]
		public void Sum1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That(
					db.Child.Sum(c => c.ParentID), Is.EqualTo(Child.Sum(c => c.ParentID)));
		}

		[ExpressionMethod("ChildCountExpression")]
		private static int ChildCount(Parent parent)
		{
			throw new NotSupportedException();
		}

		static Expression ChildCountExpression()
		{
			return
				(Expression<Func<Parent, int>>)
				(p => p.Children.Where(c => c.ParentID > 2).Sum(c => c.ParentID * c.ChildID));
		}

		[Test]
		[RequiresCorrelatedSubquery]
		public void Sum2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Select(p => p.Children.Where(c => c.ParentID > 2).Sum(c => c.ParentID * c.ChildID)),
					db.Parent.Select(p => ChildCount(p)));
		}

		[Test]
		public void CustomAggregate([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					group p by p.ParentID into g
					select new
					{
						sum1 = g.Sum  (i => i.Value1) ?? 0,
						sum2 = g.Sum  (i => i.Value1) ?? 0,
					},
					from p in db.Parent
					group p by p.ParentID into g
					select new
					{
						sum1 = g.Sum  (i => i.Value1) ?? 0,
						sum2 = g.MySum(i => i.Value1) ?? 0,
					});
		}

		[Test]
		public void GetValueOrDefault([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where p.Value1.GetValueOrDefault() > 0 select new { Value = p.Value1.GetValueOrDefault() },
					from p in db.Parent where p.Value1.GetValueOrDefault() > 0 select new { Value = p.Value1.GetValueOrDefault() });
		}

		[Test]
		public void AsNullTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p1 in    Parent
					from p2 in    Parent
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
			using (var db = GetDataContext(context))
				AreEqual(
					from p1 in    Parent
					from p2 in    Parent
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
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent
					where p.Value1.Between(1, 10)
					select p,
					from p in db.Parent
					where p.Value1.Between(1, 10)
					select p);
		}

		[Test]
		public void Between2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent
					where p.ParentID.Between(1, 10)
					select p,
					from p in db.Parent
					where p.ParentID.Between(1, 10)
					select p);
		}

		[Test]
		public void MatchFtsTest([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from c in db.Types
					where SqlLite.MatchFts(c, "some*")
					select c;

				// FTS5 required
				//q.ToArray();

				var str = q.ToSqlQuery().Sql;
				Assert.That(str, Does.Contain(" MATCH "));
			}
		}

		[Table]
		sealed class TagsTable
		{
			[Column] public string? Name { get; set; }
		}

		[Test]
		public void Issue3543Test([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
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

		[Sql.Function("COALESCE")]
		static int Coalesce(int? value, int defaultValue) => throw new ServerSideOnlyException(nameof(Coalesce));
	}

	public static class SqlLite
	{
		sealed class MatchBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
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
			throw new InvalidOperationException();
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
			throw new InvalidOperationException();
		}

	}

}
