﻿using System;
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
					from p in    Parent where arr.Keys.Contains(p.ParentID) select p,
					from p in db.Parent where arr.Keys.Contains(p.ParentID) select p);
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
					from p in    Parent where arr.Values.Contains(p.ParentID) select p,
					from p in db.Parent where arr.Values.Contains(p.ParentID) select p);
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
		public void ContainsHashSet1([DataSources] string context)
		{
			var arr = new HashSet<int> { 1, 2 };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where arr.Contains(p.ParentID) select p,
					from p in db.Parent where arr.Contains(p.ParentID) select p);
		}

		[Test]
		public void EmptyContains1([DataSources] string context)
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
					dd.Customer.Where(c => !c.Address.Equals(null)),
					db.Customer.Where(c => !c.Address.Equals(null)));
			}
		}

		[Test]
		public void NewGuid1(
			[DataSources(
				ProviderName.DB2,
				ProviderName.Informix,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				ProviderName.Access)]
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
				ProviderName.Informix,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				ProviderName.Access)]
			string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreNotEqual(Guid.Empty, (from p in db.Types select Sql.NewGuid()).First());
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
				Assert.AreEqual(
					   Child.Count(c => c.ParentID == 1),
					db.Child.Count(c => c.ParentID == 1));
		}

		[Test]
		public void Sum1([DataSources] string context)
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

		[Test]
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

				var str = q.ToString();
				Assert.True(str.Contains(" matches "));
			}
		}
	}

	public static class SqlLite
	{
		class MatchBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				if (!(builder.GetExpression("src") is SqlField field))
					throw new InvalidOperationException("Can not get table");

				var sqlTable = (SqlTable)field.Table;

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

}
