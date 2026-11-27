using System;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class EnumMappingTests : TestBase
	{
		enum TestEnum1
		{
			[MapValue(ProviderName.Access, 11), MapValue(11L)] Value1 = 3,
			[MapValue(ProviderName.Access, 12), MapValue(12L)] Value2,
		}

		enum TestEnum2
		{
			Value1 = 3,
			Value2,
		}

		enum TestEnum21
		{
			[MapValue(ProviderName.Access, 11), MapValue(11L)] Value1 = 3,
			[MapValue(ProviderName.Access, 12), MapValue(12L)] Value2,
		}

		enum TestEnum3
		{
			Value1 = 3,
			Value2,
		}

		enum UndefinedEnum
		{
			[MapValue(ProviderName.Access, 11), MapValue(11L)]
			Value1,
			[MapValue(ProviderName.Access, 12), MapValue(12L)]
			Value2,
		}

		[Table("LinqDataTypes")]
		sealed class TestTable1
		{
			[PrimaryKey, Column("ID")] public int       Id;
			[Column("BigIntValue")]    public TestEnum1 TestField;
		}

		[Table("LinqDataTypes")]
		sealed class TestTable2
		{
			[PrimaryKey, Column("ID")] public int        Id;
			[Column("BigIntValue")]    public TestEnum21 TestField;
			[Column("IntValue")]       public TestEnum3  Int32Field;
		}

		[Table("LinqDataTypes")]
		sealed class NullableTestTable1
		{
			[PrimaryKey, Column("ID")] public int?       Id;
			[Column("BigIntValue")]    public TestEnum1? TestField;
		}

		[Table("LinqDataTypes")]
		sealed class NullableTestTable2
		{
			[PrimaryKey, Column("ID")] public int?        Id;
			[Column("BigIntValue")]    public TestEnum21? TestField;
			[Column("IntValue")]       public TestEnum3?  Int32Field;
		}

		[Table("LinqDataTypes")]
		sealed class RawTable
		{
			[PrimaryKey, Column("ID")] public int  Id;
			[Column("BigIntValue")]    public long TestField;
			[Column("IntValue")]       public int  Int32Field;
		}

		[Table("LinqDataTypes")]
		sealed class UndefinedValueTest
		{
			[PrimaryKey, Column("ID")] public int?          Id;
			[Column("BigIntValue")]    public UndefinedEnum TestField;
		}

		sealed class Cleaner : IDisposable
		{
			private readonly int _records;
			readonly ITestDataContext _db;

			public Cleaner(ITestDataContext db, int records = 1)
			{
				_records = records;
				_db = db;
				Clean();
			}

			private void Clean()
			{
				_db.GetTable<RawTable>().Where(r => r.Id >= RID && r.Id < RID + _records).Delete();
			}

			public void Dispose()
			{
				try
				{
					// rollback emulation for WCF
					Clean();
				}
				catch
				{
				}
			}
		}

		const long VAL2 = 12;
		const long VAL1 = 11;
		const int  RID  = 101;

		[Test]
		public void EnumMapInsert1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<TestTable1>().Insert(() => new TestTable1
				{
					Id = RID,
					TestField = TestEnum1.Value2
				});

				Assert.That(db.GetTable<RawTable>().Where(r => r.Id == RID && r.TestField == VAL2).Count(), Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapInsert2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<TestTable2>().Insert(() => new TestTable2
				{
					Id = RID,
					TestField = TestEnum21.Value2
				});

				Assert.That(db.GetTable<RawTable>().Where(r => r.Id == RID && r.TestField == VAL2).Count(), Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapInsert3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<NullableTestTable1>().Insert(() => new NullableTestTable1
				{
					Id = RID,
					TestField = TestEnum1.Value2
				});

				Assert.That(db.GetTable<RawTable>()
					.Where(r => r.Id == RID && r.TestField == VAL2).Count(), Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapInsert4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<NullableTestTable2>().Insert(() => new NullableTestTable2
				{
					Id = RID,
					TestField = TestEnum21.Value2
				});

				Assert.That(db.GetTable<RawTable>().Where(r => r.Id == RID && r.TestField == VAL2).Count(), Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapWhere1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var result = db.GetTable<TestTable1>().Where(r => r.Id == RID && r.TestField == TestEnum1.Value2).Select(r => r.TestField).FirstOrDefault();
				Assert.That(result, Is.EqualTo(TestEnum1.Value2));
			}
		}

		[Test]
		public void EnumMapWhere2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var result = db.GetTable<TestTable2>().Where(r => r.Id == RID && r.TestField == TestEnum21.Value2).Select(r => r.TestField).FirstOrDefault();
				Assert.That(result, Is.EqualTo(TestEnum21.Value2));
			}
		}

		[Test]
		public void EnumMapWhere3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var result = db.GetTable<NullableTestTable1>()
					.Where(r => r.Id == RID && r.TestField == TestEnum1.Value2)
					.Select(r => r.TestField).FirstOrDefault();
				Assert.That(result, Is.EqualTo(TestEnum1.Value2));
			}
		}

		[Test]
		public void EnumMapWhere4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var result = db.GetTable<NullableTestTable2>()
					.Where(r => r.Id == RID && r.TestField == TestEnum21.Value2)
					.Select(r => r.TestField).FirstOrDefault();
				Assert.That(result, Is.EqualTo(TestEnum21.Value2));
			}
		}

		[Test]
		public void EnumMapUpdate1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL1
				});

				db.GetTable<TestTable1>()
					.Where(r => r.Id == RID && r.TestField == TestEnum1.Value1)
					.Update(r => new TestTable1 { TestField = TestEnum1.Value2 });

				var result = db.GetTable<RawTable>()
					.Where(r => r.Id == RID && r.TestField == VAL2)
					.Select(r => r.TestField)
					.FirstOrDefault();

				Assert.That(result, Is.EqualTo(VAL2));
			}
		}

		[Test]
		public void EnumMapUpdate2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL1
				});

				db.GetTable<TestTable2>()
					.Where(r => r.Id == RID && r.TestField == TestEnum21.Value1)
					.Update(r => new TestTable2 { TestField = TestEnum21.Value2 });

				var result = db.GetTable<RawTable>()
					.Where(r => r.Id == RID && r.TestField == VAL2)
					.Select(r => r.TestField)
					.FirstOrDefault();

				Assert.That(result, Is.EqualTo(VAL2));
			}
		}

		[Test]
		public void EnumMapUpdate3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL1
				});

				db.GetTable<NullableTestTable1>()
					.Where(r => r.Id == RID && r.TestField == TestEnum1.Value1)
					.Update(r => new NullableTestTable1 { TestField = TestEnum1.Value2 });

				var result = db.GetTable<RawTable>()
					.Where(r => r.Id == RID && r.TestField == VAL2)
					.Select(r => r.TestField)
					.FirstOrDefault();

				Assert.That(result, Is.EqualTo(VAL2));
			}
		}

		[Test]
		public void EnumMapUpdate4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL1
				});

				db.GetTable<NullableTestTable2>()
					.Where(r => r.Id == RID && r.TestField == TestEnum21.Value1)
					.Update(r => new NullableTestTable2 { TestField = TestEnum21.Value2 });

				var result = db.GetTable<RawTable>()
					.Where(r => r.Id == RID && r.TestField == VAL2)
					.Select(r => r.TestField)
					.FirstOrDefault();

				Assert.That(result, Is.EqualTo(VAL2));
			}
		}

		[Test]
		public void EnumMapSelectAnon1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var result = db.GetTable<TestTable1>()
					.Where(r => r.Id == RID && r.TestField == TestEnum1.Value2)
					.Select(r => new { r.TestField })
					.FirstOrDefault()!;

				Assert.That(result, Is.Not.Null);
				Assert.That(result.TestField, Is.EqualTo(TestEnum1.Value2));
			}
		}

		[Test]
		public void EnumMapSelectAnon2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var result = db.GetTable<TestTable2>()
					.Where(r => r.Id == RID && r.TestField == TestEnum21.Value2)
					.Select(r => new { r.TestField })
					.FirstOrDefault()!;

				Assert.That(result, Is.Not.Null);
				Assert.That(result.TestField, Is.EqualTo(TestEnum21.Value2));
			}
		}

		[Test]
		public void EnumMapSelectAnon3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var result = db.GetTable<NullableTestTable1>()
					.Where(r => r.Id == RID && r.TestField == TestEnum1.Value2)
					.Select(r => new { r.TestField })
					.FirstOrDefault()!;

				Assert.That(result, Is.Not.Null);
				Assert.That(result.TestField, Is.EqualTo(TestEnum1.Value2));
			}
		}

		[Test]
		public void EnumMapSelectAnon4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var result = db.GetTable<NullableTestTable2>()
					.Where(r => r.Id == RID && r.TestField == TestEnum21.Value2)
					.Select(r => new { r.TestField })
					.FirstOrDefault()!;

				Assert.That(result, Is.Not.Null);
				Assert.That(result.TestField, Is.EqualTo(TestEnum21.Value2));
			}
		}

		[Test]
		public void EnumMapDelete1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var cnt = db.GetTable<TestTable1>().Delete(r => r.Id == RID && r.TestField == TestEnum1.Value2);
				if (context.SupportsRowcount())
					Assert.That(cnt, Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapDelete2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var cnt = db.GetTable<TestTable2>().Delete(r => r.Id == RID && r.TestField == TestEnum21.Value2);
				if (context.SupportsRowcount())
					Assert.That(cnt, Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapDelete3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var cnt = db.GetTable<NullableTestTable1>().Delete(r => r.Id == RID && r.TestField == TestEnum1.Value2);
				if (context.SupportsRowcount())
					Assert.That(cnt, Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapDelete4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var cnt = db.GetTable<NullableTestTable2>().Delete(r => r.Id == RID && r.TestField == TestEnum21.Value2);
				if (context.SupportsRowcount())
					Assert.That(cnt, Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapSet1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL1
				});

				db.GetTable<TestTable1>()
					.Where(r => r.Id == RID && r.TestField == TestEnum1.Value1)
					.Set(r => r.TestField, TestEnum1.Value2).Update();
				var result = db.GetTable<RawTable>().Where(r => r.Id == RID && r.TestField == VAL2).Select(r => r.TestField).FirstOrDefault();
				Assert.That(result, Is.EqualTo(VAL2));
			}
		}

		[Test]
		public void EnumMapSet2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL1
				});

				db.GetTable<TestTable2>()
					.Where(r => r.Id == RID && r.TestField == TestEnum21.Value1)
					.Set(r => r.TestField, TestEnum21.Value2).Update();
				var result = db.GetTable<RawTable>().Where(r => r.Id == RID && r.TestField == VAL2).Select(r => r.TestField).FirstOrDefault();
				Assert.That(result, Is.EqualTo(VAL2));
			}
		}

		[Test]
		public void EnumMapSet3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					Int32Field = 3
				});

				db.GetTable<TestTable2>()
					.Where(r => r.Id == RID && r.Int32Field == TestEnum3.Value1)
					.Set(r => r.Int32Field, () => TestEnum3.Value2).Update();

				Assert.That(db.GetTable<RawTable>().Where(r => r.Id == RID && r.Int32Field == 4).Count(), Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapSet4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL1
				});

				db.GetTable<NullableTestTable1>()
					.Where(r => r.Id == RID && r.TestField == TestEnum1.Value1)
					.Set(r => r.TestField, TestEnum1.Value2).Update();
				var result = db.GetTable<RawTable>().Where(r => r.Id == RID && r.TestField == VAL2).Select(r => r.TestField).FirstOrDefault();
				Assert.That(result, Is.EqualTo(VAL2));
			}
		}

		[Test]
		public void EnumMapSet5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL1
				});

				db.GetTable<NullableTestTable2>()
					.Where(r => r.Id == RID && r.TestField == TestEnum21.Value1)
					.Set(r => r.TestField, TestEnum21.Value2).Update();
				var result = db.GetTable<RawTable>().Where(r => r.Id == RID && r.TestField == VAL2).Select(r => r.TestField).FirstOrDefault();
				Assert.That(result, Is.EqualTo(VAL2));
			}
		}

		[Test]
		public void EnumMapSet6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					Int32Field = 3
				});

				db.GetTable<NullableTestTable2>()
					.Where(r => r.Id == RID && r.Int32Field == TestEnum3.Value1)
					.Set(r => r.Int32Field, () => TestEnum3.Value2).Update();
				Assert.That(db.GetTable<RawTable>().Where(r => r.Id == RID && r.Int32Field == 4).Count(), Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapContains1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				Assert.That(db.GetTable<TestTable1>()
					.Where(r => r.Id == RID && new[] { TestEnum1.Value2 }.Contains(r.TestField)).Count(), Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapContains2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				Assert.That(db.GetTable<TestTable2>().Where(r => r.Id == RID && new[] { TestEnum21.Value2 }.Contains(r.TestField)).Count(), Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapContains3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				Assert.That(db.GetTable<NullableTestTable1>()
					.Where(r => r.Id == RID && new[] { (TestEnum1?)TestEnum1.Value2 }.Contains(r.TestField)).Count(), Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapContains4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				Assert.That(db.GetTable<NullableTestTable2>()
					.Where(r => r.Id == RID && new[] { (TestEnum21?)TestEnum21.Value2 }.Contains(r.TestField)).Count(), Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapSelectNull1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID
				});

				var result = db.GetTable<NullableTestTable1>()
					.Where(r => r.Id == RID)
					.Select(r => new { r.TestField })
					.FirstOrDefault()!;

				Assert.That(result, Is.Not.Null);
				Assert.That(result.TestField, Is.Null);
			}
		}

		[Test]
		public void EnumMapSelectNull2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID
				});

				var result = db.GetTable<NullableTestTable2>()
					.Where(r => r.Id == RID)
					.Select(r => new { r.TestField })
					.FirstOrDefault()!;

				Assert.That(result, Is.Not.Null);
				Assert.That(result.TestField, Is.Null);
			}
		}

		[Test]
		public void EnumMapWhereNull1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID
				});

				var result = db.GetTable<NullableTestTable1>()
					.Where(r => r.Id == RID && r.TestField == null)
					.Select(r => new { r.TestField }).FirstOrDefault()!;
				Assert.That(result, Is.Not.Null);
				Assert.That(result.TestField, Is.Null);
			}
		}

		[Test]
		public void EnumMapWhereNull2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID
				});

				var result = db.GetTable<NullableTestTable2>()
					.Where(r => r.Id == RID && r.TestField == null)
					.Select(r => new { r.TestField }).FirstOrDefault()!;
				Assert.That(result, Is.Not.Null);
				Assert.That(result.TestField, Is.Null);
			}
		}

		[Test]
		public void EnumMapInsertObject1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.Insert(new TestTable1
				{
					Id = RID,
					TestField = TestEnum1.Value2
				});

				Assert.That(db.GetTable<RawTable>().Where(r => r.Id == RID && r.TestField == VAL2).Count(), Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapInsertObject2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.Insert(new TestTable2
				{
					Id = RID,
					TestField = TestEnum21.Value2
				});

				Assert.That(db.GetTable<RawTable>().Where(r => r.Id == RID && r.TestField == VAL2).Count(), Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapInsertObject3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.Insert(new NullableTestTable1
				{
					Id = RID,
					TestField = TestEnum1.Value2
				});

				Assert.That(db.GetTable<RawTable>()
					.Where(r => r.Id == RID && r.TestField == VAL2).Count(), Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapInsertObject4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.Insert(new NullableTestTable2
				{
					Id = RID,
					TestField = TestEnum21.Value2
				});

				Assert.That(db.GetTable<RawTable>().Where(r => r.Id == RID && r.TestField == VAL2).Count(), Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapInsertFromSelectWithParam1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db, 2))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var param = TestEnum1.Value1;

				// insert a distinct row (RID + 1) rather than a duplicate of the source PK: YDB enforces
				// the LinqDataTypes primary key, so re-inserting RID would conflict.
				var result = db.GetTable<TestTable1>()
					.Where(r => r.Id == RID && r.TestField == TestEnum1.Value2)
					.Select(r => new TestTable1
					{
						Id = r.Id + 1,
						TestField = param
					})
					.Insert(db.GetTable<TestTable1>(), r => r);

				if (context.SupportsRowcount())
					Assert.That(result, Is.EqualTo(1));
				Assert.That(db.GetTable<RawTable>().Where(r => r.Id == RID + 1 && r.TestField == VAL1).Count(), Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapInsertFromSelectWithParam2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db, 2))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var param = TestEnum21.Value1;

				// insert a distinct row (RID + 1) rather than a duplicate of the source PK: YDB enforces
				// the LinqDataTypes primary key, so re-inserting RID would conflict.
				var result = db.GetTable<TestTable2>()
					.Where(r => r.Id == RID && r.TestField == TestEnum21.Value2)
					.Select(r => new TestTable2
					{
						Id = r.Id + 1,
						TestField = param
					})
					.Insert(db.GetTable<TestTable2>(), r => r);

				if (context.SupportsRowcount())
					Assert.That(result, Is.EqualTo(1));
				Assert.That(db.GetTable<RawTable>().Where(r => r.Id == RID + 1 && r.TestField == VAL1).Count(), Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapInsertFromSelectWithParam3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db, 2))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var param = TestEnum1.Value1;

				// insert a distinct row (RID + 1) rather than a duplicate of the source PK: YDB enforces
				// the LinqDataTypes primary key, so re-inserting RID would conflict.
				var result = db.GetTable<NullableTestTable1>()
					.Where(r => r.Id == RID && r.TestField == TestEnum1.Value2)
					.Select(r => new NullableTestTable1
					{
						Id = r.Id + 1,
						TestField = param
					})
					.Insert(db.GetTable<NullableTestTable1>(), r => r);

				if (context.SupportsRowcount())
					Assert.That(result, Is.EqualTo(1));
				Assert.That(db.GetTable<RawTable>().Where(r => r.Id == RID + 1 && r.TestField == VAL1).Count(), Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapInsertFromSelectWithParam4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db, 2))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var param = TestEnum21.Value1;

				// insert a distinct row (RID + 1) rather than a duplicate of the source PK: YDB enforces
				// the LinqDataTypes primary key, so re-inserting RID would conflict.
				var result = db.GetTable<NullableTestTable2>()
					.Where(r => r.Id == RID && r.TestField == TestEnum21.Value2)
					.Select(r => new NullableTestTable2
					{
						Id = r.Id + 1,
						TestField = param
					})
					.Insert(db.GetTable<NullableTestTable2>(), r => r);

				if (context.SupportsRowcount())
					Assert.That(result, Is.EqualTo(1));
				Assert.That(db.GetTable<RawTable>().Where(r => r.Id == RID + 1 && r.TestField == VAL1).Count(), Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapDeleteEquals1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var cnt = db.GetTable<TestTable1>().Delete(r => r.Id == RID && r.TestField.Equals(TestEnum1.Value2));
				if (context.SupportsRowcount())
					Assert.That(cnt, Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapDeleteEquals2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var cnt = db.GetTable<TestTable2>().Delete(r => r.Id == RID && r.TestField.Equals(TestEnum21.Value2));
				if (context.SupportsRowcount())
					Assert.That(cnt, Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapDeleteEquals3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var cnt = db.GetTable<NullableTestTable1>().Delete(r => r.Id == RID && r.TestField.Equals(TestEnum1.Value2));

				if (context.SupportsRowcount())
					Assert.That(cnt, Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapDeleteEquals4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var cnt = db.GetTable<NullableTestTable2>().Delete(r => r.Id == RID && r.TestField.Equals(TestEnum21.Value2));

				if (context.SupportsRowcount())
					Assert.That(cnt, Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapCustomPredicate1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var entityParameter = Expression.Parameter(typeof(TestTable1), "entity"); // parameter name required for BLToolkit
				var filterExpression = Expression.Equal(Expression.Field(entityParameter, "TestField"), Expression.Constant(TestEnum1.Value2));
				var filterPredicate = Expression.Lambda<Func<TestTable1, bool>>(filterExpression, entityParameter);
				var result = db.GetTable<TestTable1>().Where(filterPredicate).ToList();

				Assert.That(result, Has.Count.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapCustomPredicate2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var entityParameter = Expression.Parameter(typeof(TestTable2), "entity"); // parameter name required for BLToolkit
				var filterExpression = Expression.Equal(Expression.Field(entityParameter, "TestField"), Expression.Constant(TestEnum21.Value2));
				var filterPredicate = Expression.Lambda<Func<TestTable2, bool>>(filterExpression, entityParameter);
				var result = db.GetTable<TestTable2>().Where(filterPredicate).ToList();

				Assert.That(result, Has.Count.EqualTo(1));
			}
		}

		[Table("LinqDataTypes")]
		sealed class TestTable3
		{
			[PrimaryKey]            public int        ID;
			[Column("BigIntValue")] public TestEnum1? TargetType;
			[Column("IntValue")]    public int?       TargetID;
		}

		[StructLayout(LayoutKind.Auto)]
		struct ObjectReference
		{
			public TestEnum1 TargetType;

			public int TargetID;

			public ObjectReference(TestEnum1 targetType, int tagetId)
			{
				TargetType = targetType;
				TargetID = tagetId;
			}
		}

		[Test]
		public void Test_4_1_18_Regression1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2,
					Int32Field = 10
				});

				var result = db.GetTable<TestTable3>().Where(r => r.ID == RID).Select(_ => new
				{
					Target = _.TargetType != null && _.TargetID != null
					  ? new ObjectReference(_.TargetType.Value, _.TargetID.Value)
					  : default(ObjectReference?)
				})
				.ToArray();

				Assert.That(result, Has.Length.EqualTo(1));
				Assert.That(result[0].Target, Is.Not.Null);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result[0].Target!.Value.TargetID, Is.EqualTo(10));
					Assert.That(result[0].Target!.Value.TargetType, Is.EqualTo(TestEnum1.Value2));
				}
			}
		}

		[Table("LinqDataTypes")]
		sealed class TestTable4
		{
			[PrimaryKey]            public int        ID;
			[Column("BigIntValue")] public TestEnum2? TargetType;
			[Column("IntValue")]    public int?       TargetID;
		}

		[StructLayout(LayoutKind.Auto)]
		struct ObjectReference2
		{
			public TestEnum2 TargetType;

			public int TargetID;

			public ObjectReference2(TestEnum2 targetType, int tagetId)
			{
				TargetType = targetType;
				TargetID = tagetId;
			}
		}

		[Test]
		public void Test_4_1_18_Regression2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable()
				{
					Id = RID,
					TestField = (long)TestEnum2.Value2,
					Int32Field = 10
				});

				var result = db.GetTable<TestTable4>().Where(r => r.ID == RID).Select(_ => new
				{
					Target = _.TargetType != null && _.TargetID != null
					  ? new ObjectReference2(_.TargetType.Value, _.TargetID.Value)
					  : default(ObjectReference2?)
				})
				.ToArray();

				Assert.That(result, Has.Length.EqualTo(1));
				Assert.That(result[0].Target, Is.Not.Null);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result[0].Target!.Value.TargetID, Is.EqualTo(10));
					Assert.That(result[0].Target!.Value.TargetType, Is.EqualTo(TestEnum2.Value2));
				}
			}
		}

		sealed class NullableResult
		{
			public TestEnum1? Value;
		}

		TestEnum1 Convert(TestEnum1 val)
		{
			return val;
		}

		[Test]
		public void EnumMapSelectNull_Regression([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var result = db.GetTable<TestTable1>()
					.Where(r => r.Id == RID)
					.Select(r => new NullableResult { Value = Convert(r.TestField) })
					.FirstOrDefault()!;

				Assert.That(result, Is.Not.Null);
				Assert.That(result.Value, Is.EqualTo(TestEnum1.Value2));
			}
		}

		[Flags]
		enum TestFlag
		{
			Value1 = 0x1,
			Value2 = 0x2
		}

		[Table]
		sealed class TestTable5
		{
			[PrimaryKey] public int      ID;
			[Column]     public TestFlag IntValue;
		}

		[Test]
		public void TestFlagEnum([DataSources(TestProvName.AllNativeAccess)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<TestTable5>();
			// MA0192 wants HasFlag here, but this is an expression tree translated to SQL and the bitwise
			// form is exactly what this test covers — HasFlag would exercise a different translation path.
#pragma warning disable MA0192 // Use HasFlag instead of bitwise checks
			var result =
					from t in table
					where (t.IntValue & TestFlag.Value1) != 0
					select t;
#pragma warning restore MA0192

			result.ToArray();

			var sql = result.ToSqlQuery().Sql;

			Assert.That(sql, Is.Not.Contains("Convert").And.Not.Contains("Int(").And.Not.Contains("Cast"));
		}

		[Test]
		public void EnumMapIntermediateObject1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				Assert.That(
					db.GetTable<TestTable1>()
					.Select(r => new { r.Id, r.TestField })
					.Where(r => r.Id == RID && r.TestField == TestEnum1.Value2).Count(), Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapIntermediateObject2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				Assert.That(
					db.GetTable<TestTable2>()
					.Select(r => new { r.Id, r.TestField })
					.Where(r => r.Id == RID && r.TestField == TestEnum21.Value2).Count(), Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapIntermediateObject3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				Assert.That(
					db.GetTable<NullableTestTable1>()
					.Select(r => new { r.Id, r.TestField })
					.Where(r => r.Id == RID && r.TestField == TestEnum1.Value2).Count(), Is.EqualTo(1));
			}
		}

		[Test]
		public void EnumMapIntermediateObject4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				Assert.That(
					db.GetTable<NullableTestTable2>()
					.Select(r => new { r.Id, r.TestField })
					.Where(r => r.Id == RID && r.TestField == TestEnum21.Value2).Count(), Is.EqualTo(1));
			}
		}

		[Table("LinqDataTypes")]
		sealed class NullableTestTable01
		{
			[PrimaryKey, Column("ID")] public int?           Id;
			[Column("IntValue")]       public NullableEnum01 Value;
		}

		[Table("LinqDataTypes")]
		sealed class NullableTestTable02
		{
			[PrimaryKey, Column("ID")] public int?            Id;
			[Column("IntValue")]       public NullableEnum01? Value;
		}

		[Table("LinqDataTypes")]
		sealed class NullableTestTable03
		{
			[PrimaryKey, Column("ID")] public int?           Id;
			[Column("StringValue")]    public NullableEnum02 Value;
		}

		[Table("LinqDataTypes")]
		sealed class NullableTestTable04
		{
			[PrimaryKey, Column("ID")] public int?            Id;
			[Column("StringValue")]    public NullableEnum02? Value;
		}

		[Table("LinqDataTypes")]
		sealed class NullableTestTable05
		{
			[PrimaryKey, Column("ID")] public int?           Id;
			[Column("IntValue")]       public NullableEnum03 Value;
		}

		[Table("LinqDataTypes")]
		sealed class NullableTestTable06
		{
			[PrimaryKey, Column("ID")] public int?            Id;
			[Column("IntValue")]       public NullableEnum03? Value;
		}

		enum NullableEnum01
		{
			[MapValue(11)]   Value1 = 3,
			[MapValue(22)]   Value2,
			[MapValue(null)] Value3
		}

		enum NullableEnum02
		{
			[MapValue("11")] Value1 = 3,
			[MapValue("22")] Value2,
			[MapValue(null)] Value3
		}

		enum NullableEnum03
		{
			[MapValue(11)]   Value1 = 3,
			[MapValue(0)]    Value2,
			[MapValue(null)] Value3
		}

		[Table("LinqDataTypes")]
		sealed class RawTable2
		{
			[PrimaryKey, Column("ID")] public int     Id;
			[Column("IntValue")]       public int?    Int32;
			[Column("StringValue")]    public string? String;
		}

		[Test]
		public void NullableEnumWithNullValue01([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db, 3))
			{
				db.Insert(new NullableTestTable01()
				{
					Id = RID,
					Value = NullableEnum01.Value1
				});

				db.Insert(new NullableTestTable01()
				{
					Id = RID + 1,
					Value = NullableEnum01.Value2
				});

				db.Insert(new NullableTestTable01()
				{
					Id = RID + 2,
					Value = NullableEnum01.Value3
				});

				var records = db.GetTable<NullableTestTable01>().Where(r => r.Id >= RID && r.Id <= RID + 2).OrderBy(r => r.Id).ToArray();
				var rawRecords = db.GetTable<RawTable2>().Where(r => r.Id >= RID && r.Id <= RID + 2).OrderBy(r => r.Id).ToArray();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(records, Has.Length.EqualTo(3));
					Assert.That(rawRecords, Has.Length.EqualTo(3));
					Assert.That(records[0].Id, Is.EqualTo(RID));
					Assert.That(rawRecords[0].Id, Is.EqualTo(RID));
					Assert.That(records[0].Value, Is.EqualTo(NullableEnum01.Value1));
					Assert.That(rawRecords[0].Int32, Is.EqualTo(11));

					Assert.That(records[1].Id, Is.EqualTo(RID + 1));
					Assert.That(rawRecords[1].Id, Is.EqualTo(RID + 1));
					Assert.That(records[1].Value, Is.EqualTo(NullableEnum01.Value2));
					Assert.That(rawRecords[1].Int32, Is.EqualTo(22));

					Assert.That(records[2].Id, Is.EqualTo(RID + 2));
					Assert.That(rawRecords[2].Id, Is.EqualTo(RID + 2));
					// for non-nullable enum on read null value mapped
					Assert.That(records[2].Value, Is.EqualTo(NullableEnum01.Value3));
					Assert.That(rawRecords[2].Int32, Is.Null);
				}
			}
		}

		[Test]
		public void NullableEnumWithNullValue02([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db, 4))
			{
				db.Insert(new NullableTestTable02()
				{
					Id = RID,
					Value = NullableEnum01.Value1
				});

				db.Insert(new NullableTestTable02()
				{
					Id = RID + 1,
					Value = NullableEnum01.Value2
				});

				db.Insert(new NullableTestTable02()
				{
					Id = RID + 2,
					Value = NullableEnum01.Value3
				});

				db.Insert(new NullableTestTable02()
				{
					Id = RID + 3
				});

				var records = db.GetTable<NullableTestTable02>().Where(r => r.Id >= RID && r.Id <= RID + 3).OrderBy(r => r.Id).ToArray();
				var rawRecords = db.GetTable<RawTable2>().Where(r => r.Id >= RID && r.Id <= RID + 3).OrderBy(r => r.Id).ToArray();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(records, Has.Length.EqualTo(4));
					Assert.That(rawRecords, Has.Length.EqualTo(4));
					Assert.That(records[0].Id, Is.EqualTo(RID));
					Assert.That(rawRecords[0].Id, Is.EqualTo(RID));
					Assert.That(records[0].Value, Is.EqualTo(NullableEnum01.Value1));
					Assert.That(rawRecords[0].Int32, Is.EqualTo(11));

					Assert.That(records[1].Id, Is.EqualTo(RID + 1));
					Assert.That(rawRecords[1].Id, Is.EqualTo(RID + 1));
					Assert.That(records[1].Value, Is.EqualTo(NullableEnum01.Value2));
					Assert.That(rawRecords[1].Int32, Is.EqualTo(22));

					Assert.That(records[2].Id, Is.EqualTo(RID + 2));
					Assert.That(rawRecords[2].Id, Is.EqualTo(RID + 2));
					// for nullable enum on read null is preferred before mapped value
					Assert.That(records[2].Value, Is.Null);
					Assert.That(rawRecords[2].Int32, Is.Null);

					Assert.That(records[3].Id, Is.EqualTo(RID + 3));
					Assert.That(rawRecords[3].Id, Is.EqualTo(RID + 3));
					Assert.That(records[3].Value, Is.Null);
					Assert.That(rawRecords[3].Int32, Is.Null);
				}
			}
		}

		[Test]
		public void NullableEnumWithNullValue03([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db, 3))
			{
				db.Insert(new NullableTestTable03()
				{
					Id = RID,
					Value = NullableEnum02.Value1
				});

				db.Insert(new NullableTestTable03()
				{
					Id = RID + 1,
					Value = NullableEnum02.Value2
				});

				db.Insert(new NullableTestTable03()
				{
					Id = RID + 2,
					Value = NullableEnum02.Value3
				});

				var records = db.GetTable<NullableTestTable03>().Where(r => r.Id >= RID && r.Id <= RID + 2).OrderBy(r => r.Id).ToArray();
				var rawRecords = db.GetTable<RawTable2>().Where(r => r.Id >= RID && r.Id <= RID + 2).OrderBy(r => r.Id).ToArray();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(records, Has.Length.EqualTo(3));
					Assert.That(rawRecords, Has.Length.EqualTo(3));
					Assert.That(records[0].Id, Is.EqualTo(RID));
					Assert.That(rawRecords[0].Id, Is.EqualTo(RID));
					Assert.That(records[0].Value, Is.EqualTo(NullableEnum02.Value1));
					Assert.That(rawRecords[0].String, Is.EqualTo("11"));

					Assert.That(records[1].Id, Is.EqualTo(RID + 1));
					Assert.That(rawRecords[1].Id, Is.EqualTo(RID + 1));
					Assert.That(records[1].Value, Is.EqualTo(NullableEnum02.Value2));
					Assert.That(rawRecords[1].String, Is.EqualTo("22"));

					Assert.That(records[2].Id, Is.EqualTo(RID + 2));
					Assert.That(rawRecords[2].Id, Is.EqualTo(RID + 2));
					// for non-nullable enum on read null value mapped
					Assert.That(records[2].Value, Is.EqualTo(NullableEnum02.Value3));
					Assert.That(rawRecords[2].String, Is.Null);
				}
			}
		}

		[Test]
		public void NullableEnumWithNullValue04([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db, 4))
			{
				db.Insert(new NullableTestTable04()
				{
					Id = RID,
					Value = NullableEnum02.Value1
				});

				db.Insert(new NullableTestTable04()
				{
					Id = RID + 1,
					Value = NullableEnum02.Value2
				});

				db.Insert(new NullableTestTable04()
				{
					Id = RID + 2,
					Value = NullableEnum02.Value3
				});

				db.Insert(new NullableTestTable04()
				{
					Id = RID + 3
				});

				var records = db.GetTable<NullableTestTable04>().Where(r => r.Id >= RID && r.Id <= RID + 3).OrderBy(r => r.Id).ToArray();
				var rawRecords = db.GetTable<RawTable2>().Where(r => r.Id >= RID && r.Id <= RID + 3).OrderBy(r => r.Id).ToArray();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(records, Has.Length.EqualTo(4));
					Assert.That(rawRecords, Has.Length.EqualTo(4));
					Assert.That(records[0].Id, Is.EqualTo(RID));
					Assert.That(rawRecords[0].Id, Is.EqualTo(RID));
					Assert.That(records[0].Value, Is.EqualTo(NullableEnum02.Value1));
					Assert.That(rawRecords[0].String, Is.EqualTo("11"));

					Assert.That(records[1].Id, Is.EqualTo(RID + 1));
					Assert.That(rawRecords[1].Id, Is.EqualTo(RID + 1));
					Assert.That(records[1].Value, Is.EqualTo(NullableEnum02.Value2));
					Assert.That(rawRecords[1].String, Is.EqualTo("22"));

					Assert.That(records[2].Id, Is.EqualTo(RID + 2));
					Assert.That(rawRecords[2].Id, Is.EqualTo(RID + 2));
					// for nullable enum on read null is preferred before mapped value
					Assert.That(records[2].Value, Is.Null);
					Assert.That(rawRecords[2].String, Is.Null);

					Assert.That(records[3].Id, Is.EqualTo(RID + 3));
					Assert.That(rawRecords[3].Id, Is.EqualTo(RID + 3));
					Assert.That(records[3].Value, Is.Null);
					Assert.That(rawRecords[3].Int32, Is.Null);
				}
			}
		}

		[Test]
		public void NullableEnumWithNullValue05([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db, 3))
			{
				db.Insert(new NullableTestTable05()
				{
					Id = RID,
					Value = NullableEnum03.Value1
				});

				db.Insert(new NullableTestTable05()
				{
					Id = RID + 1,
					Value = NullableEnum03.Value2
				});

				db.Insert(new NullableTestTable05()
				{
					Id = RID + 2,
					Value = NullableEnum03.Value3
				});

				var records = db.GetTable<NullableTestTable05>().Where(r => r.Id >= RID && r.Id <= RID + 2).OrderBy(r => r.Id).ToArray();
				var rawRecords = db.GetTable<RawTable2>().Where(r => r.Id >= RID && r.Id <= RID + 2).OrderBy(r => r.Id).ToArray();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(records, Has.Length.EqualTo(3));
					Assert.That(rawRecords, Has.Length.EqualTo(3));
					Assert.That(records[0].Id, Is.EqualTo(RID));
					Assert.That(rawRecords[0].Id, Is.EqualTo(RID));
					Assert.That(records[0].Value, Is.EqualTo(NullableEnum03.Value1));
					Assert.That(rawRecords[0].Int32, Is.EqualTo(11));

					Assert.That(records[1].Id, Is.EqualTo(RID + 1));
					Assert.That(rawRecords[1].Id, Is.EqualTo(RID + 1));
					Assert.That(records[1].Value, Is.EqualTo(NullableEnum03.Value2));
					Assert.That(rawRecords[1].Int32, Is.Zero);

					Assert.That(records[2].Id, Is.EqualTo(RID + 2));
					Assert.That(rawRecords[2].Id, Is.EqualTo(RID + 2));
					// for non-nullable enum on read null value mapped
					Assert.That(records[2].Value, Is.EqualTo(NullableEnum03.Value3));
					Assert.That(rawRecords[2].Int32, Is.Null);
				}
			}
		}

		[Test]
		public void NullableEnumWithNullValue06([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db, 4))
			{
				db.Insert(new NullableTestTable06()
				{
					Id = RID,
					Value = NullableEnum03.Value1
				});

				db.Insert(new NullableTestTable06()
				{
					Id = RID + 1,
					Value = NullableEnum03.Value2
				});

				db.Insert(new NullableTestTable06()
				{
					Id = RID + 2,
					Value = NullableEnum03.Value3
				});

				db.Insert(new NullableTestTable06()
				{
					Id = RID + 3
				});

				var records = db.GetTable<NullableTestTable06>().Where(r => r.Id >= RID && r.Id <= RID + 3).OrderBy(r => r.Id).ToArray();
				var rawRecords = db.GetTable<RawTable2>().Where(r => r.Id >= RID && r.Id <= RID + 3).OrderBy(r => r.Id).ToArray();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(records, Has.Length.EqualTo(4));
					Assert.That(rawRecords, Has.Length.EqualTo(4));
					Assert.That(records[0].Id, Is.EqualTo(RID));
					Assert.That(rawRecords[0].Id, Is.EqualTo(RID));
					Assert.That(records[0].Value, Is.EqualTo(NullableEnum03.Value1));
					Assert.That(rawRecords[0].Int32, Is.EqualTo(11));

					Assert.That(records[1].Id, Is.EqualTo(RID + 1));
					Assert.That(rawRecords[1].Id, Is.EqualTo(RID + 1));
					Assert.That(records[1].Value, Is.EqualTo(NullableEnum03.Value2));
					Assert.That(rawRecords[1].Int32, Is.Zero);

					Assert.That(records[2].Id, Is.EqualTo(RID + 2));
					Assert.That(rawRecords[2].Id, Is.EqualTo(RID + 2));
					// for nullable enum on read null is preferred before mapped value
					Assert.That(records[2].Value, Is.Null);
					Assert.That(rawRecords[2].Int32, Is.Null);

					Assert.That(records[3].Id, Is.EqualTo(RID + 3));
					Assert.That(rawRecords[3].Id, Is.EqualTo(RID + 3));
					Assert.That(records[3].Value, Is.Null);
					Assert.That(rawRecords[3].Int32, Is.Null);
				}
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/363")]
		public void EnumMappingWriteUndefinedValue([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using (new Cleaner(db))
			{
				db.GetTable<UndefinedValueTest>().Insert(() => new UndefinedValueTest
				{
					Id = RID,
					TestField = (UndefinedEnum)5
				});

				var result = db.GetTable<RawTable>()
						.Select(r => new { r.Id, r.TestField })
						.Where(r => r.Id == RID)
						.ToList();

				Assert.That(result, Has.Count.EqualTo(1));
				Assert.That(result[0].TestField, Is.EqualTo(5));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/363")]
		public void EnumMappingReadUndefinedValue([DataSources] string context)
		{
			GetProviderName(context, out var isLinqService);

			using var db = GetDataContext(context, suppressSequentialAccess: true);
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = 5
				});

				Assert.That(() =>
					db.GetTable<UndefinedValueTest>()
						.Select(r => new { r.Id, r.TestField })
						.Where(r => r.Id == RID)
						.ToList(),
					Throws.TypeOf<LinqToDBConvertException>());
			}
		}

		[Table]
		public class EnumConverterTable
		{
			[PrimaryKey]
			public int Id { get; set; }
			[Column]
			public string? SomeText { get; set; }
		}

		public enum EnumConverterValue
		{
			Value1, Value2
		}

		[Sql.Expression("{0} = {1}", InlineParameters = true, ServerSideOnly = true, IsPredicate = true)]
		private static bool SomeComparison(string? column, EnumConverterValue value) => throw new ServerSideOnlyException(nameof(SomeComparison));

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1622")]
		public void ValueToSqlConverterForEnumInSqlExpression([DataSources] string context)
		{
			var ms = new MappingSchema();
				ms.SetValueToSqlConverter(typeof(EnumConverterValue),
					(sb, dt, v) =>
					{
						sb.Append('\'').Append(((EnumConverterValue)v).ToString()).Append("_suffix'");
					});

			using var db = GetDataContext(context, ms);
			using var table = db.CreateLocalTable<EnumConverterTable>();
			var item = new EnumConverterTable() { Id = 1, SomeText = "Value1_suffix" };
			db.Insert(item);

			var res = table.Where(e => SomeComparison(e.SomeText, EnumConverterValue.Value1)).Single();
			var res2 = table.Where(e => e.Id == 1).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(item.Id, Is.EqualTo(res.Id));
				Assert.That(item.SomeText, Is.EqualTo(res.SomeText));
				Assert.That(item.Id, Is.EqualTo(res2.Id));
				Assert.That(item.SomeText, Is.EqualTo(res2.SomeText));
			}
		}

		public enum CharEnum
		{
			[MapValue('A')]
			A = 6,
			[MapValue('B')]
			B = 5,
			[MapValue('C')]
			C = 4
		}

		public enum CharEnumS : ushort
		{
			[MapValue('A')]
			A = 6,
			[MapValue('B')]
			B = 5,
			[MapValue('C')]
			C = 4
		}

		public enum CharEnumL : ulong
		{
			[MapValue('A')]
			A = 0xFFFFFFFFFFFFFFFF,
			[MapValue('B')]
			B = 0xFFFFFFFFFFFFFFFE,
			[MapValue('C')]
			C = 0xFFFFFFFFFFFFFFFD
		}

		[Table]
		public class EnumCardinality
		{
			[Column]
			public int Id { get; set; }

			[Column] public CharEnum   Property1 { get; set; }
			[Column] public CharEnum?  Property2 { get; set; }
			[Column] public CharEnumS  Property3 { get; set; }
			[Column] public CharEnumS? Property4 { get; set; }
			[Column] public CharEnumL  Property5 { get; set; }
			[Column] public CharEnumL? Property6 { get; set; }

			public static EnumCardinality[] Seed { get; }
				= new[]
				{
					new EnumCardinality() { Id = 1, Property1 = CharEnum.A, Property2 = CharEnum.A, Property3 = CharEnumS.A, Property4 = CharEnumS.A, Property5 = CharEnumL.A, Property6 = CharEnumL.A },
					new EnumCardinality() { Id = 2, Property1 = CharEnum.B, Property2 = CharEnum.B, Property3 = CharEnumS.B, Property4 = CharEnumS.B, Property5 = CharEnumL.B, Property6 = CharEnumL.B },
					new EnumCardinality() { Id = 3, Property1 = CharEnum.C, Property2 = CharEnum.C, Property3 = CharEnumS.C, Property4 = CharEnumS.C, Property5 = CharEnumL.C, Property6 = CharEnumL.C },
				};
		}

		[Test]
		public void TestCardinalityOperators_Less([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(EnumCardinality.Seed);
			var res = table.Where(_ => _.Property1 < CharEnum.B).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.Id, Is.EqualTo(1));
				Assert.That(res.Property1, Is.EqualTo(CharEnum.A));
			}
		}

		[Test]
		public void TestCardinalityOperators_LessOrEqual([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(EnumCardinality.Seed);
			var res = table.Where(_ => _.Property1 <= CharEnum.A).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.Id, Is.EqualTo(1));
				Assert.That(res.Property1, Is.EqualTo(CharEnum.A));
			}
		}

		[Test]
		public void TestCardinalityOperators_Greater([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(EnumCardinality.Seed);
			var res = table.Where(_ => _.Property1 > CharEnum.B).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.Id, Is.EqualTo(3));
				Assert.That(res.Property1, Is.EqualTo(CharEnum.C));
			}
		}

		[Test]
		public void TestCardinalityOperators_GreaterOrEqual([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(EnumCardinality.Seed);
			var res = table.Where(_ => _.Property1 >= CharEnum.C).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.Id, Is.EqualTo(3));
				Assert.That(res.Property1, Is.EqualTo(CharEnum.C));
			}
		}

		[Test]
		public void TestCardinalityOperators_Less_Nullable([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(EnumCardinality.Seed);
			var res = table.Where(_ => _.Property2 < CharEnum.B).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.Id, Is.EqualTo(1));
				Assert.That(res.Property2, Is.EqualTo(CharEnum.A));
			}
		}

		[Test]
		public void TestCardinalityOperators_LessOrEqual_Nullable([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(EnumCardinality.Seed);
			var res = table.Where(_ => _.Property2 <= CharEnum.A).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.Id, Is.EqualTo(1));
				Assert.That(res.Property2, Is.EqualTo(CharEnum.A));
			}
		}

		[Test]
		public void TestCardinalityOperators_Greater_Nullable([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(EnumCardinality.Seed);
			var res = table.Where(_ => _.Property2 > CharEnum.B).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.Id, Is.EqualTo(3));
				Assert.That(res.Property2, Is.EqualTo(CharEnum.C));
			}
		}

		[Test]
		public void TestCardinalityOperators_GreaterOrEqual_Nullable([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(EnumCardinality.Seed);
			var res = table.Where(_ => _.Property2 >= CharEnum.C).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.Id, Is.EqualTo(3));
				Assert.That(res.Property2, Is.EqualTo(CharEnum.C));
			}
		}

		[Test]
		public void TestCardinalityOperators_Less_Short([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(EnumCardinality.Seed);
			var res = table.Where(_ => _.Property3 < CharEnumS.B).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.Id, Is.EqualTo(1));
				Assert.That(res.Property3, Is.EqualTo(CharEnumS.A));
			}
		}

		[Test]
		public void TestCardinalityOperators_LessOrEqual_Short([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(EnumCardinality.Seed);
			var res = table.Where(_ => _.Property3 <= CharEnumS.A).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.Id, Is.EqualTo(1));
				Assert.That(res.Property3, Is.EqualTo(CharEnumS.A));
			}
		}

		[Test]
		public void TestCardinalityOperators_Greater_Short([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(EnumCardinality.Seed);
			var res = table.Where(_ => _.Property3 > CharEnumS.B).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.Id, Is.EqualTo(3));
				Assert.That(res.Property3, Is.EqualTo(CharEnumS.C));
			}
		}

		[Test]
		public void TestCardinalityOperators_GreaterOrEqual_Short([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(EnumCardinality.Seed);
			var res = table.Where(_ => _.Property3 >= CharEnumS.C).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.Id, Is.EqualTo(3));
				Assert.That(res.Property3, Is.EqualTo(CharEnumS.C));
			}
		}

		[Test]
		public void TestCardinalityOperators_Less_Short_Nullable([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(EnumCardinality.Seed);
			var res = table.Where(_ => _.Property4 < CharEnumS.B).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.Id, Is.EqualTo(1));
				Assert.That(res.Property4, Is.EqualTo(CharEnumS.A));
			}
		}

		[Test]
		public void TestCardinalityOperators_LessOrEqual_Short_Nullable([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(EnumCardinality.Seed);
			var res = table.Where(_ => _.Property4 <= CharEnumS.A).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.Id, Is.EqualTo(1));
				Assert.That(res.Property4, Is.EqualTo(CharEnumS.A));
			}
		}

		[Test]
		public void TestCardinalityOperators_Greater_Short_Nullable([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(EnumCardinality.Seed);
			var res = table.Where(_ => _.Property4 > CharEnumS.B).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.Id, Is.EqualTo(3));
				Assert.That(res.Property4, Is.EqualTo(CharEnumS.C));
			}
		}

		[Test]
		public void TestCardinalityOperators_GreaterOrEqual_Short_Nullable([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(EnumCardinality.Seed);
			var res = table.Where(_ => _.Property4 >= CharEnumS.C).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.Id, Is.EqualTo(3));
				Assert.That(res.Property4, Is.EqualTo(CharEnumS.C));
			}
		}

		[Test]
		public void TestCardinalityOperators_Less_Long([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(EnumCardinality.Seed);
			var res = table.Where(_ => _.Property5 < CharEnumL.B).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.Id, Is.EqualTo(1));
				Assert.That(res.Property5, Is.EqualTo(CharEnumL.A));
			}
		}

		[Test]
		public void TestCardinalityOperators_LessOrEqual_Long([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(EnumCardinality.Seed);
			var res = table.Where(_ => _.Property5 <= CharEnumL.A).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.Id, Is.EqualTo(1));
				Assert.That(res.Property5, Is.EqualTo(CharEnumL.A));
			}
		}

		[Test]
		public void TestCardinalityOperators_Greater_Long([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(EnumCardinality.Seed);
			var res = table.Where(_ => _.Property5 > CharEnumL.B).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.Id, Is.EqualTo(3));
				Assert.That(res.Property5, Is.EqualTo(CharEnumL.C));
			}
		}

		[Test]
		public void TestCardinalityOperators_GreaterOrEqual_Long([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(EnumCardinality.Seed);
			var res = table.Where(_ => _.Property5 >= CharEnumL.C).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.Id, Is.EqualTo(3));
				Assert.That(res.Property5, Is.EqualTo(CharEnumL.C));
			}
		}

		[Test]
		public void TestCardinalityOperators_Less_Long_Nullable([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(EnumCardinality.Seed);
			var res = table.Where(_ => _.Property6 < CharEnumL.B).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.Id, Is.EqualTo(1));
				Assert.That(res.Property6, Is.EqualTo(CharEnumL.A));
			}
		}

		[Test]
		public void TestCardinalityOperators_LessOrEqual_Long_Nullable([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(EnumCardinality.Seed);
			var res = table.Where(_ => _.Property6 <= CharEnumL.A).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.Id, Is.EqualTo(1));
				Assert.That(res.Property6, Is.EqualTo(CharEnumL.A));
			}
		}

		[Test]
		public void TestCardinalityOperators_Greater_Long_Nullable([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(EnumCardinality.Seed);
			var res = table.Where(_ => _.Property6 > CharEnumL.B).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.Id, Is.EqualTo(3));
				Assert.That(res.Property6, Is.EqualTo(CharEnumL.C));
			}
		}

		[Test]
		public void TestCardinalityOperators_GreaterOrEqual_Long_Nullable([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(EnumCardinality.Seed);
			var res = table.Where(_ => _.Property6 >= CharEnumL.C).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.Id, Is.EqualTo(3));
				Assert.That(res.Property6, Is.EqualTo(CharEnumL.C));
			}
		}

		enum EnumChar
		{
			[MapValue("A")] Value1 = 1,
			[MapValue("B")] Value2 = 2,
		}

		[Table]
		class EnumCharTable
		{
			[Column(DbType="char(1)", DataType=DataType.Char, Length=1), Nullable] 
			public EnumChar? EnumValue { get; set; }

			[Column]
			public int? IntValue { get; set; }

			[Column]
			public double? DoubleValue { get; set; }
		}

		[Test]
		public void TestEqualityFromObject([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db  = GetDataContext(context);
			
			EnumCharTable[] items =
			[
				new () { EnumValue = EnumChar.Value1, IntValue = 1, DoubleValue    = 1.0 },
				new () { EnumValue = null, IntValue            = null, DoubleValue = null },
			];

			using var tmp = db.CreateLocalTable(items);

			object? charValue      = "A";
			object? intValue       = 1;
			object? stringIntValue = "1";
			object? doubleValue    = 1.0;

			CheckCount();

			charValue      = null;
			intValue       = null;
			stringIntValue = null;
			doubleValue    = null;

			CheckCount();

			void CheckCount()
			{
				var count = tmp.Count(t =>
					t.EnumValue.Equals(charValue)
					&& t.EnumValue.Equals(intValue)
					&& t.IntValue.Equals(stringIntValue)
					&& t.DoubleValue.Equals(doubleValue)
				);

				Assert.That(count, Is.EqualTo(1));

				count = db.GetTable<EnumCharTable>().Count(t =>
					charValue!.Equals(t.EnumValue)
					&& intValue!.Equals(t.EnumValue)
					&& stringIntValue!.Equals(t.IntValue)
					&& doubleValue!.Equals(t.DoubleValue)
				);

				Assert.That(count, Is.EqualTo(1));
			}
		}

		sealed class EnumMethodArgumentModel
		{
			public enum TestStatus
			{
				Outstanding = 0,
				Completed   = 1,
			}

			[Table]
			public sealed class Test
			{
				[Column] public TestStatus Status { get; set; }

				public static readonly Test[] Data =
				[
					new () { Status = TestStatus.Outstanding }
				];
			}

			public static class Ext
			{
				public static string GetStrValue(Enum e)
				{
					return e.ToString();
				}
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5012")]
		public void EnumConditionalArgumentToMethod([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db  = GetDataContext(context);
			using var tb  = db.CreateLocalTable(EnumMethodArgumentModel.Test.Data);

			_ = tb
				.Select(i => new
				{
					Status = EnumMethodArgumentModel.Ext.GetStrValue(i.Status == EnumMethodArgumentModel.TestStatus.Completed
					? EnumMethodArgumentModel.TestStatus.Completed
					: EnumMethodArgumentModel.TestStatus.Outstanding)
				})
				.ToList();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5012")]
		public void EnumConditionalCastArgumentToMethod([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db  = GetDataContext(context);
			using var tb  = db.CreateLocalTable(EnumMethodArgumentModel.Test.Data);

			_ = tb
				.Select(i => new
				{
					Status = EnumMethodArgumentModel.Ext.GetStrValue((EnumMethodArgumentModel.TestStatus)(i.Status == EnumMethodArgumentModel.TestStatus.Completed
					? EnumMethodArgumentModel.TestStatus.Completed
					: EnumMethodArgumentModel.TestStatus.Outstanding))
				})
				.ToList();
		}

		enum ItemTypeEnum
		{
			[MapValue("type1")]
			Type1,
			[MapValue("type2")]
			Type2,
			[MapValue("type3")]
			Type3,
		}

		sealed class Item
		{
			[PrimaryKey]
			public ItemTypeEnum Type { get; set; }
			public int? OwnerId { get; set; }

			[Association(ThisKey = nameof(OwnerId), OtherKey = nameof(Owner.Id), CanBeNull = true)]
			public Owner Owner => throw new InvalidOperationException();
		}

		sealed class Owner
		{
			[PrimaryKey]
			public int Id { get; set; }
			public string? Name { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5416")]
		public void HandleEnumComparisonWithNull([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<Item>();
			using var t2 = db.CreateLocalTable<Owner>();

			_ = t1
				.Select(x => new
				{
					x.Type,
					Owner = x.OwnerId.HasValue ? new
					{
						x.Owner.Id,
						x.Owner.Name,
					} : null,
				}).ToArray();
		}

		[Table("MappedEnumKeyRecord")]
		public class MappedEnumKeyRecord
		{
			[PrimaryKey]
			public int       Id              { get; set; }
			[Column]
			public MapValueKeyType  ClaimedKeyType  { get; set; }
			[Column]
			public MapValueKeyType? ClaimedKeyTypeN { get; set; }

			public static MappedEnumKeyRecord TestInstance = new MappedEnumKeyRecord()
			{
				Id              = 0,
				ClaimedKeyType  = MapValueKeyType.RSA,
				ClaimedKeyTypeN = MapValueKeyType.RSA
			};
		}

		public class FluentMappedEnumKeyRecord
		{
			public int       Id              { get; set; }
			public MapValueKeyType  ClaimedKeyType  { get; set; }
			public MapValueKeyType? ClaimedKeyTypeN { get; set; }

			public static FluentMappedEnumKeyRecord TestInstance = new FluentMappedEnumKeyRecord()
			{
				Id              = 0,
				ClaimedKeyType  = MapValueKeyType.RSA,
				ClaimedKeyTypeN = MapValueKeyType.RSA
			};
		}

		public enum MapValueKeyType : byte
		{
			[MapValue("RSA")]
			RSA = 1,
			[MapValue("EC")]
			EC  = 2
		}

		public enum MapValueAlternateKeyType
		{
			[MapValue("NOT_RSA")]
			RSA = 1,
			[MapValue("NOT_EC")]
			EC  = 2
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1554")]
		public void UpdateSetMapValueEnumFromVariable([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<MappedEnumKeyRecord>();
			db.Insert(MappedEnumKeyRecord.TestInstance);

			var claimedKeyType = MapValueKeyType.EC;

			table.Where(p => p.Id == 0).AsUpdatable()
				.Set(p => p.ClaimedKeyType, claimedKeyType)
				.Set(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Update();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(MapValueKeyType.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(MapValueKeyType.EC));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1554")]
		public void UpdateSetMapValueEnumFromBoxedVariable([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<MappedEnumKeyRecord>();
			db.Insert(MappedEnumKeyRecord.TestInstance);

			object claimedKeyType = MapValueKeyType.EC;

			table.Where(p => p.Id == 0).AsUpdatable()
				.Set(p => p.ClaimedKeyType, claimedKeyType)
				.Set(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Update();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(MapValueKeyType.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(MapValueKeyType.EC));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1554")]
		public void UpdateSetMapValueEnumFromCastVariable([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<MappedEnumKeyRecord>();
			db.Insert(MappedEnumKeyRecord.TestInstance);

			var claimedKeyType = (MapValueKeyType)MapValueAlternateKeyType.EC;

			table.Where(p => p.Id == 0).AsUpdatable()
				.Set(p => p.ClaimedKeyType, claimedKeyType)
				.Set(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Update();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(MapValueKeyType.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(MapValueKeyType.EC));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1554")]
		public void UpdateSetMapValueEnumFromBoxedCastVariable([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<MappedEnumKeyRecord>();
			db.Insert(MappedEnumKeyRecord.TestInstance);

			object claimedKeyType = (MapValueKeyType)MapValueAlternateKeyType.EC;

			table.Where(p => p.Id == 0).AsUpdatable()
				.Set(p => p.ClaimedKeyType, claimedKeyType)
				.Set(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Update();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(MapValueKeyType.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(MapValueKeyType.EC));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1554")]
		public void UpdateSetMapValueEnumFromInlineCast([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<MappedEnumKeyRecord>();
			db.Insert(MappedEnumKeyRecord.TestInstance);

			table.Where(p => p.Id == 0).AsUpdatable()
				.Set(p => p.ClaimedKeyType, (MapValueKeyType)MapValueAlternateKeyType.EC)
				.Set(p => p.ClaimedKeyTypeN, (MapValueKeyType)MapValueAlternateKeyType.EC)
				.Update();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(MapValueKeyType.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(MapValueKeyType.EC));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1554")]
		public void InsertValueMapValueEnumFromVariable([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<MappedEnumKeyRecord>();
			var claimedKeyType = MapValueKeyType.EC;

			table.Value(p => p.Id, 0)
				.Value(p => p.ClaimedKeyType, claimedKeyType)
				.Value(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Insert();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(MapValueKeyType.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(MapValueKeyType.EC));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1554")]
		public void InsertValueMapValueEnumFromBoxedVariable([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<MappedEnumKeyRecord>();
			object claimedKeyType = MapValueKeyType.EC;

			table.Value(p => p.Id, 0)
				.Value(p => p.ClaimedKeyType, claimedKeyType)
				.Value(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Insert();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(MapValueKeyType.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(MapValueKeyType.EC));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1554")]
		public void InsertValueMapValueEnumFromCastVariable([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<MappedEnumKeyRecord>();
			var claimedKeyType = (MapValueKeyType)MapValueAlternateKeyType.EC;

			table.Value(p => p.Id, 0)
				.Value(p => p.ClaimedKeyType, claimedKeyType)
				.Value(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Insert();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(MapValueKeyType.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(MapValueKeyType.EC));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1554")]
		public void InsertValueMapValueEnumFromBoxedCastVariable([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<MappedEnumKeyRecord>();
			object claimedKeyType = (MapValueKeyType)MapValueAlternateKeyType.EC;

			table.Value(p => p.Id, 0)
				.Value(p => p.ClaimedKeyType, claimedKeyType)
				.Value(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Insert();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(MapValueKeyType.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(MapValueKeyType.EC));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1554")]
		public void InsertValueMapValueEnumFromInlineCast([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<MappedEnumKeyRecord>();
			table.Value(p => p.Id, 0)
				.Value(p => p.ClaimedKeyType, (MapValueKeyType)MapValueAlternateKeyType.EC)
				.Value(p => p.ClaimedKeyTypeN, (MapValueKeyType)MapValueAlternateKeyType.EC)
				.Insert();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(MapValueKeyType.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(MapValueKeyType.EC));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1554")]
		public void FluentUpdateSetMapValueEnumFromVariable([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureFluentKeyRecordMapping());
			using var table = db.CreateLocalTable<FluentMappedEnumKeyRecord>();
			db.Insert(FluentMappedEnumKeyRecord.TestInstance);

			var claimedKeyType = MapValueKeyType.EC;

			table.Where(p => p.Id == 0).AsUpdatable()
				.Set(p => p.ClaimedKeyType, claimedKeyType)
				.Set(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Update();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(MapValueKeyType.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(MapValueKeyType.EC));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1554")]
		public void FluentUpdateSetMapValueEnumFromBoxedVariable([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureFluentKeyRecordMapping());
			using var table = db.CreateLocalTable<FluentMappedEnumKeyRecord>();
			db.Insert(FluentMappedEnumKeyRecord.TestInstance);

			object claimedKeyType = MapValueKeyType.EC;

			table.Where(p => p.Id == 0).AsUpdatable()
				.Set(p => p.ClaimedKeyType, claimedKeyType)
				.Set(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Update();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(MapValueKeyType.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(MapValueKeyType.EC));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1554")]
		public void FluentUpdateSetMapValueEnumFromCastVariable([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureFluentKeyRecordMapping());
			using var table = db.CreateLocalTable<FluentMappedEnumKeyRecord>();
			db.Insert(FluentMappedEnumKeyRecord.TestInstance);

			var claimedKeyType = (MapValueKeyType)MapValueAlternateKeyType.EC;

			table.Where(p => p.Id == 0).AsUpdatable()
				.Set(p => p.ClaimedKeyType, claimedKeyType)
				.Set(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Update();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(MapValueKeyType.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(MapValueKeyType.EC));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1554")]
		public void FluentUpdateSetMapValueEnumFromBoxedCastVariable([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureFluentKeyRecordMapping());
			using var table = db.CreateLocalTable<FluentMappedEnumKeyRecord>();
			db.Insert(FluentMappedEnumKeyRecord.TestInstance);

			object claimedKeyType = (MapValueKeyType)MapValueAlternateKeyType.EC;

			table.Where(p => p.Id == 0).AsUpdatable()
				.Set(p => p.ClaimedKeyType, claimedKeyType)
				.Set(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Update();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(MapValueKeyType.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(MapValueKeyType.EC));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1554")]
		public void FluentUpdateSetMapValueEnumFromInlineCast([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureFluentKeyRecordMapping());
			using var table = db.CreateLocalTable<FluentMappedEnumKeyRecord>();
			db.Insert(FluentMappedEnumKeyRecord.TestInstance);

			table.Where(p => p.Id == 0).AsUpdatable()
				.Set(p => p.ClaimedKeyType, (MapValueKeyType)MapValueAlternateKeyType.EC)
				.Set(p => p.ClaimedKeyTypeN, (MapValueKeyType)MapValueAlternateKeyType.EC)
				.Update();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(MapValueKeyType.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(MapValueKeyType.EC));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1554")]
		public void FluentInsertValueMapValueEnumFromVariable([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureFluentKeyRecordMapping());
			using var table = db.CreateLocalTable<FluentMappedEnumKeyRecord>();
			var claimedKeyType = MapValueKeyType.EC;

			table.Value(p => p.Id, 0)
				.Value(p => p.ClaimedKeyType, claimedKeyType)
				.Value(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Insert();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(MapValueKeyType.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(MapValueKeyType.EC));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1554")]
		public void FluentInsertValueMapValueEnumFromBoxedVariableAndNull([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureFluentKeyRecordMapping());
			using var table = db.CreateLocalTable<FluentMappedEnumKeyRecord>();
			object  claimedKeyType     = MapValueKeyType.EC;
			object? claimedKeyTypeNull = null;

			table.Value(p => p.Id, 0)
				.Value(p => p.ClaimedKeyType, claimedKeyType)
				.Value(p => p.ClaimedKeyTypeN, claimedKeyTypeNull)
				.Insert();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(MapValueKeyType.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.Null);
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1554")]
		public void FluentInsertValueMapValueEnumFromCastVariable([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureFluentKeyRecordMapping());
			using var table = db.CreateLocalTable<FluentMappedEnumKeyRecord>();
			var claimedKeyType = (MapValueKeyType)MapValueAlternateKeyType.EC;

			table.Value(p => p.Id, 0)
				.Value(p => p.ClaimedKeyType, claimedKeyType)
				.Value(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Insert();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(MapValueKeyType.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(MapValueKeyType.EC));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1554")]
		public void FluentInsertValueMapValueEnumFromBoxedCastVariable([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureFluentKeyRecordMapping());
			using var table = db.CreateLocalTable<FluentMappedEnumKeyRecord>();
			object claimedKeyType = (MapValueKeyType)MapValueAlternateKeyType.EC;

			table.Value(p => p.Id, 0)
				.Value(p => p.ClaimedKeyType, claimedKeyType)
				.Value(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Insert();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(MapValueKeyType.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(MapValueKeyType.EC));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1554")]
		public void FluentInsertValueMapValueEnumFromInlineCast([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureFluentKeyRecordMapping());
			using var table = db.CreateLocalTable<FluentMappedEnumKeyRecord>();
			table.Value(p => p.Id, 0)
				.Value(p => p.ClaimedKeyType, (MapValueKeyType)MapValueAlternateKeyType.EC)
				.Value(p => p.ClaimedKeyTypeN, (MapValueKeyType)MapValueAlternateKeyType.EC)
				.Insert();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(MapValueKeyType.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(MapValueKeyType.EC));
			}
		}

		private static MappingSchema ConfigureFluentKeyRecordMapping()
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<FluentMappedEnumKeyRecord>()
					.HasTableName("FluentMappedEnumKeyRecord")
					.Property(p => p.Id)
						.IsPrimaryKey()
					.Property(p => p.ClaimedKeyType)
					.Property(p => p.ClaimedKeyTypeN)
				.Build();

			return ms;
		}

		public enum UserReleaseVersion
		{
			FirstRelease = 0,
			SecondRelease = 1
		}

		[Table("Users")]
		public class NullableEnumVersionUser
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column(CanBeNull = true)]
			public UserReleaseVersion? Version { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1946")]
		public void NullableEnumLessThanEnumVariable([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new NullableEnumVersionUser { Id = 1, Version = UserReleaseVersion.FirstRelease } });
			var maxVersion = UserReleaseVersion.SecondRelease;

			var query = from u in table
						where u.Version!.Value < maxVersion
						select u.Id;

			var ids1 = query.ToArray();
			Assert.That(ids1, Has.Length.EqualTo(1));
		}

		public enum StringMappedStatus
		{
			Undefined = 0,
			Used = 40,
			Finished = 88
		}

		public class StringMappedStatusRecord
		{
			public Guid Id { get; set; }

			public StringMappedStatus Status { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2372")]
		public void InsertEnumWithStringConvertersAndFluentDataType([DataSources] string context)
		{
			Model.ITestDataContext? db1 = null;
			try
			{
				var ms1 = new MappingSchema();
				var fmb1 = new FluentMappingBuilder(ms1);
				fmb1.Entity<StringMappedStatusRecord>()
				  .HasTableName("InventoryResource")
				  .Property(e => e.Id).IsPrimaryKey()
				  .Property(e => e.Status).HasDataType(DataType.NVarChar)
				  .Build();

				var ms2 = new MappingSchema();
				ms2.SetConverter<StringMappedStatus, string>((obj) =>
				{
					return obj.ToString();
				});
				ms2.SetConverter<StringMappedStatus, DataParameter>((obj) =>
				{
					return new DataParameter { Value = obj.ToString() };
				});
				ms2.SetConverter<string, StringMappedStatus>((txt) =>
				{
#pragma warning disable CA2263 // Prefer generic overload when type is known
					return (StringMappedStatus)Enum.Parse(typeof(StringMappedStatus), txt, true);
#pragma warning restore CA2263 // Prefer generic overload when type is known
				});

				var fmb2 = new FluentMappingBuilder(ms2);
				fmb2.Entity<StringMappedStatusRecord>()
				  .HasTableName("InventoryResource")
				  .Property(e => e.Id).IsPrimaryKey()
				  .Property(e => e.Status)
				  .Build();

				db1 = GetDataContext(context, ms1);
				db1.DropTable<StringMappedStatusRecord>(throwExceptionIfNotExists: false);
				using var t = db1.CreateLocalTable<StringMappedStatusRecord>();

				using var db2 = GetDataContext(context, ms2);
				var dto1 = new StringMappedStatusRecord
				{
					Status = StringMappedStatus.Used,
					Id     = TestData.Guid1
				};
				db2.Insert(dto1);
			}
			finally
			{
				if (db1 != null)
					db1.Dispose();
			}
		}

		public enum InEnumValue
		{
			Val1 = 1,
			Val2 = 2,
			Val3 = 3,
		}

		[Table]
		public class InEnumTable
		{
			[Column, PrimaryKey] public int  Id    { get; set; }
			[Column]             public int? Value { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5398")]
		public void CastNullableIntToEnumWithIn([DataSources] string context)
		{
			var testData = new[]
			{
				new InEnumTable { Id = 1, Value = 1 },
				new InEnumTable { Id = 2, Value = 2 },
				new InEnumTable { Id = 3, Value = 3 },
				new InEnumTable { Id = 4, Value = null },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var actual = table
				.Where(i => ((InEnumValue)i.Value!).In(InEnumValue.Val1))
				.Select(i => i.Id)
				.ToList();

			var expected = testData
				.Where(i => i.Value != null && ((InEnumValue)i.Value!).In(InEnumValue.Val1))
				.Select(i => i.Id)
				.ToList();

			AreEqual(actual, expected);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5398")]
		public void CastNullableIntToEnumWithInMultipleValues([DataSources] string context)
		{
			var testData = new[]
			{
				new InEnumTable { Id = 1, Value = 1 },
				new InEnumTable { Id = 2, Value = 2 },
				new InEnumTable { Id = 3, Value = 3 },
				new InEnumTable { Id = 4, Value = null },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var actual = table
				.Where(i => ((InEnumValue)i.Value!).In(InEnumValue.Val1, InEnumValue.Val2))
				.Select(i => i.Id)
				.OrderBy(id => id)
				.ToList();

			var expected = testData
				.Where(i => i.Value != null && ((InEnumValue)i.Value!).In(InEnumValue.Val1, InEnumValue.Val2))
				.Select(i => i.Id)
				.OrderBy(id => id)
				.ToList();

			AreEqual(actual, expected);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5398")]
		public void CastIntToEnumWithIn([DataSources] string context)
		{
			var testData = new[]
			{
				new InEnumTable { Id = 1, Value = 1 },
				new InEnumTable { Id = 2, Value = 2 },
				new InEnumTable { Id = 3, Value = 3 },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var query = table
				.Where(i => ((InEnumValue)i.Value!).In(InEnumValue.Val1, InEnumValue.Val3))
				.Select(i => i.Id)
				.OrderBy(id => id);

			AssertQuery(query);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5398")]
		public void CastNullableIntToNullableEnumWithIn([DataSources] string context)
		{
			var testData = new[]
			{
				new InEnumTable { Id = 1, Value = 1 },
				new InEnumTable { Id = 2, Value = 2 },
				new InEnumTable { Id = 3, Value = 3 },
				new InEnumTable { Id = 4, Value = null },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var actual = table
				.Where(i => ((InEnumValue?)i.Value).In(InEnumValue.Val1))
				.Select(i => i.Id)
				.ToList();

			var expected = testData
				.Where(i => i.Value != null && ((InEnumValue?)i.Value).In(InEnumValue.Val1))
				.Select(i => i.Id)
				.ToList();

			AreEqual(actual, expected);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5398")]
		public void CastNullableIntToNullableEnumWithInMultipleValues([DataSources] string context)
		{
			var testData = new[]
			{
				new InEnumTable { Id = 1, Value = 1 },
				new InEnumTable { Id = 2, Value = 2 },
				new InEnumTable { Id = 3, Value = 3 },
				new InEnumTable { Id = 4, Value = null },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var actual = table
				.Where(i => ((InEnumValue?)i.Value).In(InEnumValue.Val1, InEnumValue.Val2))
				.Select(i => i.Id)
				.OrderBy(id => id)
				.ToList();

			var expected = testData
				.Where(i => i.Value != null && ((InEnumValue?)i.Value).In(InEnumValue.Val1, InEnumValue.Val2))
				.Select(i => i.Id)
				.OrderBy(id => id)
				.ToList();

			AreEqual(actual, expected);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5398")]
		public void CastNullableIntToNullableEnumWithInNull([DataSources] string context)
		{
			var testData = new[]
			{
				new InEnumTable { Id = 1, Value = 1 },
				new InEnumTable { Id = 2, Value = 2 },
				new InEnumTable { Id = 3, Value = null },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var actual = table
				.Where(i => ((InEnumValue?)i.Value).In(InEnumValue.Val1, null))
				.Select(i => i.Id)
				.OrderBy(id => id)
				.ToList();

			var expected = testData
				.Where(i => i.Value == null || ((InEnumValue?)i.Value).In(InEnumValue.Val1, null))
				.Select(i => i.Id)
				.OrderBy(id => id)
				.ToList();

			AreEqual(actual, expected);
		}

#nullable disable

		enum ReceiptStatus
		{
			Ok,
			Fail,
		}

		class EnumStatusReceipt
		{
			[Column("RECEIPT_NO")]  public string ReceiptNo  { get; set; }
			[Column("SERV_CATG")]   public ReceiptStatus  Status     { get; set; }
			[Column("PROPERTY_ID")] public int?   PropertyId { get; set; }

			[Association(ThisKey = nameof(PropertyId), OtherKey = nameof(ReceiptProperty.Id))]
			public ReceiptProperty ReceiptProperty { get; }
		}

		class ReceiptProperty
		{
			public int Id { get; set; }
		}

		class OptionalStatusTable
		{
			[Column] public int    Id     { get; set; }
			[Column] public ReceiptStatus? Status { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5666")]
		public void NonNullableEnumColumnDoesNotLeakIntoAssociationJoin([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db        = GetDataContext(context);
			using var receipts  = db.CreateLocalTable(new[]
			{
				new EnumStatusReceipt { ReceiptNo = "R1", Status = ReceiptStatus.Ok,   PropertyId = 100 },
				new EnumStatusReceipt { ReceiptNo = "R2", Status = ReceiptStatus.Fail, PropertyId = 100 },
				new EnumStatusReceipt { ReceiptNo = "R3", Status = ReceiptStatus.Ok,   PropertyId = 999 },
			});
			using var lookup    = db.CreateLocalTable(new[]
			{
				new ReceiptProperty { Id = 100 },
			});

			var query =
				from i in db.GetTable<EnumStatusReceipt>()
				where i.ReceiptProperty.Id == 100
				select new { i.ReceiptNo };

			// R1 (Status = Ok = 0) and R2 (Status = Fail) both reference Property 100, so both must
			// be returned. The bug injects "SERV_CATG <> 0" into the join, silently dropping R1.
			var result = query.ToList();
			Assert.That(result.Select(x => x.ReceiptNo), Is.EquivalentTo(new[] { "R1", "R2" }));

			// The non-nullable enum column is never referenced by the query; it must not leak into the SQL.
			var sql = query.ToSqlQuery().Sql;
			Assert.That(sql, Does.Not.Contain("SERV_CATG"));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5666")]
		public void NullableEnumNullComparison([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool notEqual)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(new[]
			{
				new OptionalStatusTable { Id = 1, Status = ReceiptStatus.Ok }, // nullable-enum vs null goes through the generic null path (IS [NOT] NULL); the "<> 0" regression itself is guarded by NonNullableEnumColumnDoesNotLeakIntoAssociationJoin
				new OptionalStatusTable { Id = 2, Status = null },
			});

			var query = notEqual
				? db.GetTable<OptionalStatusTable>().Where(x => x.Status != null)
				: db.GetTable<OptionalStatusTable>().Where(x => x.Status == null);

			var sql = query.ToSqlQuery().Sql;
			Assert.That(sql, Does.Contain(notEqual ? "IS NOT NULL" : "IS NULL"));

			var result = query.Select(x => x.Id).ToList();
			Assert.That(result, Is.EquivalentTo(notEqual ? new[] { 1 } : new[] { 2 }));
		}

#nullable restore

		[Table(Name = "Person")]
		public class PersonWithNullableEnumMiddleName
		{
			[SequenceName(ProviderName.Firebird, "PersonID")]
			[Column("PersonID", Configuration = ProviderName.ClickHouse)]
			[Column("PersonID", IsIdentity = true)]
			[PrimaryKey]
			        public int     ID         { get; set; }

			[Column]public Gender  Gender     { get; set; }
			[Column]public string  FirstName  { get; set; } = null!;
			[DataType(DataType.NVarChar, Configuration = ProviderName.Sybase)]
			[Column]public MiddleNameKind?   MiddleName { get; set; }
			[Column]public string  LastName   { get; set; } = null!;
		}

		public enum MiddleNameKind
		{
			A
		}

		[ActiveIssue(5594, Configuration = TestProvName.AllYdb, ErrorTypeName = "System.InvalidOperationException", ErrorMessage = "Writing value of 'null' is not supported without explicit mapping to the YdbDbType",
			Details = "YDB: null parameter requires an explicit type (potential type-propagation bug, not investigated)")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/693")]
		public void NullableEnumConvertedToStringAndDataParameter([DataSources(false)] string context)
		{
			ResetPersonIdentity(context);

			var ms = new MappingSchema();

			ms.SetConverter<MiddleNameKind?, string?>((obj) =>
			{
				if (obj != null)
					return obj.ToString();
				return null;
			});

			ms.SetConverter<MiddleNameKind?,DataParameter>((obj) =>
			{
				if (obj != null)
					return new DataParameter { Value = obj.ToString(), DataType = DataType.NVarChar };
				return new DataParameter { Value = DBNull.Value };
			});

			ms.SetConverter<string, MiddleNameKind?>((txt) =>
			{
				if (string.IsNullOrEmpty(txt))
					return null;
#pragma warning disable CA2263 // Prefer generic overload when type is known
				return (MiddleNameKind?)Enum.Parse(typeof(MiddleNameKind), txt, true);
#pragma warning restore CA2263 // Prefer generic overload when type is known
			});

			using (var db = GetDataContext(context, ms))
			using (new RestoreBaseTables(db))
			{
				var obj = new PersonWithNullableEnumMiddleName
				{
					FirstName  = "a",
					MiddleName = MiddleNameKind.A,
					LastName   = "b",
					Gender     = Gender.Male
				};

				int id1;
				if (context.IsAnyOf(TestProvName.AllClickHouse))
				{
					obj.ID = id1 = 100;
					db.Insert(obj);
				}
				else
					id1 = db.InsertWithInt32Identity(obj);

				var obj2 = new PersonWithNullableEnumMiddleName
				{
					FirstName  = "c",
					MiddleName = null,
					LastName   = "d",
					Gender     = Gender.Male
				};

				int id2;
				if (context.IsAnyOf(TestProvName.AllClickHouse))
				{
					obj2.ID = id2 = 101;
					db.Insert(obj2);
				}
				else
					id2 = db.InsertWithInt32Identity(obj2);

				var obj3 = db.GetTable<PersonWithNullableEnumMiddleName>().First(_ => _.ID == id1);
				var obj4 = db.GetTable<PersonWithNullableEnumMiddleName>().First(_ => _.ID == id2);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(obj4.MiddleName, Is.Null);
					Assert.That(obj3.MiddleName, Is.Not.Null);
				}
			}
		}
	}
}
