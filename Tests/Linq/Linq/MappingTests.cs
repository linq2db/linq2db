using System;
using System.Linq;
#if NETFRAMEWORK
using System.ServiceModel;
#endif

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class MappingTests : TestBase
	{
		[Test]
		public void Enum1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where new[] { Gender.Male }.Contains(p.Gender) select p,
					from p in db.Person where new[] { Gender.Male }.Contains(p.Gender) select p);
		}

		[Test]
		public void Enum2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where p.Gender == Gender.Male select p,
					from p in db.Person where p.Gender == Gender.Male select p);
		}

		[Test]
		public void Enum21([DataSources] string context)
		{
			var gender = Gender.Male;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where p.Gender == gender select p,
					from p in db.Person where p.Gender == gender select p);
		}

		[Test]
		public void Enum3([DataSources] string context)
		{
			var fm = Gender.Female;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where p.Gender != fm select p,
					from p in db.Person where p.Gender != fm select p);
		}

		[Test]
		public void Enum4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent4 where p.Value1 == TypeValue.Value1 select p,
					from p in db.Parent4 where p.Value1 == TypeValue.Value1 select p);
		}

		[Test]
		public void EnumValue1()
		{
			var value = ConvertTo<TypeValue>.From(1);

			Assert.Multiple(() =>
			{
				Assert.That(value, Is.EqualTo(TypeValue.Value1));
				Assert.That((int)value, Is.EqualTo(10));
			});
		}

		[Test]
		public void Enum5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent4 where p.Value1 == TypeValue.Value3 select p,
					from p in db.Parent4 where p.Value1 == TypeValue.Value3 select p);
		}

		[Test]
		public void Enum6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent4
					join c in    Child on p.ParentID equals c.ParentID
					where p.Value1 == TypeValue.Value1 select p,
					from p in db.Parent4
					join c in db.Child on p.ParentID equals c.ParentID
					where p.Value1 == TypeValue.Value1 select p);
		}

		[Test]
		public void Enum7([DataSources] string context)
		{
			var v1 = TypeValue.Value1;

			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();
				db.Parent4.Update(p => p.Value1 == v1, p => new Parent4 { Value1 = v1 });
			}
		}

		public enum TestValue
		{
			Value1 = 1,
		}

		[Table("Parent")]
		sealed class TestParent
		{
			[Column] public int       ParentID;
			[Column] public TestValue Value1;
		}

		[Test]
		public void Enum81([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				db.GetTable<TestParent>().Where(p => p.Value1 == TestValue.Value1).ToList();
		}

		internal sealed class LinqDataTypes
		{
			public TestValue ID;
		}

		[Test]
		public void Enum812([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				db.GetTable<LinqDataTypes>()
					.Where(p => p.ID == TestValue.Value1)
					.Count();
		}

		[Test]
		public void Enum82([DataSources] string context)
		{
			var testValue = TestValue.Value1;
			using (var db = GetDataContext(context))
				db.GetTable<TestParent>().Where(p => p.Value1 == testValue).ToList();
		}

		public enum Gender9
		{
			[MapValue("M")] Male,
			[MapValue("F")] Female,
			[MapValue("U")] Unknown,
			[MapValue("O")] Other,
		}

		[Table("Person", IsColumnAttributeRequired=false)]
		public class Person9
		{
			public int     PersonID;
			public string  FirstName = null!;
			public string  LastName = null!;
			public string? MiddleName;
			public Gender9 Gender;
		}

		[Test]
		public void Enum9([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				db.GetTable<Person9>().Where(p => p.PersonID == 1 && p.Gender == Gender9.Male).ToList();
		}

		[Table("Parent")]
		public class ParentObject
		{
			[Column]                      public int   ParentID;
			[Column("Value1", ".Value1")] public Inner Value = new ();

			public class Inner
			{
				public int? Value1;
			}
		}

		[Test]
		public void Inner1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var e = db.GetTable<ParentObject>().First(p => p.ParentID == 1);
				Assert.Multiple(() =>
				{
					Assert.That(e.ParentID, Is.EqualTo(1));
					Assert.That(e.Value.Value1, Is.EqualTo(1));
				});
			}
		}

		[Test]
		public void Inner2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var e = db.GetTable<ParentObject>().First(p => p.ParentID == 1 && p.Value.Value1 == 1);
				Assert.Multiple(() =>
				{
					Assert.That(e.ParentID, Is.EqualTo(1));
					Assert.That(e.Value.Value1, Is.EqualTo(1));
				});
			}
		}

		[Table(Name="Child")]
		public class ChildObject
		{
			[Column] public int ParentID;
			[Column] public int ChildID;

			[Association(ThisKey="ParentID", OtherKey="ParentID")]
			public ParentObject? Parent;
		}

		[Test]
		public void Inner3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var e = db.GetTable<ChildObject>().First(c => c.Parent!.Value.Value1 == 1);
				Assert.That(e.ParentID, Is.EqualTo(1));
			}
		}

		struct MyInt
		{
			public int MyValue;
		}

		[Table(Name="Parent")]
		sealed class MyParent
		{
			[Column] public MyInt ParentID;
			[Column] public int?  Value1;
		}

		sealed class MyMappingSchema : MappingSchema
		{
			public MyMappingSchema()
			{
				SetConvertExpression<long,MyInt>         (n => new MyInt { MyValue = (int)n });
				SetConvertExpression<int,MyInt>          (n => new MyInt { MyValue =      n });
				SetConvertExpression<MyInt,DataParameter>(n => new DataParameter { Value = n.MyValue });
			}
		}

		static readonly MyMappingSchema _myMappingSchema = new ();

		[Test]
		public void MyType1()
		{
			using (var db = new TestDataConnection())
			{
				db.AddMappingSchema(_myMappingSchema);
				var _ = db.GetTable<MyParent>().ToList();
			}
		}

		[Test]
		public void MyType2()
		{
			using (var db = new TestDataConnection())
			{
				db.AddMappingSchema(_myMappingSchema);
				var _ = db.GetTable<MyParent>()
					.Select(t => new MyParent { ParentID = t.ParentID, Value1 = t.Value1 })
					.ToList();
			}
		}

		[Test]
		public void MyType3()
		{
			using (var db = (TestDataConnection) new TestDataConnection())
			{
				db.AddMappingSchema(_myMappingSchema);
				try
				{
					db.Insert(new MyParent { ParentID = new MyInt { MyValue = 1001 }, Value1 = 1001 });
				}
				finally
				{
					db.Parent.Delete(p => p.ParentID >= 1000);
				}
			}
		}

		[Test]
		public void MyType4()
		{
			using (var db = (TestDataConnection)new TestDataConnection())
			{
				db.AddMappingSchema(_myMappingSchema);
				try
				{
					var id = new MyInt { MyValue = 1001 };
					db.GetTable<MyParent>().Insert(() => new MyParent { ParentID = id, Value1 = 1001 });
				}
				finally
				{
					db.Parent.Delete(p => p.ParentID >= 1000);
				}
			}
		}

		[Test]
		public void MyType5()
		{
			using (var db = (TestDataConnection)new TestDataConnection())
			{
				db.AddMappingSchema(_myMappingSchema);
				try
				{
					db.GetTable<MyParent>().Insert(() => new MyParent { ParentID = new MyInt { MyValue = 1001 }, Value1 = 1001 });
				}
				finally
				{
					db.Parent.Delete(p => p.ParentID >= 1000);
				}
			}
		}

		[Table("Parent")]
		sealed class MyParent1
		{
			[Column] public int  ParentID;
			[Column] public int? Value1;

			public string Value2 { get { return "1"; } }

			public int GetValue() { return 2; }
		}

		[Test]
		public void MapIgnore1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					              Parent    .Select(p => new { p.ParentID,   Value2 = "1" }),
					db.GetTable<MyParent1>().Select(p => new { p.ParentID, p.Value2 }));
		}

		[Test]
		public void MapIgnore2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					              Parent    .Select(p => new { p.ParentID,          Length = 1 }),
					db.GetTable<MyParent1>().Select(p => new { p.ParentID, p.Value2.Length }));
		}

		[Test]
		public void MapIgnore3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					              Parent    .Select(p => new { p.ParentID, Value = 2            }),
					db.GetTable<MyParent1>().Select(p => new { p.ParentID, Value = p.GetValue() }));
		}

		public class     Entity    { public int Id { get; set; } }
		public interface IDocument { int Id { get; set; } }
		public class     Document : Entity, IDocument { }

		[Test]
		public void TestMethod()
		{
			using (var db = new TestDataConnection())
			{
				IQueryable<IDocument> query = db.GetTable<Document>();
				var idsQuery = query.Select(s => s.Id);
				var str = idsQuery.ToString(); // Exception
				Assert.That(str, Is.Not.Null);
			}
		}

		[Table("Person")]
		sealed class Table171
		{
			[Column] public Gender Gender;
		}

		[Test]
		public void Issue171Test([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			db.GetTable<Table171>()
				.Where (t => t.Gender == Gender.Male)
				.Select(t => new { value = (int)t.Gender })
				.ToList();
		}

		[Table("Child")]
		interface IChild
		{
			[Column]
			int ChildID { get; set; }
		}

		[Test]
		public void TestInterfaceMapping1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var results = db.GetTable<IChild>().Where(c => c.ChildID == 32).Count();

				Assert.That(results, Is.EqualTo(1));
			}
		}

		[Test]
		public void TestInterfaceMapping2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var results = db.GetTable<IChild>().Where(c => c.ChildID == 32).Select(_ => new { _.ChildID }).ToList();

				Assert.That(results, Has.Count.EqualTo(1));
				Assert.That(results[0].ChildID, Is.EqualTo(32));
			}
		}

		[Table("Person")]
		public class BadMapping
		{
			[Column("FirstName")]
			public int NotInt { get; set; }

			[Column("LastName")]
			public BadEnum BadEnum { get; set; }
		}

		public enum BadEnum
		{
			[MapValue("SOME_VALUE")]
			Value = 1
		}

		[Test]
		public void ColumnMappingException1([DataSources(ProviderName.SqlCe)] string context)
		{
			GetProviderName(context, out var isLinqService);

			using (var db = GetDataContext(context, testLinqService : false, suppressSequentialAccess: true))
			{
				if (isLinqService)
				{
#if NETFRAMEWORK
					var fe = Assert.Throws<FaultException>(() => db.GetTable<BadMapping>().Select(_ => new { _.NotInt }).ToList())!;
#else
					var fe = Assert.Throws<Grpc.Core.RpcException>(() => db.GetTable<BadMapping>().Select(_ => new { _.NotInt }).ToList())!;
#endif
					Assert.That(fe.Message.ToLowerInvariant(), Does.Contain("firstname"));
				}
				else
				{
					var ex = Assert.Throws<LinqToDBConvertException>(() => db.GetTable<BadMapping>().Select(_ => new { _.NotInt }).ToList())!;
					// field name casing depends on database
					Assert.That(ex.ColumnName!.ToLowerInvariant(), Is.EqualTo("firstname"));
				}
			}
		}

		[Test]
		public void ColumnMappingException2([DataSources(ProviderName.SqlCe)] string context)
		{
			GetProviderName(context, out var isLinqService);

			using (var db = GetDataContext(context, suppressSequentialAccess: true))
			{
				var ex = Assert.Throws<LinqToDBConvertException>(() => db.GetTable<BadMapping>().Select(_ => new { _.BadEnum }).ToList())!;
				Assert.That(ex.ColumnName!.ToLowerInvariant(), Is.EqualTo("lastname"));
			}
		}

