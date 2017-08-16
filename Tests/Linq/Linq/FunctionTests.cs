using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Linq;
using LinqToDB.SqlQuery;
using NUnit.Framework;

// ReSharper disable UnusedMember.Local

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class FunctionTests : TestBase
	{
		[Test, DataContextSource]
		public void Contains1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where new[] { 1, 2 }.Contains(p.ParentID) select p,
					from p in db.Parent where new[] { 1, 2 }.Contains(p.ParentID) select p);
		}

		[Test, DataContextSource]
		public void Contains2(string context)
		{
			var arr = new[] { 1, 2 };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where arr.Contains(p.ParentID) select p,
					from p in db.Parent where arr.Contains(p.ParentID) select p);
		}

		[Test, DataContextSource]
		public void Contains3(string context)
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

		[Test, DataContextSource]
		public void Contains4(string context)
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

		[Test, DataContextSource]
		public void Contains5(string context)
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

		[Test, DataContextSource]
		public void Contains6(string context)
		{
			var arr = new List<int> { 1, 2 };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where arr.Contains(p.ParentID) select p,
					from p in db.Parent where arr.Contains(p.ParentID) select p);
		}

		[Test, DataContextSource]
		public void Contains7(string context)
		{
			IEnumerable<int> arr = new[] { 1, 2 };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where arr.Contains(p.ParentID) select p,
					from p in db.Parent where arr.Contains(p.ParentID) select p);
		}

		[Test, DataContextSource]
		public void ContainsKey1(string context)
		{
			var arr = new Dictionary<int,int>
			{
				{ 1, 1 },
				{ 2, 2 },
			};

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where arr.Keys.Contains(p.ParentID) select p,
					from p in db.Parent where arr.Keys.Contains(p.ParentID) select p);
		}

		[Test, DataContextSource]
		public void ContainsKey2(string context)
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

		[Test, DataContextSource]
		public void ContainsValue1(string context)
		{
			var arr = new Dictionary<int,int>
			{
				{ 1, 1 },
				{ 2, 2 },
			};

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where arr.Values.Contains(p.ParentID) select p,
					from p in db.Parent where arr.Values.Contains(p.ParentID) select p);
		}

		[Test, DataContextSource]
		public void ContainsValue2(string context)
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

		[Test, DataContextSource]
		public void ContainsHashSet1(string context)
		{
			var arr = new HashSet<int> { 1, 2 };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where arr.Contains(p.ParentID) select p,
					from p in db.Parent where arr.Contains(p.ParentID) select p);
		}

		[Test, DataContextSource]
		public void EmptyContains1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					where new int[0].Contains(p.ParentID) || p.ParentID == 2
					select p,
					from p in db.Parent
					where new int[0].Contains(p.ParentID) || p.ParentID == 2
					select p);
		}

		[Test, DataContextSource]
		public void ContainsString11(string context)
		{
			var arr = new List<string> { "John" };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where arr.Contains(p.FirstName) select p,
					from p in db.Person where arr.Contains(p.FirstName) select p);
		}

		[Test, DataContextSource]
		public void ContainsString12(string context)
		{
			var nm = "John";

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where new List<string> { nm }.Contains(p.FirstName) select p,
					from p in db.Person where new List<string> { nm }.Contains(p.FirstName) select p);
		}

		[Test, DataContextSource]
		public void ContainsString13(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where new List<string> { "John" }.Contains(p.FirstName) select p,
					from p in db.Person where new List<string> { "John" }.Contains(p.FirstName) select p);
		}

		[Test, DataContextSource]
		public void ContainsString21(string context)
		{
			var arr = new[] { "John" };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where arr.Contains(p.FirstName) select p,
					from p in db.Person where arr.Contains(p.FirstName) select p);
		}

		[Test, DataContextSource]
		public void ContainsString22(string context)
		{
			var nm = "John";

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where new[] { nm }.Contains(p.FirstName) select p,
					from p in db.Person where new[] { nm }.Contains(p.FirstName) select p);
		}

		[Test, DataContextSource]
		public void ContainsString23(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where new[] { "John" }.Contains(p.FirstName) select p,
					from p in db.Person where new[] { "John" }.Contains(p.FirstName) select p);
		}

		[Test, DataContextSource]
		public void Equals1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where p.ParentID.Equals(2) select p,
					from p in db.Parent where p.ParentID.Equals(2) select p);
		}

		[Test, DataContextSource]
		public void Equals2(string context)
		{
			var child = (from ch in Child where ch.ParentID == 2 select ch).First();

			using (var db = GetDataContext(context))
				AreEqual(
					from ch in    Child where !ch.Equals(child) select ch,
					from ch in db.Child where !ch.Equals(child) select ch);
		}

		[Test, DataContextSource]
		public void Equals3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where p.Value1.Equals(null) select p,
					from p in db.Parent where p.Value1.Equals(null) select p);
		}

		[Test, NorthwindDataContext]
		public void Equals4(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				AreEqual(
					dd.Customer.Where(c => !c.Address.Equals(null)),
					db.Customer.Where(c => !c.Address.Equals(null)));
			}
		}

		[Test, DataContextSource(
			ProviderName.DB2, ProviderName.Informix, ProviderName.PostgreSQL, ProviderName.SQLite, TestProvName.SQLiteMs, ProviderName.Access)]
		public void NewGuid1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Types where p.GuidValue != Sql.NewGuid() select p.GuidValue,
					from p in db.Types where p.GuidValue != Sql.NewGuid() select p.GuidValue);
		}

		[Test, DataContextSource(ProviderName.DB2, ProviderName.Informix, ProviderName.PostgreSQL, ProviderName.SQLite, TestProvName.SQLiteMs, ProviderName.Access)]
		public void NewGuid2(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreNotEqual(Guid.Empty, (from p in db.Types select Sql.NewGuid()).First());
		}

		[Test, DataContextSource]
		public void CustomFunc(string context)
		{
			Expressions.MapMember<Person>(p => p.FullName(), (Expression<Func<Person,string>>)(p => p.LastName + ", " + p.FirstName));

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where p.FullName() == "Pupkin, John" select p.FullName(),
					from p in db.Person where p.FullName() == "Pupkin, John" select p.FullName());
		}

		[Test, DataContextSource]
		public void Count1(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Child.Count(c => c.ParentID == 1),
					db.Child.Count(c => c.ParentID == 1));
		}

		[Test, DataContextSource]
		public void Sum1(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Child.Sum(c => c.ParentID),
					db.Child.Sum(c => c.ParentID));
		}

		[ExpressionMethod("ChildCountExpression")]
		public static int ChildCount(Parent parent)
		{
			throw new NotSupportedException();
		}

		static Expression ChildCountExpression()
		{
			return
				(Expression<Func<Parent, int>>)
				(p => p.Children.Where(c => c.ParentID > 2).Sum(c => c.ParentID * c.ChildID));
		}

		[Test, DataContextSource]
		public void Sum2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Select(p => p.Children.Where(c => c.ParentID > 2).Sum(c => c.ParentID * c.ChildID)),
					db.Parent.Select(p => ChildCount(p)));
		}

		[Test, DataContextSource]
		public void CustomAggregate(string context)
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

		[Test, DataContextSource]
		public void GetValueOrDefault(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where p.Value1.GetValueOrDefault() > 0 select new { Value = p.Value1.GetValueOrDefault() },
					from p in db.Parent where p.Value1.GetValueOrDefault() > 0 select new { Value = p.Value1.GetValueOrDefault() });
		}

		[Test, DataContextSource]
		public void AsNullTest(string context)
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

		[Test, DataContextSource]
		public void AsNotNullTest(string context)
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

		[Test, DataContextSource]
		public void Between1(string context)
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

		[Test, DataContextSource]
		public void Between2(string context)
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

		[Test, IncludeDataContextSource(true, ProviderName.SQLite)]
		public void MatchFtsTest(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from c in db.Types
					where SqlLite.MatchFts(c, "some*")
					select c;

				var str = q.ToString();
				Assert.True(str.Contains(" matches "));
			}
		}
	}

	public static class FunctionExtension
	{
		public static string FullName(this Person person)
		{
			return person.LastName + ", " + person.FirstName;
		}

		[Sql.Function("SUM", ServerSideOnly = true, IsAggregate = true, ArgIndices = new[]{0})]
		public static TItem MySum<TSource,TItem>(this IEnumerable<TSource> src, Expression<Func<TSource,TItem>> value)
		{
			throw new InvalidOperationException();
		}

	}

	public static class SqlLite
	{
		class MatchBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var field = builder.GetExpression("src") as SqlField;
				if (field == null)
					throw new InvalidOperationException("Can not get table");

				var sqlTable = (SqlTable) field.Table;
				var newField = new SqlField
				{
					Name  = sqlTable.PhysicalName,
					Table = sqlTable
				};

				builder.AddParameter("table_field", newField);
			}
		}

		[Sql.Extension("{table_field} matches {match}", BuilderType = typeof(MatchBuilder), IsPredicate = true)]
		public static bool MatchFts<TEntity>(TEntity src, [ExprParameter]string match)
		{
			throw new InvalidOperationException();
		}
	}

}
