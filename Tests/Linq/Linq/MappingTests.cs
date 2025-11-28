using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

#if NETFRAMEWORK
using System.ServiceModel;
#endif

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
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
			using (Assert.EnterMultipleScope())
			{
				Assert.That(value, Is.EqualTo(TypeValue.Value1));
				Assert.That((int)value, Is.EqualTo(10));
			}
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(e.ParentID, Is.EqualTo(1));
					Assert.That(e.Value.Value1, Is.EqualTo(1));
				}
			}
		}

		[Test]
		public void Inner2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var e = db.GetTable<ParentObject>().First(p => p.ParentID == 1 && p.Value.Value1 == 1);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(e.ParentID, Is.EqualTo(1));
					Assert.That(e.Value.Value1, Is.EqualTo(1));
				}
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

		public class     Entity    { [PrimaryKey] public int Id { get; set; } }
		public interface IDocument { int Id { get; set; } }
		public class     Document : Entity, IDocument { }

		[Test]
		public void TestMethod([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Document>();

			var query = db.GetTable<Document>().Select(s => s.Id).ToArray();
		}

		[Table("Person")]
		sealed class Table171
		{
			[Column] public Gender Gender;
		}

		[Test]
		public void Issue171Test([DataSources] string context)
		{
			using var db = GetDataContext(context);
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(1));
					Assert.That(data[0].Value, Is.EqualTo("One"));
					Assert.That(data[0].BaseValue, Is.EqualTo("OneBase"));
					Assert.That(data[1].Id, Is.EqualTo(2));
					Assert.That(data[1].Value, Is.EqualTo("Two"));
					Assert.That(data[1].BaseValue, Is.EqualTo("TwoBase"));
				}

				var proj = table.OrderBy(r => r.Id).Select(r => new { r.Id, r.Value, r.BaseValue }).ToArray();

				Assert.That(proj, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(proj[0].Id, Is.EqualTo(1));
					Assert.That(proj[0].Value, Is.EqualTo("One"));
					Assert.That(proj[0].BaseValue, Is.EqualTo("OneBase"));
					Assert.That(proj[1].Id, Is.EqualTo(2));
					Assert.That(proj[1].Value, Is.EqualTo("Two"));
					Assert.That(proj[1].BaseValue, Is.EqualTo("TwoBase"));
				}
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(1));
					Assert.That(data[0].Value, Is.EqualTo("One"));
					Assert.That(data[0].BaseValue, Is.EqualTo("OneBase"));
					Assert.That(data[1].Id, Is.EqualTo(2));
					Assert.That(data[1].Value, Is.EqualTo("Two"));
					Assert.That(data[1].BaseValue, Is.EqualTo("TwoBase"));
				}

				var proj = table.OrderBy(r => r.Id).Select(r => new { r.Id, r.Value, r.BaseValue }).ToArray();

				Assert.That(proj, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(proj[0].Id, Is.EqualTo(1));
					Assert.That(proj[0].Value, Is.EqualTo("One"));
					Assert.That(proj[0].BaseValue, Is.EqualTo("OneBase"));
					Assert.That(proj[1].Id, Is.EqualTo(2));
					Assert.That(proj[1].Value, Is.EqualTo("Two"));
					Assert.That(proj[1].BaseValue, Is.EqualTo("TwoBase"));
				}
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(1));
					Assert.That(data[0].Value, Is.EqualTo("One"));
					Assert.That(data[0].BaseValue, Is.EqualTo("OneBase"));
					Assert.That(data[1].Id, Is.EqualTo(2));
					Assert.That(data[1].Value, Is.EqualTo("Two"));
					Assert.That(data[1].BaseValue, Is.EqualTo("TwoBase"));
				}

				var proj = table.OrderBy(r => r.Id).Select(r => new { r.Id, r.Value, r.BaseValue }).ToArray();

				Assert.That(proj, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(proj[0].Id, Is.EqualTo(1));
					Assert.That(proj[0].Value, Is.EqualTo("One"));
					Assert.That(proj[0].BaseValue, Is.EqualTo("OneBase"));
					Assert.That(proj[1].Id, Is.EqualTo(2));
					Assert.That(proj[1].Value, Is.EqualTo("Two"));
					Assert.That(proj[1].BaseValue, Is.EqualTo("TwoBase"));
				}
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

			[PrimaryKey] public int Id;

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
			db.Insert(new StorageTable() { Id = 1, TestAccess = 5 });

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

		#region Issue 3060
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3060")]
		public void Issue3060Test([DataSources(TestProvName.AllAccess)] string context)
		{
			var ms = new MappingSchema();
			ms.SetConvertExpression<byte[], Blob16AsGuidType>(x => new Blob16AsGuidType(x));
			ms.SetConvertExpression<Blob16AsGuidType, byte[]?>(x => x.Bytes);
			ms.SetConvertExpression<Blob16AsGuidType, DataParameter>(x => DataParameter.Blob(null, x.Bytes));

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable<Issue3060Table>();

			var row = new Issue3060Table()
			{
				Uid = new Blob16AsGuidType(TestData.Guid1)
			};

			db.Update(row);
		}

		[Table]
		sealed class Issue3060Table
		{
			[Column(DataType = DataType.Int64, Length = 8, Precision = 19, Scale = 0), PrimaryKey, NotNull] public long Id { get; set; }
			[Column(DataType = DataType.VarBinary, Length = 16, Precision = 0, Scale = 0), Nullable] public Blob16AsGuidType? Uid { get; set; }
		}

		sealed class Blob16AsGuidType
		{
			public Blob16AsGuidType(byte[] value)
			{
				Bytes = value;
			}

			public Blob16AsGuidType(Guid value)
			{
				Guid = value;
			}

			private byte[]? _bytes;

			public byte[]? Bytes
			{
				get { return _bytes; }
				set
				{
					_bytes = value;
					_guid = value == null ? Guid.Empty : new Guid(value);
				}
			}

			private Guid _guid;

			public Guid Guid
			{
				get { return _guid; }
				set
				{
					_guid = value;
					_bytes = value == Guid.Empty ? null : value.ToByteArray();
				}
			}
		}
		#endregion

		#region Issue 3117

		[ActiveIssue(Configurations = [TestProvName.AllDB2, TestProvName.AllInformix, TestProvName.AllMySqlConnector, TestProvName.AllOracle, TestProvName.AllPostgreSQL, TestProvName.AllSQLite])]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3117")]
		public void Issue3117Test1([DataSources(false, TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();
			ms.SetGenericConvertProvider(typeof(IdConverter<>));

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable<User>();

			var userId = new Id<User>(5);
			db.InsertWithIdentity(new User(userId));

			var users = db.GetTable<User>().ToList();
			var user = db.GetTable<User>().FirstOrDefault(u => u.Id == userId);

			var userIds = new[]{userId};
			user = db.GetTable<User>().FirstOrDefault(u => userIds.Contains(u.Id));
		}

		[ActiveIssue(Configurations = [TestProvName.AllDB2, TestProvName.AllInformix, TestProvName.AllMySqlConnector, TestProvName.AllOracle, TestProvName.AllPostgreSQL, TestProvName.AllSQLite])]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3117")]
		public void Issue3117Test2([DataSources(false, TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();
			ms.SetDataType(typeof(Id<User>), DataType.Int32);
			ms.SetValueToSqlConverter(typeof(Id<User>), (sb, dt, v) => sb.Append(((Id<User>)v).Value));

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable<User>();

			var userId = new Id<User>(5);
			db.InsertWithIdentity(new User(userId));

			var users = db.GetTable<User>().ToList();
			var user = db.GetTable<User>().FirstOrDefault(u => u.Id == userId);

			var userIds = new[]{userId};
			user = db.GetTable<User>().FirstOrDefault(u => userIds.Contains(u.Id));
		}

		class Id<T> : IConvertible
		{
			public int Value { get; set; }

			public Id(int id)
			{
				Value = id;
			}

			public Id(long id)
			{
				Value = (int)id;
			}

			public TypeCode GetTypeCode() => TypeCode.Int32;

			public int ToInt32(IFormatProvider? provider) => Value;
			public long ToInt64(IFormatProvider? provider) => Value;

			public bool ToBoolean(IFormatProvider? provider) => throw new NotImplementedException();
			public char ToChar(IFormatProvider? provider) => throw new NotImplementedException();
			public sbyte ToSByte(IFormatProvider? provider) => throw new NotImplementedException();
			public byte ToByte(IFormatProvider? provider) => throw new NotImplementedException();
			public short ToInt16(IFormatProvider? provider) => throw new NotImplementedException();
			public ushort ToUInt16(IFormatProvider? provider) => throw new NotImplementedException();
			public uint ToUInt32(IFormatProvider? provider) => throw new NotImplementedException();
			public ulong ToUInt64(IFormatProvider? provider) => throw new NotImplementedException();
			public float ToSingle(IFormatProvider? provider) => throw new NotImplementedException();
			public double ToDouble(IFormatProvider? provider) => throw new NotImplementedException();
			public decimal ToDecimal(IFormatProvider? provider) => throw new NotImplementedException();
			public DateTime ToDateTime(IFormatProvider? provider) => throw new NotImplementedException();
			public string ToString(IFormatProvider? provider) => throw new NotImplementedException();

			public object ToType(Type conversionType, IFormatProvider? provider) => Convert.ChangeType(Value, conversionType);
			public static implicit operator int(Id<T> id) => (int)id.Value;
			public static implicit operator long(Id<T> id) => id.Value;
		}

		[Table]
		sealed class User
		{
			public User() { Id = new Id<User>(0); }
			public User(Id<User> id) { Id = id; }

			[Column(IsPrimaryKey = true, DataType = DataType.Int32, CanBeNull = false)]
			public Id<User> Id { get; set; }
		}

		sealed class IdConverter<T> : IGenericInfoProvider
		{
			public void SetInfo(MappingSchema mappingSchema)
			{
				// just check call site and you will see it cannot work
				mappingSchema.SetDataType(typeof(Id<T>), DataType.Int32);
				mappingSchema.SetValueToSqlConverter(typeof(Id<T>), (sb, dt, o) => sb.Append(((Id<T>)o).Value));
			}
		}
		#endregion

		struct RecordId
		{
			public RecordId(int value)
			{
				Value = value;
			}

			public int Value { get; set; }

			public static RecordId  From(int value)  => new RecordId(value);
			public static RecordId? From(int? value) => value == null ? null : new RecordId(value.Value);

			public static implicit operator int?(RecordId? id) => id?.Value;
		}

		[Table("Parent")]
		sealed class RecordTable
		{
			[Column] public int       ParentID { get; set; }
			[Column] public RecordId? Value1   { get; set; }
		}

		private static MappingSchema SetupStructMapping(bool useExpressions, bool addNullCheck, bool mapNull)
		{
			// use unique name to avoid mapping conflicts due to use of non-detectable mapping changes (e.g. defaultValue having different values)
			var ms = new MappingSchema($"{useExpressions}{addNullCheck}{mapNull}");

			// note that it affects only convert expressions and doesn't replace default type value
			// which results in different behavior for expression-based and delegate-based mappings
			// as delegate-based conversions doesn't have addNullCheck feature
			var defaultValue = mapNull ? 6 : (int?)null;

			if (useExpressions)
			{
				ms.SetConvertExpression<RecordId, int>(id => id.Value, addNullCheck: addNullCheck);
				ms.SetConvertExpression<RecordId, int?>(id => id.Value, addNullCheck: addNullCheck);
				ms.SetConvertExpression<int, RecordId>(value => RecordId.From(value), addNullCheck: addNullCheck);
				ms.SetConvertExpression<int, RecordId?>(g => RecordId.From(g), addNullCheck: addNullCheck);
				ms.SetConvertExpression<int?, RecordId>(g => g == null ? default : RecordId.From((int)g), addNullCheck: addNullCheck);
				ms.SetConvertExpression<int?, RecordId?>(value => RecordId.From(value), addNullCheck: addNullCheck);
				ms.SetConvertExpression<RecordId, DataParameter>(id => new DataParameter { DataType = DataType.Int32, Value = id.Value }, addNullCheck: addNullCheck);

				ms.SetConvertExpression<RecordId?, int>(id => id == (int?)null ? (defaultValue ?? default) : id.Value.Value, addNullCheck: addNullCheck);
				ms.SetConvertExpression<RecordId?, int?>(id => id != null ? id.Value : defaultValue, addNullCheck: addNullCheck);
				ms.SetConvertExpression<RecordId?, DataParameter>(id => new DataParameter { DataType = DataType.Int32, Value = id != null ? id.Value : defaultValue }, addNullCheck: addNullCheck);
			}
			else
			{
				ms.SetConverter<RecordId, int>(id => id.Value);
				ms.SetConverter<RecordId, int?>(id => id.Value);
				ms.SetConverter<int, RecordId>(RecordId.From);
				ms.SetConverter<int, RecordId?>(g => RecordId.From(g));
				ms.SetConverter<int?, RecordId>(g => g == null ? default : RecordId.From((int)g));
				ms.SetConverter<int?, RecordId?>(RecordId.From);
				ms.SetConverter<RecordId, DataParameter>(id => new DataParameter { DataType = DataType.Int32, Value = id.Value });

				ms.SetConverter<RecordId?, int>(id => id == (int?)null ? (defaultValue ?? default) : id.Value.Value);
				ms.SetConverter<RecordId?, int?>(id => id?.Value ?? defaultValue);
				ms.SetConverter<RecordId?, DataParameter>(id => new DataParameter { DataType = DataType.Int32, Value = id?.Value ?? defaultValue });
			}

			return ms;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4539")]
		public void StructMapping_Value([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool useExpressions, [Values] bool addNullCheck, [Values] bool mapNull)
		{
			using var db = GetDataContext(context, SetupStructMapping(useExpressions, addNullCheck, mapNull));

			var tenderIds = new List<RecordId?>() { new RecordId(5), new RecordId(3), new RecordId(4), null };

			var cnt = db.GetTable<RecordTable>()
				.Where(i => i.Value1!.Value == tenderIds[0] || i.Value1!.Value == tenderIds[1] || i.Value1!.Value == tenderIds[2] || i.Value1!.Value == tenderIds[3])
				.Count();

			Assert.That(cnt, Is.EqualTo(mapNull ? 3 : 4));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4539")]
		public void StructMapping_Value_IntList([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool useExpressions, [Values] bool addNullCheck, [Values] bool mapNull)
		{
			using var db = GetDataContext(context, SetupStructMapping(useExpressions, addNullCheck, mapNull));

			var tenderIds = new List<int?>() { 5, 3, 4, null };

			var cnt = db.GetTable<RecordTable>().Where(i => i.Value1!.Value == tenderIds[0] || i.Value1!.Value == tenderIds[1] || i.Value1!.Value == tenderIds[2] || i.Value1!.Value == tenderIds[3]).Count();

			Assert.That(cnt, Is.EqualTo(mapNull ? 3 : 4));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4539")]
		public void StructMapping_Collection([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool useExpressions, [Values] bool addNullCheck, [Values] bool mapNull)
		{
			using var db = GetDataContext(context, SetupStructMapping(useExpressions, addNullCheck, mapNull));

			var tenderIds = new List<RecordId?>() { new RecordId(5), new RecordId(3), new RecordId(4), null };

			var cnt = db.GetTable<RecordTable>().Where(i => tenderIds.Contains(i.Value1!.Value)).Count();

			Assert.That(cnt, Is.EqualTo(mapNull ? 3 : 4));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4539")]
		public void StructMapping_Collection_IntList([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool useExpressions, [Values] bool addNullCheck, [Values] bool mapNull)
		{
			using var db = GetDataContext(context, SetupStructMapping(useExpressions, addNullCheck, mapNull));

			var tenderIds = new List<int?>() { 5, 3, 4, null };

			var cnt = db.GetTable<RecordTable>().Where(i => tenderIds.Contains(i.Value1!.Value)).Count();

			Assert.That(cnt, Is.EqualTo(4));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4539")]
		public void StructMapping_EmptyCollection([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool useExpressions, [Values] bool addNullCheck, [Values] bool mapNull)
		{
			using var db = GetDataContext(context, SetupStructMapping(useExpressions, addNullCheck, mapNull));

			var tenderIds = new List<RecordId?>();

			var cnt = db.GetTable<RecordTable>().Where(i => tenderIds.Contains(i.Value1!.Value)).Count();

			Assert.That(cnt, Is.Zero);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4539")]
		public void StructMapping_EmptyCollection_IntList([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool useExpressions, [Values] bool addNullCheck, [Values] bool mapNull)
		{
			using var db = GetDataContext(context, SetupStructMapping(useExpressions, addNullCheck, mapNull));

			var tenderIds = new List<int?>();

			var cnt = db.GetTable<RecordTable>().Where(i => tenderIds.Contains(i.Value1!.Value)).Count();

			Assert.That(cnt, Is.Zero);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4539")]
		public void StructMapping_Enumerable([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool useExpressions, [Values] bool addNullCheck, [Values] bool mapNull)
		{
			using var db = GetDataContext(context, SetupStructMapping(useExpressions, addNullCheck, mapNull));

			var tenderIds = new ArrayList() { new RecordId(5), new RecordId(3), new RecordId(4), null };

			var cnt = db.GetTable<RecordTable>().Where(i => tenderIds.Contains(i.Value1!.Value)).Count();

			Assert.That(cnt, Is.EqualTo(mapNull ? 3 : 4));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4539")]
		public void StructMapping_Enumerable_IntList([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool useExpressions, [Values] bool addNullCheck, [Values] bool mapNull)
		{
			using var db = GetDataContext(context, SetupStructMapping(useExpressions, addNullCheck, mapNull));

			var tenderIds = new ArrayList() { 5, 3, 4, null };

			// not supported case
			Assert.That(() => db.GetTable<RecordTable>().Where(i => tenderIds.Contains(i.Value1!.Value)).Count(), Throws.TypeOf<InvalidCastException>());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4539")]
		public void StructMapping_EmptyEnumerable([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool useExpressions, [Values] bool addNullCheck, [Values] bool mapNull)
		{
			using var db = GetDataContext(context, SetupStructMapping(useExpressions, addNullCheck, mapNull));

			var tenderIds = new ArrayList();

			var cnt = db.GetTable<RecordTable>().Where(i => tenderIds.Contains(i.Value1!.Value)).Count();

			Assert.That(cnt, Is.Zero);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4539")]
		public void StructMapping_MixedEnumerable([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool useExpressions, [Values] bool addNullCheck, [Values] bool mapNull)
		{
			using var db = GetDataContext(context, SetupStructMapping(useExpressions, addNullCheck, mapNull));

			var tenderIds = new ArrayList() { new RecordId(5), 3, new RecordId(4), null };

			// not supported case
			Assert.That(() => db.GetTable<RecordTable>().Where(i => tenderIds.Contains(i.Value1!.Value)).Count(), Throws.TypeOf<InvalidCastException>());
		}

		#region Issue 5057

		static class Issue5057
		{
			[Table]
			public sealed class Tender
			{
				[Column] public TenderId Id { get; set; }
				[Column] public string? Name { get; set; }

				public static readonly Tender[] Data =
				[
					new() { Id = TenderId.From(TestData.Guid1), Name = "Name 1" },
					new() { Id = TenderId.From(TestData.Guid2), Name = "Name 2" },
					new() { Id = TenderId.From(TestData.Guid3), Name = "Name 3" },
				];
			}

#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
			[ScalarType]
			public struct TenderId
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning restore CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
			{
				public Guid Value { get; set; }
				public static TenderId From(Guid value) => new TenderId { Value = value };
				public static TenderId? From(Guid? value) => value.HasValue ? new TenderId { Value = value.Value } : null;

				public static bool operator ==(TenderId a, TenderId b) => a.Value == b.Value;
				public static bool operator !=(TenderId a, TenderId b) => !(a == b);
				public static bool operator ==(TenderId a, Guid b) => a.Value == b;
				public static bool operator !=(TenderId a, Guid b) => !(a == b);
				public static bool operator ==(Guid a, TenderId b) => a == b.Value;
				public static bool operator !=(Guid a, TenderId b) => !(a == b);

				public static MappingSchema GetMappings()
				{
					var ms = new MappingSchema();

					ms.SetConverter<TenderId, Guid>(id => id.Value);
					ms.SetConverter<TenderId, Guid?>(id => id.Value);
					ms.SetConverter<TenderId?, Guid>(id => id?.Value ?? default);
					ms.SetConverter<TenderId?, Guid?>(id => id?.Value);
					ms.SetConverter<Guid, TenderId>(From);
					ms.SetConverter<Guid, TenderId?>(g => From(g));
					ms.SetConverter<Guid?, TenderId>(g => g == null ? default : From((Guid)g));
					ms.SetConverter<Guid?, TenderId?>(From);

					ms.SetConverter<TenderId, LinqToDB.Data.DataParameter>(id => new LinqToDB.Data.DataParameter { DataType = DataType.Guid, Value = id.Value });
					ms.SetConverter<TenderId?, LinqToDB.Data.DataParameter>(id => new LinqToDB.Data.DataParameter { DataType = DataType.Guid, Value = id?.Value });
					// sqlite.ms returns byte[]
					ms.SetConverter<byte[], TenderId>(raw => From(new Guid(raw)));

					return ms;
				}
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5057")]
		public void Issue5057Test([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context, Issue5057.TenderId.GetMappings());
			using var tb = db.CreateLocalTable(Issue5057.Tender.Data);

			var ids = new List<Issue5057.TenderId> { Issue5057.Tender.Data[0].Id, Issue5057.Tender.Data[2].Id };

			var result = tb
				.Where(i => ids.Any(id => id == i.Id))
				.FirstOrDefault();
		}
		#endregion

		#region issue 4798

#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
		struct TenderId
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning restore CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
		{
			public Guid Value { get; set; }
			public static TenderId From(Guid value) => new TenderId { Value = value };
			public static TenderId? From(Guid? value) => value.HasValue ? new TenderId { Value = value.Value } : null;

			public static bool operator ==(TenderId a, Guid b) => a.Value == b;
			public static bool operator !=(TenderId a, Guid b) => !(a == b);

			public static implicit operator string(TenderId tenderId) => tenderId.Value.ToString();
			public static implicit operator Guid(TenderId tenderId) => tenderId.Value;

			internal static MappingSchema LinqToDbMapping()
			{
				var ms = new MappingSchema();

				ms.SetConverter<TenderId, Guid>(id => id.Value);
				ms.SetConverter<TenderId, Guid?>(id => id.Value);
				ms.SetConverter<TenderId?, Guid>(id => id?.Value ?? default);
				ms.SetConverter<TenderId?, Guid?>(id => id?.Value);
				ms.SetConverter<Guid, TenderId>(From);
				ms.SetConverter<Guid, TenderId?>(g => From(g));
				ms.SetConverter<Guid?, TenderId>(g => g == null ? default : From((Guid)g));
				ms.SetConverter<Guid?, TenderId?>(From);

				ms.SetConverter<TenderId, DataParameter>(id => new DataParameter { DataType = DataType.Guid, Value = id.Value });
				ms.SetConverter<TenderId?, DataParameter>(id => new DataParameter { DataType = DataType.Guid, Value = id?.Value });

				return ms;
			}
		}

		[Table("tender")]
		sealed class Tender
		{
			[Column("id")]
			public TenderId Id { get; set; }

			[Column("name")]
			public string? Name { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4798")]
		public void Issue4798Test([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context, TenderId.LinqToDbMapping());
			using var tb = db.CreateLocalTable<Tender>();

			var tenderIdsGuid = new List<Guid> { TestData.Guid1, TestData.Guid2 };
			db.GetTable<Tender>().Where(i => tenderIdsGuid.Contains(i.Id)).Any();

			TenderId? tenderId = new TenderId { Value = TestData.Guid1 };
			db.GetTable<Tender>().Where(i => tenderId != null && i.Id == tenderId).Any();
		}

		#endregion

		#region Issue 4437

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4437")]
		public void Issue4437Test1([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(new Issue4437Record[] { new("value") });

			var result = db.GetTable<Issue4437Record>().ToArray();

			Assert.That(result, Has.Length.EqualTo(1));
			Assert.That(result[0].SomeColumn, Is.EqualTo("value"));
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4437")]
		public void Issue4437Test2([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(new Issue4437Record[] { new("value") });

			var result = db.Query<Issue4437Record>("select some_column from test4437").ToArray();

			Assert.That(result, Has.Length.EqualTo(1));
			Assert.That(result[0].SomeColumn, Is.EqualTo("value"));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4437")]
		public void Issue4437Test3([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(new Issue4437Record[] { new("value") });

			var result = db.Query<Issue4437Record>("select some_column as SomeColumn from test4437").ToArray();

			Assert.That(result, Has.Length.EqualTo(1));
			Assert.That(result[0].SomeColumn, Is.EqualTo("value"));
		}

		[Table("test4437")]
		sealed record Issue4437Record([property: Column("some_column")] string SomeColumn);
		#endregion

		#region Issue 1833
		[Test(Description = "https://github.com/linq2db/linq2db/issues/1833")]
		public void Issue1833Test1([DataSources] string context)
		{
			var fb = new FluentMappingBuilder()
				.Entity<MyPerson>()
					.Ignore(e => e.MiddleName)
					.HasAttribute(e => e.MiddleName, new ExpressionMethodAttribute(nameof(MyPerson.FullName)) {IsColumn = true})
				.Build();

			using var db = GetDataContext(context, fb.MappingSchema);

			var record = db.GetTable<MyPerson>().Where(e => e.ID == 1).Single();

			Assert.That(record.MiddleName, Is.EqualTo($"{record.FirstName}:{record.LastName}"));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1833")]
		public void Issue1833Test2([DataSources] string context)
		{
			var entityDescriptor = MappingSchema.Default.GetEntityDescriptor(typeof(MyPerson));
			var columnDescriptor = entityDescriptor.Columns.Single(c => c.MemberName == nameof(MyPerson.MiddleName));

			var fb = new FluentMappingBuilder().Entity<MyPerson>();
			fb.HasAttribute(columnDescriptor.MemberInfo, new NotColumnAttribute());
			fb.HasAttribute(columnDescriptor.MemberInfo, new ExpressionMethodAttribute(nameof(MyPerson.FullName)) { IsColumn = true });

			using var db = GetDataContext(context, fb.Build().MappingSchema);

			var record = db.GetTable<MyPerson>().Where(e => e.ID == 1).Single();

			Assert.That(record.MiddleName, Is.EqualTo($"{record.FirstName}:{record.LastName}"));
		}

		[Table("Person")]
		sealed class MyPerson
		{
			[Column("PersonID"), PrimaryKey] public int ID { get; set; }
			[Column(CanBeNull = false)] public string FirstName { get; set; } = null!;
			[Column(CanBeNull = false)] public string LastName { get; set; } = null!;
			[Column] public string? MiddleName { get; set; }
			public static Expression<Func<MyPerson, string>> FullName() => e => $"{e.FirstName}:{e.LastName}";
		}
		#endregion

		#region Issue 2362
		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2362")]
		public void Issue2362Test([DataSources] string context, [Values] bool value)
		{
			var fb = new FluentMappingBuilder()
				.Entity<Issue2362Table>()
					.Property(p => p.Value)
					.IsNullable()
					.HasDataType(DataType.VarChar)
					.HasLength(4)
					.HasConversion(
						cs => cs ? "+" : "",
						db => db == "+",
						true);

			using var db = GetDataContext(context, fb.Build().MappingSchema);
			using var tb = db.CreateLocalTable(Issue2362Raw.Data);

			var res = db.GetTable<Issue2362Table>().Where(r => r.Value == value).OrderBy(r => r.Id).ToArray();

			if (value)
			{
				Assert.That(res, Has.Length.EqualTo(1));
				Assert.That(res[0].Value, Is.True);
			}
			else
			{
				Assert.That(res, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0].Value, Is.False);
					Assert.That(res[1].Value, Is.False);
				}
			}
		}

		[Table("Issue2362Table")]
		sealed class Issue2362Table
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public bool Value { get; set; }
		}

		[Table("Issue2362Table")]
		sealed class Issue2362Raw
		{
			[PrimaryKey] public int Id { get; set; }
			[Column(DataType = DataType.Char, Length = 4)] public string? Value { get; set; }

			public static readonly Issue2362Raw[] Data =
			[
				new Issue2362Raw() { Id = 1, Value = null },
				new Issue2362Raw() { Id = 2, Value = "+" },
				new Issue2362Raw() { Id = 3, Value = "" },
			];
		}
		#endregion

		sealed class TimeSpanAsTicks
		{
			[PrimaryKey] public Guid Id { get; set; }
			[Column] public TimeSpan Value { get; set; }
		}

		[Test]
		public void Test_DefaultMappingOverride([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var ms = new MappingSchema();
			ms.SetDataType(typeof(TimeSpan), DataType.Int64);

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable<TimeSpanAsTicks>();

			Guid? id = Guid.NewGuid();

			var query = tb.Where(r => r.Value == -new TimeSpan(1200000000L));

			query.ToArray();
		}

		#region Issue 4955

		record MappingTypingByConstant<T>(int Id, T Value);

		[ActiveIssue("CAST to BIGINT doesn't work in MariaDB and MySQL 5.7", Configurations = [TestProvName.AllMariaDB, TestProvName.AllMySql57], SkipForLinqService = true)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4955")]
		public void MappingTypingByConstant_FromEnumerable_Int64([DataSources(TestProvName.AllAccess)] string context, [Values(null, 1L)] long? first)
		{
			using var db = GetDataContext(context);

			Query.ClearCaches();
			Test(first);
			Test(2147483648L);

			void Test(long? value)
			{
				var res = db.Person
					.InnerJoin(
						new MappingTypingByConstant<long?>[] { new(1, value) }.AsQueryable(),
						(entity, arg) => entity.ID == arg.Id,
						(entity, arg) => new { arg.Id, arg.Value })
					.ToArray();

				Assert.That(res, Has.Length.EqualTo(1));
				Assert.That(res[0].Value, Is.EqualTo(value));
			}
		}

		[ActiveIssue("CAST to BIGINT doesn't work in MariaDB", Configuration = TestProvName.AllMariaDB, SkipForLinqService = true)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4955")]
		public void MappingTypingByConstant_FromQuery_Int64([DataSources(TestProvName.AllAccess)] string context, [Values] bool inline, [Values(null, 1L)] long? first)
		{
			using var db = GetDataContext(context);
			db.InlineParameters = inline;

			var value = first;
			var query = db.Person.Select(r => new { Id = r.ID, Value = Sql.AsSql(value) }).AsSubQuery();
			query.ClearCache();

			var res = query.ToArray();
			Assert.That(res, Has.Length.EqualTo(4));
			Assert.That(res[0].Value, Is.EqualTo(value));

			value = 2147483648L;

			res = query.ToArray();
			Assert.That(res, Has.Length.EqualTo(4));
			Assert.That(res[0].Value, Is.EqualTo(value));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4955")]
		public void MappingTypingByConstant_FromEnumerable_UInt64([DataSources(TestProvName.AllAccess)] string context, [Values(null, 1ul)] ulong? first)
		{
			using var db = GetDataContext(context);

			Query.ClearCaches();
			Test(first);
			Test(2147483648ul);

			void Test(ulong? value)
			{
				var res = db.Person
					.InnerJoin(
						new MappingTypingByConstant<ulong?>[] { new(1, Sql.AsSql(value)) }.AsQueryable(),
						(entity, arg) => entity.ID == arg.Id,
						(entity, arg) => new { arg.Id, arg.Value })
					.ToArray();

				Assert.That(res, Has.Length.EqualTo(1));
				Assert.That(res[0].Value, Is.EqualTo(value));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4955")]
		public void MappingTypingByConstant_FromQuery_UInt64([DataSources(TestProvName.AllAccess)] string context, [Values] bool inline, [Values(null, 1ul)] ulong? first)
		{
			using var _ = inline && context.IsAnyOf(TestProvName.AllPostgreSQL) ? new DisableBaseline("TODO: https://github.com/linq2db/linq2db/issues/5169") : null;

			using var db = GetDataContext(context);
			db.InlineParameters = inline;

			var value = first;
			var query = db.Person.Select(r => new { Id = r.ID, Value = Sql.AsSql(value) }).AsSubQuery();
			query.ClearCache();

			var res = query.ToArray();
			Assert.That(res, Has.Length.EqualTo(4));
			Assert.That(res[0].Value, Is.EqualTo(value));

			value = 2147483648ul;

			res = query.ToArray();
			Assert.That(res, Has.Length.EqualTo(4));
			Assert.That(res[0].Value, Is.EqualTo(value));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4955")]
		public void MappingTypingByConstant_FromEnumerable_UInt32([DataSources(TestProvName.AllAccess)] string context, [Values(null, 1u)] uint? first)
		{
			using var db = GetDataContext(context);

			Query.ClearCaches();
			Test(first);
			Test(2147483648u);

			void Test(uint? value)
			{
				var res = db.Person
					.InnerJoin(
						new MappingTypingByConstant<uint?>[] { new(1, value) }.AsQueryable(),
						(entity, arg) => entity.ID == arg.Id,
						(entity, arg) => new { arg.Id, arg.Value })
					.ToArray();

				Assert.That(res, Has.Length.EqualTo(1));
				Assert.That(res[0].Value, Is.EqualTo(value));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4955")]
		public void MappingTypingByConstant_FromQuery_UInt32([DataSources(TestProvName.AllAccess)] string context, [Values] bool inline, [Values(null, 1u)] uint? first)
		{
			using var db = GetDataContext(context);
			db.InlineParameters = inline;

			var value = first;
			var query = db.Person.Select(r => new { Id = r.ID, Value = Sql.AsSql(value) }).AsSubQuery();
			query.ClearCache();

			var res = query.ToArray();
			Assert.That(res, Has.Length.EqualTo(4));
			Assert.That(res[0].Value, Is.EqualTo(value));

			value = 2147483648u;

			res = query.ToArray();
			Assert.That(res, Has.Length.EqualTo(4));
			Assert.That(res[0].Value, Is.EqualTo(value));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4955")]
		public void MappingTypingByConstant_FromEnumerable_Decimal([DataSources(TestProvName.AllAccess)] string context, [Values] bool isNull)
		{
			using var db = GetDataContext(context);

			Query.ClearCaches();
			Test(isNull ? (decimal?)null : 1m);
			Test(2147483648.123m);

			void Test(decimal? value)
			{
				var res = db.Person
					.InnerJoin(
						new MappingTypingByConstant<decimal?>[] { new(1, value) }.AsQueryable(),
						(entity, arg) => entity.ID == arg.Id,
						(entity, arg) => new { arg.Id, arg.Value })
					.ToArray();

				Assert.That(res, Has.Length.EqualTo(1));
				Assert.That(res[0].Value, Is.EqualTo(value));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4955")]
		public void MappingTypingByConstant_FromQuery_Decimal([DataSources(TestProvName.AllAccess)] string context, [Values] bool inline, [Values] bool isNull)
		{
			using var db = GetDataContext(context);
			db.InlineParameters = inline;

			var value = isNull ? (decimal?)null : 1m;
			var query = db.Person.Select(r => new { Id = r.ID, Value = Sql.AsSql(value) }).AsSubQuery();
			query.ClearCache();

			var res = query.ToArray();
			Assert.That(res, Has.Length.EqualTo(4));
			Assert.That(res[0].Value, Is.EqualTo(value));

			value = 2147483648.123m;

			res = query.ToArray();
			Assert.That(res, Has.Length.EqualTo(4));
			Assert.That(res[0].Value, Is.EqualTo(value));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4955")]
		public void MappingTypingByConstant_FromEnumerable_Double([DataSources(TestProvName.AllAccess)] string context, [Values(null, 0D)] double? first)
		{
			using var db = GetDataContext(context);

			Query.ClearCaches();
			Test(first);
			Test(3147483648D);

			void Test(double? value)
			{
				var res = db.Person
					.InnerJoin(
						new MappingTypingByConstant<double?>[] { new(1, value) }.AsQueryable(),
						(entity, arg) => entity.ID == arg.Id,
						(entity, arg) => new { arg.Id, arg.Value })
					.ToArray();

				Assert.That(res, Has.Length.EqualTo(1));
				Assert.That(res[0].Value, Is.EqualTo(value));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4955")]
		public void MappingTypingByConstant_FromQuery_Double([DataSources] string context, [Values] bool inline, [Values(null, 0D)] double? first)
		{
			using var db = GetDataContext(context);
			db.InlineParameters = inline;

			var value = first;
			var query = db.Person.Select(r => new { Id = r.ID, Value = Sql.AsSql(value) }).AsSubQuery();
			query.ClearCache();

			var res = query.ToArray();
			Assert.That(res, Has.Length.EqualTo(4));
			Assert.That(res[0].Value, Is.EqualTo(value));

			value = 3147483648D;

			res = query.ToArray();
			Assert.That(res, Has.Length.EqualTo(4));
			Assert.That(res[0].Value, Is.EqualTo(value));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4955")]
		public void MappingTypingByConstant_FromEnumerable_Float([DataSources(TestProvName.AllAccess)] string context, [Values(null, 0F)] float? first)
		{
			using var db = GetDataContext(context);

			Query.ClearCaches();
			Test(first);
			Test(3147483648F);

			void Test(float? value)
			{
				var res = db.Person
					.InnerJoin(
						new MappingTypingByConstant<float?>[] { new(1, value) }.AsQueryable(),
						(entity, arg) => entity.ID == arg.Id,
						(entity, arg) => new { arg.Id, arg.Value })
					.ToArray();

				Assert.That(res, Has.Length.EqualTo(1));
				Assert.That(res[0].Value, Is.EqualTo(value));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4955")]
		public void MappingTypingByConstant_FromQuery_Float([DataSources] string context, [Values] bool inline, [Values(null, 0F)] float? first)
		{
			using var db = GetDataContext(context);
			db.InlineParameters = inline;

			var value = first;
			var query = db.Person.Select(r => new { Id = r.ID, Value = Sql.AsSql(value) }).AsSubQuery();
			query.ClearCache();

			var res = query.ToArray();
			Assert.That(res, Has.Length.EqualTo(4));
			Assert.That(res[0].Value, Is.EqualTo(value));

			value = 3147483648F;

			res = query.ToArray();
			Assert.That(res, Has.Length.EqualTo(4));
			Assert.That(res[0].Value, Is.EqualTo(value));
		}
		#endregion
	}
}
