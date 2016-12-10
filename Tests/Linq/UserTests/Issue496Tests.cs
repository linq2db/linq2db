using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue496Tests : TestBase
	{
		[Table("Parent", IsColumnAttributeRequired = false)]
		class Parent1
		{
			public int ParentId;
			[Association(ThisKey = "ParentId", OtherKey = "ParentId", CanBeNull = true, IsBackReference = true)]
			public ICollection<Child1> Children;
		}

		[Table("Child", IsColumnAttributeRequired = false)]
		class Child1
		{
			public int  ChildId;
			public int? ParentId;
		}

		[Table("Parent", IsColumnAttributeRequired = false)]
		class Parent2
		{
			public int ParentId;
			[Association(ThisKey = "ParentId", OtherKey = "ParentId", CanBeNull = true, IsBackReference = true)]
			public ICollection<Child2> Children;
		}

		[Table("Child", IsColumnAttributeRequired = false)]
		class Child2
		{
			public int  ChildId;
			public long ParentId;
		}

		[Test, DataContextSource]
		public void Test1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var children = db.GetTable<Parent1>()
					.Where(_ => _.ParentId == 1)
					.SelectMany(_ => _.Children)
					.ToList();

				Assert.IsNotEmpty(children);

				var expected = Child.Where(_ => _.ParentID == 1);
				var result = children.Select(_ => new Model.Child() { ChildID = _.ChildId, ParentID = _.ParentId.Value });

				AreEqual(expected, result);
			}
		}

		[Test, DataContextSource]
		public void Test2(string context)
		{
			try
			{
				LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

				using (var db = GetDataContext(context))
				{
					var children = db.GetTable<Parent1>()
						.Select(_ => new { _.Children })
						.ToList();

					Assert.IsNotEmpty(children);
				}
			}
			finally
			{
				LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
			}
		}

		[Test, DataContextSource]
		public void Test3(string context)
		{
			using (var db = GetDataContext(context))
			{
				var children = db.GetTable<Parent2>()
					.Where(_ => _.ParentId == 1)
					.SelectMany(_ => _.Children)
					.ToList();

				Assert.IsNotEmpty(children);

				var expected = Child.Where(_ => _.ParentID == 1);
				var result = children.Select(_ => new Model.Child() { ChildID = _.ChildId, ParentID = (int)_.ParentId });

				AreEqual(expected, result);
			}
		}

		[Test, DataContextSource]
		public void Test4(string context)
		{
			try
			{
				LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

				using (var db = GetDataContext(context))
				{
					var children = db.GetTable<Parent2>()
						.Select(_ => new { _.Children })
						.ToList();

					Assert.IsNotEmpty(children);
				}
			}
			finally
			{
				LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
			}
		}
	}
}
