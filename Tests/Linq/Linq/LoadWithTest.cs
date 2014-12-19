using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using LinqToDB;
using LinqToDB.Expressions;
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

				var ch = q.ToList().Select(t => t.p).SelectMany(p => p.Children3).FirstOrDefault();

				Assert.IsNotNull(ch);
			}

			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
		}

		class EnumerableToImmutableListConvertProvider<T> : IGenericInfoProvider
		{
			public void SetInfo(MappingSchema mappingSchema)
			{
				mappingSchema.SetConvertExpression<IEnumerable<T>,ImmutableList<T>>(
					t => ImmutableList.Create(t.ToArray()));
			}
		}

		[Test, DataContextSource]
		public void LoadWith4(string context)
		{
			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

			MappingSchema.Default.SetGenericConvertProvider(typeof(EnumerableToImmutableListConvertProvider<>));

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

			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
		}

		[Test, DataContextSource]
		public void LoadWith5(string context)
		{
			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

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

			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
		}

		[Test, DataContextSource]
		public void LoadWith6(string context)
		{
			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

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

			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
		}

		[Test, DataContextSource]
		public void LoadWith7(string context)
		{
			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

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

			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
		}

		[Test, DataContextSource(ProviderName.Access)]
		public void LoadWith8(string context)
		{
			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

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

			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
		}

		[Test, DataContextSource(ProviderName.Access)]
		public void LoadWith9(string context)
		{
			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.GrandChild.LoadWith(p => p.Child.GrandChildren)
					select p;

				var ch = q.SelectMany(p => p.Child.GrandChildren).FirstOrDefault();

				Assert.IsNotNull(ch);
				Assert.IsNull   (ch.Child);
			}

			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
		}

		[Table("Parent")]
		class TestParent
		{
			[Column] public int  ParentID;
			[Column] public int? Value1;

			[Association(ThisKey = "ParentID", OtherKey = "ID")]
			public TestLinqDataTypes       LinqDataType;

			[Association(ThisKey = "ParentID", OtherKey = "ID")]
			public List<TestLinqDataTypes> LinqDataTypes;
		}

		[Table("LinqDataTypes")]
		class TestLinqDataTypes
		{
			[Column] public int      ID;
			[Column] public decimal  MoneyValue;
		}

		[Test, DataContextSource]
		public void LoadWith10(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.GetTable<TestParent>().LoadWith(p => p.LinqDataType)
					where p.ParentID == 1
					select p;

				var list = q.ToList();

				Assert.IsNotNull(list[0].LinqDataType);
			}
		}

		[Test, DataContextSource]
		public void LoadWith11(string context)
		{
			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.GetTable<TestParent>().LoadWith(p => p.LinqDataTypes)
					where p.ParentID == 1
					select p;

				var list = q.ToList();

				Assert.IsNotNull(list[0].LinqDataTypes);
				Assert.That(list[0].LinqDataTypes.Count, Is.GreaterThan(0));
			}

			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
		}
	}
}
