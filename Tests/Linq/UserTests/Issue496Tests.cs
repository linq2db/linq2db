using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
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

		public class MyInt
		{
			public int RealValue { get; set; }
		}

		[Table("Parent", IsColumnAttributeRequired = false)]
		class Parent3
		{
			public int ParentId;
			[Association(ThisKey = "ParentId", OtherKey = "ParentId", CanBeNull = true, IsBackReference = true)]
			public ICollection<Child3> Children;
		}

		[Table("Child", IsColumnAttributeRequired = false)]
		class Child3
		{
			        public int   ChildId;
			[Column]public MyInt ParentId;
		}

		[Table("Parent", IsColumnAttributeRequired = false)]
		class Parent4
		{
			[Column]
			public MyInt ParentId;
			[Association(ThisKey = "ParentId", OtherKey = "ParentId", CanBeNull = true, IsBackReference = true)]
			public ICollection<Child4> Children;
		}

		[Table("Child", IsColumnAttributeRequired = false)]
		class Child4
		{
			public int ChildId;
			public int ParentId;
		}

		[Table("Parent", IsColumnAttributeRequired = false)]
		class Parent5
		{
			[Column]
			public MyInt ParentId;
			[Association(ThisKey = "ParentId", OtherKey = "ParentId", CanBeNull = true, IsBackReference = true)]
			public ICollection<Child5> Children;
		}

		[Table("Child", IsColumnAttributeRequired = false)]
		class Child5
		{
			        public int   ChildId;
			[Column]public MyInt ParentId;
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

		private static MappingSchema GetMyIntSchema()
		{
			var schema = new MappingSchema();

			schema.SetDataType  (typeof(MyInt), DataType.Int32);
			schema.SetScalarType(typeof(MyInt));
			schema.SetCanBeNull (typeof(MyInt), false);

			schema.SetConvertExpression<MyInt,   int>          (x => x.RealValue);
			schema.SetConvertExpression<int,     MyInt>        (x => new MyInt { RealValue = x });
			schema.SetConvertExpression<Int64,   MyInt>        (x => new MyInt { RealValue = (int)x }); //SQLite
			schema.SetConvertExpression<decimal, MyInt>        (x => new MyInt { RealValue = (int)x }); //Oracle
			schema.SetConvertExpression<MyInt,   DataParameter>(x => new DataParameter { DataType = DataType.Int32, Value = x.RealValue });

			return schema;
		}

		[Test, DataContextSource]
		public void Test5(string context)
		{
			using (var db = GetDataContext(context, GetMyIntSchema()))
			{
				var children = db.GetTable<Parent3>()
					.Where(_ => _.ParentId == 1)
					.SelectMany(_ => _.Children)
					.ToList();

				Assert.IsNotEmpty(children);

				var expected = Child.Where(_ => _.ParentID == 1);
				var result = children.Select(_ => new Model.Child() { ChildID = _.ChildId, ParentID = _.ParentId.RealValue });

				AreEqual(expected, result);
			}
		}

		[Test, DataContextSource]
		public void Test7(string context)
		{
			using (var db = GetDataContext(context, GetMyIntSchema()))
			{
				var children = db.GetTable<Parent4>()
					.Where(_ => _.ParentId.RealValue == 1)
					.SelectMany(_ => _.Children)
					.ToList();

				Assert.IsNotEmpty(children);

				var expected = Child.Where(_ => _.ParentID == 1);
				var result = children.Select(_ => new Model.Child() { ChildID = _.ChildId, ParentID = _.ParentId });

				AreEqual(expected, result);
			}
		}

		[Test, DataContextSource]
		public void Test9(string context)
		{
			using (var db = GetDataContext(context, GetMyIntSchema()))
			{
				var children = db.GetTable<Parent5>()
					.Where(_ => _.ParentId.RealValue == 1)
					.SelectMany(_ => _.Children)
					.ToList();

				Assert.IsNotEmpty(children);

				var expected = Child.Where(_ => _.ParentID == 1);
				var result = children.Select(_ => new Model.Child() { ChildID = _.ChildId, ParentID = _.ParentId.RealValue });

				AreEqual(expected, result);
			}
		}
	}
}
