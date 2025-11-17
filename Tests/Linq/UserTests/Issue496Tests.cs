using System.Collections.Generic;
using System.Globalization;
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
		new sealed class Parent1
		{
			public int ParentID;
			[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = true)]
			public ICollection<Child1> Children = null!;
		}

		[Table("Child", IsColumnAttributeRequired = false)]
		sealed class Child1
		{
			public int  ChildID;
			public int? ParentID;
		}

		[Table("Parent", IsColumnAttributeRequired = false)]
		sealed class Parent2
		{
			public int ParentID;
			[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = true)]
			public ICollection<Child2> Children = null!;
		}

		[Table("Child", IsColumnAttributeRequired = false)]
		sealed class Child2
		{
			public int  ChildID;
			public long ParentID;
		}

		public class MyInt
		{
			public int RealValue { get; set; }
		}

		[Table("Parent", IsColumnAttributeRequired = false)]
		sealed class Parent3
		{
			public int ParentID;
			[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = true)]
			public ICollection<Child3> Children = null!;
		}

		[Table("Child", IsColumnAttributeRequired = false)]
		sealed class Child3
		{
			        public int    ChildID;
			[Column]public MyInt? ParentID;
		}

		[Table("Parent", IsColumnAttributeRequired = false)]
		new sealed class Parent4
		{
			[Column]
			public MyInt? ParentID;
			[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = true)]
			public ICollection<Child4> Children = null!;
		}

		[Table("Child", IsColumnAttributeRequired = false)]
		sealed class Child4
		{
			public int ChildID;
			public int ParentID;
		}

		[Table("Parent", IsColumnAttributeRequired = false)]
		new sealed class Parent5
		{
			[Column]
			public MyInt? ParentID;
			[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = true)]
			public ICollection<Child5> Children = null!;
		}

		[Table("Child", IsColumnAttributeRequired = false)]
		sealed class Child5
		{
			         public int    ChildID;
			[Column] public MyInt? ParentID;
		}

		[Test]
		public void Test1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var children = db.GetTable<Parent1>()
					.Where(p => p.ParentID == 1)
					.SelectMany(c => c.Children)
					.ToList();

				Assert.That(children, Is.Not.Empty);

				var expected = Child.Where(c => c.ParentID == 1);
				var result = children.Select(c => new Model.Child { ChildID = c.ChildID, ParentID = c.ParentID!.Value });

				AreEqual(expected, result);
			}
		}

		[YdbTableNotFound]
		[Test]
		public void Test2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var children = db.GetTable<Parent1>()
					.Select(p => new {p.Children})
					.ToList();

				Assert.That(children, Is.Not.Empty);
			}
		}

		[Test]
		public void Test3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var children = db.GetTable<Parent2>()
					.Where(p => p.ParentID == 1)
					.SelectMany(p => p.Children)
					.ToList();

				Assert.That(children, Is.Not.Empty);

				var expected = Child.Where(p => p.ParentID == 1);
				var result = children.Select(c => new Model.Child() { ChildID = c.ChildID, ParentID = (int)c.ParentID });

				AreEqual(expected, result);
			}
		}

		[YdbTableNotFound]
		[Test]
		public void Test4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var children = db.GetTable<Parent2>()
					.Select(p => new {p.Children})
					.ToList();

				Assert.That(children, Is.Not.Empty);
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
			schema.SetConvertExpression<long,    MyInt>        (x => new MyInt { RealValue = (int)x }); //SQLite
			schema.SetConvertExpression<decimal, MyInt>        (x => new MyInt { RealValue = (int)x }); //Oracle
			schema.SetConvertExpression<MyInt,   DataParameter>(x => new DataParameter { DataType = DataType.Int32, Value = x.RealValue });

			// linqservice serialization
			schema.SetConvertExpression<MyInt, string>(x => x.RealValue.ToString(CultureInfo.InvariantCulture));
			schema.SetConvertExpression<string, MyInt>(x => new MyInt { RealValue = int.Parse(x) });

			return schema;
		}

		[Ignore("(sdanyliv): Why event such translations is possible. Decided to do not complicate translator with strange cases.")]
		[Test]
		public void Test5([DataSources] string context)
		{
			using (var db = GetDataContext(context, GetMyIntSchema()))
			{
				var children = db.GetTable<Parent3>()
					.Where(p => p.ParentID == 1)
					.SelectMany(p => p.Children)
					.ToList();

				Assert.That(children, Is.Not.Empty);

				var expected = Child.Where(c => c.ParentID == 1);
				var result = children.Select(c => new Model.Child() { ChildID = c.ChildID, ParentID = c.ParentID!.RealValue });

				AreEqual(expected, result);
			}
		}

		[Ignore("(sdanyliv): Why event such translations is possible. Decided to do not complicate translator with strange cases.")]
		[Test]
		public void Test7([DataSources] string context)
		{
			using (var db = GetDataContext(context, GetMyIntSchema()))
			{
				var children = db.GetTable<Parent4>()
					.Where(p => p.ParentID!.RealValue == 1)
					.SelectMany(p => p.Children)
					.ToList();

				Assert.That(children, Is.Not.Empty);

				var expected = Child.Where(c => c.ParentID == 1);
				var result = children.Select(c => new Model.Child() { ChildID = c.ChildID, ParentID = c.ParentID });

				AreEqual(expected, result);
			}
		}

		[Ignore("(sdanyliv): Why event such translations is possible. Decided to do not complicate translator with strange cases.")]
		[Test]
		public void Test9([DataSources] string context)
		{
			using (var db = GetDataContext(context, GetMyIntSchema()))
			{
				var children = db.GetTable<Parent5>()
					.Where(p => p.ParentID!.RealValue == 1)
					.SelectMany(p => p.Children)
					.ToList();

				Assert.That(children, Is.Not.Empty);

				var expected = Child.Where(c => c.ParentID == 1);
				var result = children.Select(c => new Model.Child() { ChildID = c.ChildID, ParentID = c.ParentID!.RealValue });

				AreEqual(expected, result);
			}
		}
	}
}