#region Records

		public record Record(int Id, string Value, string BaseValue) : RecordBase(Id, BaseValue);
		public abstract record RecordBase(int Id, string BaseValue);

		public class RecordLike : RecordLikeBase
		{
			public RecordLike(int Id, string Value, string BaseValue)
				: base(Id, BaseValue)
			{
				this.Value = Value;
			}

			public string Value { get; init; }
		}

		public abstract class RecordLikeBase
		{
			protected RecordLikeBase(int Id, string BaseValue)
			{
				this.Id = Id;
				this.BaseValue = BaseValue;
			}

			public int    Id        { get; init; }
			public string BaseValue { get; init; }
		}

		public class WithInitOnly : WithInitOnlyBase
		{
			public string? Value { get; init; }
		}

		public abstract class WithInitOnlyBase
		{
			public int     Id        { get; init; }
			public string? BaseValue { get; init; }
		}

		[Test]
		public void TestRecordMapping([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<Record>()
					.Property(p => p.Id).IsPrimaryKey()
					.Property(p => p.Value)
					.Property(p => p.BaseValue)
				.Build();

			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable<Record>())
			{
				db.Insert(new Record(1, "One", "OneBase"));
				db.Insert(new Record(2, "Two", "TwoBase"));

				var data = table.OrderBy(r => r.Id).ToArray();

				Assert.That(data, Has.Length.EqualTo(2));
				Assert.Multiple(() =>
				{
					Assert.That(data[0].Id, Is.EqualTo(1));
					Assert.That(data[0].Value, Is.EqualTo("One"));
					Assert.That(data[0].BaseValue, Is.EqualTo("OneBase"));
					Assert.That(data[1].Id, Is.EqualTo(2));
					Assert.That(data[1].Value, Is.EqualTo("Two"));
					Assert.That(data[1].BaseValue, Is.EqualTo("TwoBase"));
				});

				var proj = table.OrderBy(r => r.Id).Select(r => new { r.Id, r.Value, r.BaseValue }).ToArray();

				Assert.That(proj, Has.Length.EqualTo(2));
				Assert.Multiple(() =>
				{
					Assert.That(proj[0].Id, Is.EqualTo(1));
					Assert.That(proj[0].Value, Is.EqualTo("One"));
					Assert.That(proj[0].BaseValue, Is.EqualTo("OneBase"));
					Assert.That(proj[1].Id, Is.EqualTo(2));
					Assert.That(proj[1].Value, Is.EqualTo("Two"));
					Assert.That(proj[1].BaseValue, Is.EqualTo("TwoBase"));
				});
			}
		}

		[Test]
		public void TestRecordLikeMapping([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<RecordLike>()
					.Property(p => p.Id).IsPrimaryKey()
					.Property(p => p.Value)
					.Property(p => p.BaseValue)
				.Build();

			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable<RecordLike>())
			{
				db.Insert(new RecordLike(1, "One", "OneBase"));
				db.Insert(new RecordLike(2, "Two", "TwoBase"));

				var data = table.OrderBy(r => r.Id).ToArray();

				Assert.That(data, Has.Length.EqualTo(2));
				Assert.Multiple(() =>
				{
					Assert.That(data[0].Id, Is.EqualTo(1));
					Assert.That(data[0].Value, Is.EqualTo("One"));
					Assert.That(data[0].BaseValue, Is.EqualTo("OneBase"));
					Assert.That(data[1].Id, Is.EqualTo(2));
					Assert.That(data[1].Value, Is.EqualTo("Two"));
					Assert.That(data[1].BaseValue, Is.EqualTo("TwoBase"));
				});

				var proj = table.OrderBy(r => r.Id).Select(r => new { r.Id, r.Value, r.BaseValue }).ToArray();

				Assert.That(proj, Has.Length.EqualTo(2));
				Assert.Multiple(() =>
				{
					Assert.That(proj[0].Id, Is.EqualTo(1));
					Assert.That(proj[0].Value, Is.EqualTo("One"));
					Assert.That(proj[0].BaseValue, Is.EqualTo("OneBase"));
					Assert.That(proj[1].Id, Is.EqualTo(2));
					Assert.That(proj[1].Value, Is.EqualTo("Two"));
					Assert.That(proj[1].BaseValue, Is.EqualTo("TwoBase"));
				});
			}
		}

		[Test]
		public void TestInitOnly([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<WithInitOnly>()
					.Property(p => p.Id).IsPrimaryKey()
					.Property(p => p.Value)
				.Build();

			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable<WithInitOnly>())
			{
				db.Insert(new WithInitOnly{Id = 1, Value = "One", BaseValue = "OneBase"});
				db.Insert(new WithInitOnly{Id = 2, Value = "Two", BaseValue = "TwoBase"});

				var data = table.OrderBy(r => r.Id).ToArray();

				Assert.That(data, Has.Length.EqualTo(2));
				Assert.Multiple(() =>
				{
					Assert.That(data[0].Id, Is.EqualTo(1));
					Assert.That(data[0].Value, Is.EqualTo("One"));
					Assert.That(data[0].BaseValue, Is.EqualTo("OneBase"));
					Assert.That(data[1].Id, Is.EqualTo(2));
					Assert.That(data[1].Value, Is.EqualTo("Two"));
					Assert.That(data[1].BaseValue, Is.EqualTo("TwoBase"));
				});

				var proj = table.OrderBy(r => r.Id).Select(r => new { r.Id, r.Value, r.BaseValue }).ToArray();

				Assert.That(proj, Has.Length.EqualTo(2));
				Assert.Multiple(() =>
				{
					Assert.That(proj[0].Id, Is.EqualTo(1));
					Assert.That(proj[0].Value, Is.EqualTo("One"));
					Assert.That(proj[0].BaseValue, Is.EqualTo("OneBase"));
					Assert.That(proj[1].Id, Is.EqualTo(2));
					Assert.That(proj[1].Value, Is.EqualTo("Two"));
					Assert.That(proj[1].BaseValue, Is.EqualTo("TwoBase"));
				});
			}
		}

		#endregion

		#region Issue 4113
		public interface IInterface1
		{
		}

		public interface IInterface2
		{
			Guid Id { get; set; }
		}

		public interface IInterface3
		{
			int Id { get; set; }
		}

		[Table("Person")]
		public abstract class BaseModel1
		{
			[Column("UNKNOWN")] public virtual Guid Id { get; set; }
		}

		[Table("Person")]
		public abstract class BaseModel2: IInterface2
		{
			[Column("UNKNOWN")] public virtual Guid Id { get; set; }
		}

		public sealed class NewModel1 : BaseModel1
		{
			[Column("PersonID")] public new int Id { get; set; }
		}

		public sealed class NewModel2 : BaseModel1, IInterface1
		{
			[Column("PersonID")] public new int Id { get; set; }
		}

		public sealed class NewModel3 : BaseModel2
		{
			[Column("PersonID")] public new int Id { get; set; }
		}

		public sealed class NewModel4 : BaseModel1, IInterface3
		{
			[Column("PersonID")] public new int Id { get; set; }
		}

		public sealed class NewModel5 : BaseModel2, IInterface3
		{
			[Column("PersonID")] public new int Id { get; set; }
		}

		[Test]
		public void ColumnReplacedWithNew1([IncludeDataSources(true, ProviderName.SQLiteClassic)] string context)
		{
			using var db = GetDataContext(context);
			db.GetTable<NewModel1>().Where(c => c.Id == -1).ToList();

			var ed = db.MappingSchema.GetEntityDescriptor(typeof(NewModel1));
			Assert.That(ed.Columns, Has.Count.EqualTo(1));
			Assert.That(ed.Columns[0].ColumnName, Is.EqualTo("PersonID"));
		}

		[Test]
		public void ColumnReplacedWithNew2([IncludeDataSources(true, ProviderName.SQLiteClassic)] string context)
		{
			using var db = GetDataContext(context);
			db.GetTable<NewModel2>().Where(c => c.Id == -1).ToList();

			var ed = db.MappingSchema.GetEntityDescriptor(typeof(NewModel2));
			Assert.That(ed.Columns, Has.Count.EqualTo(1));
			Assert.That(ed.Columns[0].ColumnName, Is.EqualTo("PersonID"));
		}

		[Test]
		public void ColumnReplacedWithNew3([IncludeDataSources(true, ProviderName.SQLiteClassic)] string context)
		{
			using var db = GetDataContext(context);
			db.GetTable<NewModel3>().Where(c => c.Id == -1).ToList();

			var ed = db.MappingSchema.GetEntityDescriptor(typeof(NewModel3));
			Assert.That(ed.Columns, Has.Count.EqualTo(1));
			Assert.That(ed.Columns[0].ColumnName, Is.EqualTo("PersonID"));
		}

		[Test]
		public void ColumnReplacedWithNew4([IncludeDataSources(true, ProviderName.SQLiteClassic)] string context)
		{
			using var db = GetDataContext(context);
			db.GetTable<NewModel4>().Where(c => c.Id == -1).ToList();

			var ed = db.MappingSchema.GetEntityDescriptor(typeof(NewModel4));
			Assert.That(ed.Columns, Has.Count.EqualTo(1));
			Assert.That(ed.Columns[0].ColumnName, Is.EqualTo("PersonID"));
		}

		[Test]
		public void ColumnReplacedWithNew5([IncludeDataSources(true, ProviderName.SQLiteClassic)] string context)
		{
			using var db = GetDataContext(context);
			db.GetTable<NewModel5>().Where(c => c.Id == -1).ToList();

			var ed = db.MappingSchema.GetEntityDescriptor(typeof(NewModel5));
			Assert.That(ed.Columns, Has.Count.EqualTo(1));
			Assert.That(ed.Columns[0].ColumnName, Is.EqualTo("PersonID"));
		}
		#endregion

		sealed class StorageTable
		{
			public StorageTable()
			{
			}

			private int _field;

			[Column] public int Field => _field;

			[NotColumn]
			public int TestAccess
			{
				get => _field;
				set => _field = value;
			}
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/279")]
		public void StorageFieldTest([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<StorageTable>();

			// Insert + Select
			db.Insert(new StorageTable() { TestAccess = 5 });

			var record = tb.Single();
			Assert.That(record.TestAccess, Is.EqualTo(5));

			// update
			tb.Where(r => r.Field == 5).Set(r => r.Field, 6).Update();

			record = tb.Single();
			Assert.That(record.TestAccess, Is.EqualTo(6));

			// filter
			record = tb.Where(r => r.Field == 6).SingleOrDefault();
			Assert.That(record, Is.Not.Null);
			Assert.That(record!.TestAccess, Is.EqualTo(6));
		}
	}
}
