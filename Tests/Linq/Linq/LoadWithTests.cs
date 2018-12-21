using System;
using System.Collections.Generic;
#if !NOIMMUTABLE
using System.Collections.Immutable;
#endif
using System.Linq;

using LinqToDB;
using LinqToDB.Expressions;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class LoadWithTests : TestBase
	{
		[Test]
		public void LoadWith1([DataSources] string context)
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

		[Test]
		public void LoadWith2([DataSources] string context)
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

		[Test]
		public void LoadWith3([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
#if !NOIMMUTABLE
				db.MappingSchema.SetConvertExpression<IEnumerable<Child>,ImmutableList<Child>>(
					t => ImmutableList.Create(t.ToArray()));
#endif

				var q =
					from p in db.Parent.LoadWith(p => p.Children3)
					select new
					{
						p.GrandChildren.Count,
						p
					};

				var ch = q.ToList().Select(t => t.p).SelectMany(p => p.Children3).FirstOrDefault();

				Assert.IsNotNull(ch);
			}
		}

		class EnumerableToImmutableListConvertProvider<T> : IGenericInfoProvider
		{
			public void SetInfo(MappingSchema mappingSchema)
			{
#if !NOIMMUTABLE
				mappingSchema.SetConvertExpression<IEnumerable<T>,ImmutableList<T>>(
					t => ImmutableList.Create(t.ToArray()));
#endif
			}
		}

		[Test]
		public void LoadWith4([DataSources] string context)
		{
			MappingSchema.Default.SetGenericConvertProvider(typeof(EnumerableToImmutableListConvertProvider<>));

			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Parent.LoadWith(p => p.Children3)
					select new
					{
						p.GrandChildren.Count,
						p
					};

				var ch = q.ToList().Select(t => t.p).SelectMany(p => p.Children3).FirstOrDefault();

				Assert.IsNotNull(ch);
			}
		}

		[Test]
		public void LoadWith5([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Parent.LoadWith(p => p.Children.First().GrandChildren[0].Child.Parent)
					select new
					{
						p.GrandChildren.Count,
						p
					};

				var ch = q.ToList().Select(t => t.p).SelectMany(p => p.Children).SelectMany(p => p.GrandChildren).FirstOrDefault();

				Assert.IsNotNull(ch);
				Assert.IsNotNull(ch.Child);
				Assert.IsNotNull(ch.Child.Parent);
			}
		}

		[Test]
		public void LoadWith6([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Child.LoadWith(p => p.GrandChildren2[0].Child.Parent)
					select new
					{
						p.GrandChildren.Count,
						p
					};

				var ch = q.ToList().Select(t => t.p).SelectMany(p => p.GrandChildren2).FirstOrDefault();

				Assert.IsNotNull(ch);
				Assert.IsNotNull(ch.Child);
				Assert.IsNotNull(ch.Child.Parent);
			}
		}

		[Test]
		public void LoadWith7([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Child.LoadWith(p => p.GrandChildren2[0].Child.Parent)
					select new
					{
						p.GrandChildren.Count,
						p
					};

				var ch = q.Select(t => t.p).SelectMany(p => p.GrandChildren2).FirstOrDefault();

				Assert.IsNotNull(ch);
				Assert.IsNotNull(ch.Child);
				Assert.IsNotNull(ch.Child.Parent);
			}
		}

		[Test]
		public void LoadWith8([DataSources(ProviderName.Access)] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.GrandChild.LoadWith(p => p.Child.GrandChildren[0].Child.Parent)
					select p;

				var ch = q.SelectMany(p => p.Child.GrandChildren).FirstOrDefault();

				Assert.IsNotNull(ch);
				Assert.IsNotNull(ch.Child);
				Assert.IsNotNull(ch.Child.Parent);
			}
		}

		[Test]
		public void LoadWith9([DataSources(ProviderName.Access)] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.GrandChild.LoadWith(p => p.Child.GrandChildren)
					select p;

				var ch = q.SelectMany(p => p.Child.GrandChildren).FirstOrDefault();

				Assert.IsNotNull(ch);
				Assert.IsNull   (ch.Child);
			}
		}

		[Test]
//#if !NETSTANDARD1_6
//		[Timeout(15000)]
//#endif
		public void LoadWith10([DataSources(ProviderName.Access)] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Parent.LoadWith(p => p.Children)
					where p.ParentID < 2
					select p;

				for (var i = 0; i < 100; i++)
				{
					var _ = q.ToList();
				}
			}
		}

		[Test]
		public void LoadWith11([DataSources(ProviderName.Access)] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Parent.LoadWith(p => p.Children).LoadWith(p => p.GrandChildren)
					where p.ParentID < 2
					select p;

				foreach (var parent in q)
				{
					Assert.IsNotNull (parent.Children);
					Assert.IsNotNull (parent.GrandChildren);
					Assert.IsNotEmpty(parent.Children);
					Assert.IsNotEmpty(parent.GrandChildren);
					Assert.IsNull    (parent.Children3);
				}
			}
		}
	}
}
