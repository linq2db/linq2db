using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class LoadWithTest : TestBase
	{
		[Test, DataContextSource]
		public void LoadWith1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from t in db.Child.LoadWith(p => p.Parent)
					select t;

				var ch = q.First();

				Assert.IsNotNull(ch.Parent);
			}
		}

		[Test, DataContextSource]
		public void LoadWith2(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from t in db.GrandChild.LoadWith(p => p.Child.Parent)
					select t;

				var ch = q.First();

				Assert.IsNotNull(ch.Child);
				Assert.IsNotNull(ch.Child.Parent);
			}
		}

		[Test, DataContextSource]
		public void LoadWith3(string context)
		{
			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

			using (var db = GetDataContext(context))
			{
				db.MappingSchema.SetConvertExpression<IEnumerable<Child>,ImmutableList<Child>>(
					t => ImmutableList.Create(t.ToArray()));

				var q =
					from p in db.Parent.LoadWith(p => p.Children3)
					select new
					{
						p.GrandChildren.Count,
						p
					};

				q.ToList();

				var q1 = q.Select(t => t.p).SelectMany(p => p.Children);

				q1.ToList();
			}

			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
		}

		class IEnumerableToImmutableListConvertProvider<T> : IGenericConvertProvider
		{
			public void SetConvertExpression(MappingSchema mappingSchema)
			{
				mappingSchema.SetConvertExpression<IEnumerable<T>,ImmutableList<T>>(
					t => ImmutableList.Create(t.ToArray()));
			}
		}

		[Test, DataContextSource]
		public void LoadWith4(string context)
		{
			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

			MappingSchema.Default.SetGenericConvertProvider(typeof(IEnumerableToImmutableListConvertProvider<>));

			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Parent.LoadWith(p => p.Children3)
					select new
					{
						p.GrandChildren.Count,
						p
					};

				q.ToList();

				var q1 = q.Select(t => t.p).SelectMany(p => p.Children);

				q1.ToList();
			}

			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
		}

		[Test, DataContextSource]
		public void LoadWith5(string context)
		{
			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

			using (var db = GetDataContext(context))
			{
				db.MappingSchema.SetConvertExpression<IEnumerable<Child>,ImmutableList<Child>>(t => ImmutableList.Create(t.ToArray()));

				var q =
					from p in db.Parent.LoadWith(p => p.Children3.First().GrandChildren[0].Child.Parent)
					select new
					{
						p.GrandChildren.Count,
						p
					};

				q.ToList();

				var q1 = q.Select(t => t.p).SelectMany(p => p.Children);

				q1.ToList();
			}

			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
		}

		[Test, DataContextSource]
		public void LoadWith6(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Child.LoadWith(p => p.GrandChildren[0].Child.Parent)
					select new
					{
						p.GrandChildren.Count,
						p.Parent
					};

				q.ToList();

				var q1 = q.Select(t => t.Parent).SelectMany(p => p.Children).Distinct();

				q1.ToList();
			}
		}
	}
}
