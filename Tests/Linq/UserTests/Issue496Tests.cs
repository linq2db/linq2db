using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

#pragma warning disable 0108

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue496Tests : TestBase
	{
		[Table("Parent", IsColumnAttributeRequired = false)]
		class Parent1
		{
			public int ParentID;
			[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = true, IsBackReference = true)]
			public ICollection<Child1> Children;
		}

		[Table("Child", IsColumnAttributeRequired = false)]
		class Child1
		{
			public int  ChildID;
			public int? ParentID;
		}

		[Table("Parent", IsColumnAttributeRequired = false)]
		class Parent2
		{
			public int ParentID;
			[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = true, IsBackReference = true)]
			public ICollection<Child2> Children;
		}

		[Table("Child", IsColumnAttributeRequired = false)]
		class Child2
		{
			public int  ChildID;
			public long ParentID;
		}

		public class MyInt
		{
			public int RealValue { get; set; }
		}

		[Table("Parent", IsColumnAttributeRequired = false)]
		class Parent3
		{
			public int ParentID;
			[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = true, IsBackReference = true)]
			public ICollection<Child3> Children;
		}

		[Table("Child", IsColumnAttributeRequired = false)]
		class Child3
		{
			        public int   ChildID;
			[Column]public MyInt ParentID;
		}

		[Table("Parent", IsColumnAttributeRequired = false)]
		class Parent4
		{
			[Column]
			public MyInt ParentID;
			[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = true, IsBackReference = true)]
			public ICollection<Child4> Children;
		}

		[Table("Child", IsColumnAttributeRequired = false)]
		class Child4
		{
			public int ChildID;
			public int ParentID;
		}

		[Table("Parent", IsColumnAttributeRequired = false)]
		class Parent5
		{
			[Column]
			public MyInt ParentID;
			[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = true, IsBackReference = true)]
			public ICollection<Child5> Children;
		}

		[Table("Child", IsColumnAttributeRequired = false)]
		class Child5
		{
			         public int   ChildID;
			[Column] public MyInt ParentID;
		}

		[Test]
		public void Test1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var children = db.GetTable<Parent1>()
					.Where(_ => _.ParentID == 1)
					.SelectMany(_ => _.Children)
					.ToList();

				Assert.IsNotEmpty(children);

				var expected = Child.Where(_ => _.ParentID == 1);
				var result = children.Select(_ => new Model.Child { ChildID = _.ChildID, ParentID = _.ParentID.Value });

				AreEqual(expected, result);
			}
		}

		[Test]
		public void Test2([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				var children = db.GetTable<Parent1>()
					.Select(_ => new {_.Children})
					.ToList();

				Assert.IsNotEmpty(children);
			}
		}

		[Test]
		public void Test3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var children = db.GetTable<Parent2>()
					.Where(_ => _.ParentID == 1)
					.SelectMany(_ => _.Children)
					.ToList();

				Assert.IsNotEmpty(children);

				var expected = Child.Where(_ => _.ParentID == 1);
				var result = children.Select(_ => new Model.Child() { ChildID = _.ChildID, ParentID = (int)_.ParentID });

				AreEqual(expected, result);
			}
		}

		[Test]
		public void Test4([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				var children = db.GetTable<Parent2>()
					.Select(_ => new {_.Children})
					.ToList();

				Assert.IsNotEmpty(children);
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

		[Test]
		public void Test5([DataSources] string context)
		{
			using (var db = GetDataContext(context, GetMyIntSchema()))
			{
				var children = db.GetTable<Parent3>()
					.Where(_ => _.ParentID == 1)
					.SelectMany(_ => _.Children)
					.ToList();

				Assert.IsNotEmpty(children);

				var expected = Child.Where(_ => _.ParentID == 1);
				var result = children.Select(_ => new Model.Child() { ChildID = _.ChildID, ParentID = _.ParentID.RealValue });

				AreEqual(expected, result);
			}
		}

		[Test]
		public void Test7([DataSources] string context)
		{
			using (var db = GetDataContext(context, GetMyIntSchema()))
			{
				var children = db.GetTable<Parent4>()
					.Where(_ => _.ParentID.RealValue == 1)
					.SelectMany(_ => _.Children)
					.ToList();

				Assert.IsNotEmpty(children);

				var expected = Child.Where(_ => _.ParentID == 1);
				var result = children.Select(_ => new Model.Child() { ChildID = _.ChildID, ParentID = _.ParentID });

				AreEqual(expected, result);
			}
		}

		[Test]
		public void Test9([DataSources] string context)
		{
			using (var db = GetDataContext(context, GetMyIntSchema()))
			{
				var children = db.GetTable<Parent5>()
					.Where(_ => _.ParentID.RealValue == 1)
					.SelectMany(_ => _.Children)
					.ToList();

				Assert.IsNotEmpty(children);

				var expected = Child.Where(_ => _.ParentID == 1);
				var result = children.Select(_ => new Model.Child() { ChildID = _.ChildID, ParentID = _.ParentID.RealValue });

				AreEqual(expected, result);
			}
		}
	}
}

#pragma warning restore 0108
