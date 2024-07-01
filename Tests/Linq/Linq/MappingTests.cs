using System;
using System.Linq;
#if NET472
using System.ServiceModel;
#endif

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	using LinqToDB.Expressions;

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

			Assert.AreEqual(TypeValue.Value1, value);
			Assert.AreEqual(10,               (int)value);
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
				Assert.AreEqual(1, e.ParentID);
				Assert.AreEqual(1, e.Value.Value1);
			}
		}

		[Test]
		public void Inner2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var e = db.GetTable<ParentObject>().First(p => p.ParentID == 1 && p.Value.Value1 == 1);
				Assert.AreEqual(1, e.ParentID);
				Assert.AreEqual(1, e.Value.Value1);
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
				Assert.AreEqual(1, e.ParentID);
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
				Assert.IsNotNull(str);
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

				Assert.AreEqual(1, results);
			}
		}

		[Test]
		public void TestInterfaceMapping2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var results = db.GetTable<IChild>().Where(c => c.ChildID == 32).Select(_ => new { _.ChildID }).ToList();

				Assert.AreEqual(1, results.Count);
				Assert.AreEqual(32, results[0].ChildID);
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
		public void ColumnMappingException1([DataSources] string context)
		{
			GetProviderName(context, out var isLinqService);

			using (var db = GetDataContext(context, testLinqService : false, suppressSequentialAccess: true))
			{
				if (isLinqService)
				{
#if NETFRAMEWORK
					var fe = Assert.Throws<FaultException>(() => db.GetTable<BadMapping>().Select(_ => new { _.NotInt }).ToList())!;
					Assert.True(fe.Message.ToLowerInvariant().Contains("firstname"));
#else
					var fe = Assert.Throws<Grpc.Core.RpcException>(() => db.GetTable<BadMapping>().Select(_ => new { _.NotInt }).ToList())!;
					Assert.True(fe.Message.ToLowerInvariant().Contains("firstname"));
#endif
				}
				else
				{
					var ex = Assert.Throws<LinqToDBConvertException>(() => db.GetTable<BadMapping>().Select(_ => new { _.NotInt }).ToList())!;
					// field name casing depends on database
					Assert.AreEqual("firstname", ex.ColumnName!.ToLowerInvariant());
				}
			}
		}

		[Test]
		public void ColumnMappingException2([DataSources] string context)
		{
			GetProviderName(context, out var isLinqService);

			using (var db = GetDataContext(context, suppressSequentialAccess: true))
			{
				var ex = Assert.Throws<LinqToDBConvertException>(() => db.GetTable<BadMapping>().Select(_ => new { _.BadEnum }).ToList())!;
				Assert.AreEqual("lastname", ex.ColumnName!.ToLowerInvariant());
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

				Assert.AreEqual(2        , data.Length);
				Assert.AreEqual(1        , data[0].Id);
				Assert.AreEqual("One"    , data[0].Value);
				Assert.AreEqual("OneBase", data[0].BaseValue );
				Assert.AreEqual(2        , data[1].Id);
				Assert.AreEqual("Two"    , data[1].Value);
				Assert.AreEqual("TwoBase", data[1].BaseValue);

				var proj = table.OrderBy(r => r.Id).Select(r => new { r.Id, r.Value, r.BaseValue }).ToArray();

				Assert.AreEqual(2        , proj.Length);
				Assert.AreEqual(1        , proj[0].Id);
				Assert.AreEqual("One"    , proj[0].Value);
				Assert.AreEqual("OneBase", proj[0].BaseValue );
				Assert.AreEqual(2        , proj[1].Id);
				Assert.AreEqual("Two"    , proj[1].Value);
				Assert.AreEqual("TwoBase", proj[1].BaseValue);
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

				Assert.AreEqual(2        , data.Length);
				Assert.AreEqual(1        , data[0].Id);
				Assert.AreEqual("One"    , data[0].Value);
				Assert.AreEqual("OneBase", data[0].BaseValue );
				Assert.AreEqual(2        , data[1].Id);
				Assert.AreEqual("Two"    , data[1].Value);
				Assert.AreEqual("TwoBase", data[1].BaseValue);

				var proj = table.OrderBy(r => r.Id).Select(r => new { r.Id, r.Value, r.BaseValue }).ToArray();

				Assert.AreEqual(2        , proj.Length);
				Assert.AreEqual(1        , proj[0].Id);
				Assert.AreEqual("One"    , proj[0].Value);
				Assert.AreEqual("OneBase", proj[0].BaseValue );
				Assert.AreEqual(2        , proj[1].Id);
				Assert.AreEqual("Two"    , proj[1].Value);
				Assert.AreEqual("TwoBase", proj[1].BaseValue);
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

				Assert.AreEqual(2        , data.Length);
				Assert.AreEqual(1        , data[0].Id);
				Assert.AreEqual("One"    , data[0].Value);
				Assert.AreEqual("OneBase", data[0].BaseValue );
				Assert.AreEqual(2        , data[1].Id);
				Assert.AreEqual("Two"    , data[1].Value);
				Assert.AreEqual("TwoBase", data[1].BaseValue);

				var proj = table.OrderBy(r => r.Id).Select(r => new { r.Id, r.Value, r.BaseValue }).ToArray();

				Assert.AreEqual(2        , proj.Length);
				Assert.AreEqual(1        , proj[0].Id);
				Assert.AreEqual("One"    , proj[0].Value);
				Assert.AreEqual("OneBase", proj[0].BaseValue );
				Assert.AreEqual(2        , proj[1].Id);
				Assert.AreEqual("Two"    , proj[1].Value);
				Assert.AreEqual("TwoBase", proj[1].BaseValue);
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
			Assert.AreEqual(1, ed.Columns.Count);
			Assert.AreEqual("PersonID", ed.Columns[0].ColumnName);
		}

		[Test]
		public void ColumnReplacedWithNew2([IncludeDataSources(true, ProviderName.SQLiteClassic)] string context)
		{
			using var db = GetDataContext(context);
			db.GetTable<NewModel2>().Where(c => c.Id == -1).ToList();

			var ed = db.MappingSchema.GetEntityDescriptor(typeof(NewModel2));
			Assert.AreEqual(1, ed.Columns.Count);
			Assert.AreEqual("PersonID", ed.Columns[0].ColumnName);
		}

		[Test]
		public void ColumnReplacedWithNew3([IncludeDataSources(true, ProviderName.SQLiteClassic)] string context)
		{
			using var db = GetDataContext(context);
			db.GetTable<NewModel3>().Where(c => c.Id == -1).ToList();

			var ed = db.MappingSchema.GetEntityDescriptor(typeof(NewModel3));
			Assert.AreEqual(1, ed.Columns.Count);
			Assert.AreEqual("PersonID", ed.Columns[0].ColumnName);
		}

		[Test]
		public void ColumnReplacedWithNew4([IncludeDataSources(true, ProviderName.SQLiteClassic)] string context)
		{
			using var db = GetDataContext(context);
			db.GetTable<NewModel4>().Where(c => c.Id == -1).ToList();

			var ed = db.MappingSchema.GetEntityDescriptor(typeof(NewModel4));
			Assert.AreEqual(1, ed.Columns.Count);
			Assert.AreEqual("PersonID", ed.Columns[0].ColumnName);
		}

		[Test]
		public void ColumnReplacedWithNew5([IncludeDataSources(true, ProviderName.SQLiteClassic)] string context)
		{
			using var db = GetDataContext(context);
			db.GetTable<NewModel5>().Where(c => c.Id == -1).ToList();

			var ed = db.MappingSchema.GetEntityDescriptor(typeof(NewModel5));
			Assert.AreEqual(1, ed.Columns.Count);
			Assert.AreEqual("PersonID", ed.Columns[0].ColumnName);
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3117")]
		public void Issue3117Test1([DataSources(TestProvName.AllAccess)] string context)
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3117")]
		public void Issue3117Test2([DataSources(TestProvName.AllAccess)] string context)
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
	}
}
