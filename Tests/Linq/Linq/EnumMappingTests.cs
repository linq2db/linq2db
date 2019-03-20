﻿using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	using LinqToDB.Common;
	using Model;

	[TestFixture, Category("MapValue")]
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
		class TestTable1
		{
			[PrimaryKey, Column("ID")] public int       Id;
			[Column("BigIntValue")]    public TestEnum1 TestField;
		}

		[Table("LinqDataTypes")]
		class TestTable2
		{
			[PrimaryKey, Column("ID")] public int        Id;
			[Column("BigIntValue")]    public TestEnum21 TestField;
			[Column("IntValue")]       public TestEnum3  Int32Field;
		}

		[Table("LinqDataTypes")]
		class NullableTestTable1
		{
			[PrimaryKey, Column("ID")] public int?       Id;
			[Column("BigIntValue")]    public TestEnum1? TestField;
		}

		[Table("LinqDataTypes")]
		class NullableTestTable2
		{
			[PrimaryKey, Column("ID")] public int?        Id;
			[Column("BigIntValue")]    public TestEnum21? TestField;
			[Column("IntValue")]       public TestEnum3?  Int32Field;
		}

		[Table("LinqDataTypes")]
		class RawTable
		{
			[PrimaryKey, Column("ID")] public int  Id;
			[Column("BigIntValue")]    public long TestField;
			[Column("IntValue")]       public int  Int32Field;
		}

		[Table("LinqDataTypes")]
		class UndefinedValueTest
		{
			[PrimaryKey, Column("ID")] public int?          Id;
			[Column("BigIntValue")]    public UndefinedEnum TestField;
		}

		class Cleaner : IDisposable
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

				Assert.AreEqual(1, db.GetTable<RawTable>().Where(r => r.Id == RID && r.TestField == VAL2).Count());
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

				Assert.AreEqual(1, db.GetTable<RawTable>().Where(r => r.Id == RID && r.TestField == VAL2).Count());
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

				Assert.AreEqual(1, db.GetTable<RawTable>()
					.Where(r => r.Id == RID && r.TestField == VAL2).Count());
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

				Assert.AreEqual(1, db.GetTable<RawTable>().Where(r => r.Id == RID && r.TestField == VAL2).Count());
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
				Assert.True(result == TestEnum1.Value2);
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
				Assert.True(result == TestEnum21.Value2);
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
				Assert.True(result == TestEnum1.Value2);
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

				Assert.True(result == VAL2);
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

				Assert.True(result == VAL2);
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

				Assert.True(result == VAL2);
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

				Assert.True(result == VAL2);
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
					.FirstOrDefault();

				Assert.NotNull(result);
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
					.FirstOrDefault();

				Assert.NotNull(result);
				Assert.True(result.TestField == TestEnum21.Value2);
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
					.FirstOrDefault();

				Assert.NotNull(result);
				Assert.True(result.TestField == TestEnum1.Value2);
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
					.FirstOrDefault();

				Assert.NotNull(result);
				Assert.True(result.TestField == TestEnum21.Value2);
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

				Assert.True(1 == db.GetTable<TestTable1>().Delete(r => r.Id == RID && r.TestField == TestEnum1.Value2));
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

				Assert.True(1 == db.GetTable<TestTable2>().Delete(r => r.Id == RID && r.TestField == TestEnum21.Value2));
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

				Assert.True(1 == db.GetTable<NullableTestTable1>()
					.Delete(r => r.Id == RID && r.TestField == TestEnum1.Value2));
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

				Assert.True(1 == db.GetTable<NullableTestTable2>()
					.Delete(r => r.Id == RID && r.TestField == TestEnum21.Value2));
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
				Assert.True(result == VAL2);
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
				Assert.True(result == VAL2);
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
				Assert.True(1 == db.GetTable<RawTable>().Where(r => r.Id == RID && r.Int32Field == 4).Count());
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
				Assert.True(result == VAL2);
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
				Assert.True(result == VAL2);
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
				Assert.True(1 == db.GetTable<RawTable>().Where(r => r.Id == RID && r.Int32Field == 4).Count());
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

				Assert.True(1 == db.GetTable<TestTable1>()
					.Where(r => r.Id == RID && new[] { TestEnum1.Value2 }.Contains(r.TestField)).Count());
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

				Assert.True(1 == db.GetTable<TestTable2>().Where(r => r.Id == RID && new[] { TestEnum21.Value2 }.Contains(r.TestField)).Count());
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

				Assert.True(1 == db.GetTable<NullableTestTable1>()
					.Where(r => r.Id == RID && new[] { (TestEnum1?)TestEnum1.Value2 }.Contains(r.TestField)).Count());
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

				Assert.True(1 == db.GetTable<NullableTestTable2>()
					.Where(r => r.Id == RID && new[] { (TestEnum21?)TestEnum21.Value2 }.Contains(r.TestField)).Count());
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
					.FirstOrDefault();

				Assert.NotNull(result);
				Assert.True(result.TestField == null);
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
					.FirstOrDefault();

				Assert.NotNull(result);
				Assert.True(result.TestField == null);
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
					.Select(r => new { r.TestField }).FirstOrDefault();
				Assert.NotNull(result);
				Assert.Null(result.TestField);
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
					.Select(r => new { r.TestField }).FirstOrDefault();
				Assert.NotNull(result);
				Assert.Null(result.TestField);
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

				Assert.AreEqual(1, db.GetTable<RawTable>().Where(r => r.Id == RID && r.TestField == VAL2).Count());
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

				Assert.AreEqual(1, db.GetTable<RawTable>().Where(r => r.Id == RID && r.TestField == VAL2).Count());
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

				Assert.AreEqual(1, db.GetTable<RawTable>()
					.Where(r => r.Id == RID && r.TestField == VAL2).Count());
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

				Assert.AreEqual(1, db.GetTable<RawTable>().Where(r => r.Id == RID && r.TestField == VAL2).Count());
			}
		}

		[Test]
		public void EnumMapInsertFromSelectWithParam1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var param = TestEnum1.Value1;

				var result = db.GetTable<TestTable1>()
					.Where(r => r.Id == RID && r.TestField == TestEnum1.Value2)
					.Select(r => new TestTable1
					{
						Id = r.Id,
						TestField = param
					})
					.Insert(db.GetTable<TestTable1>(), r => r);

				Assert.AreEqual(1, result);
				Assert.AreEqual(1, db.GetTable<RawTable>().Where(r => r.Id == RID && r.TestField == VAL1).Count());
			}
		}

		[Test]
		public void EnumMapInsertFromSelectWithParam2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var param = TestEnum21.Value1;

				var result = db.GetTable<TestTable2>()
					.Where(r => r.Id == RID && r.TestField == TestEnum21.Value2)
					.Select(r => new TestTable2
					{
						Id = r.Id,
						TestField = param
					})
					.Insert(db.GetTable<TestTable2>(), r => r);

				Assert.AreEqual(1, result);
				Assert.AreEqual(1, db.GetTable<RawTable>().Where(r => r.Id == RID && r.TestField == VAL1).Count());
			}
		}

		[Test]
		public void EnumMapInsertFromSelectWithParam3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var param = TestEnum1.Value1;

				var result = db.GetTable<NullableTestTable1>()
					.Where(r => r.Id == RID && r.TestField == TestEnum1.Value2)
					.Select(r => new NullableTestTable1
					{
						Id = r.Id,
						TestField = param
					})
					.Insert(db.GetTable<NullableTestTable1>(), r => r);

				Assert.AreEqual(1, result);
				Assert.AreEqual(1, db.GetTable<RawTable>().Where(r => r.Id == RID && r.TestField == VAL1).Count());
			}
		}

		[Test]
		public void EnumMapInsertFromSelectWithParam4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new Cleaner(db))
			{
				db.GetTable<RawTable>().Insert(() => new RawTable
				{
					Id = RID,
					TestField = VAL2
				});

				var param = TestEnum21.Value1;

				var result = db.GetTable<NullableTestTable2>()
					.Where(r => r.Id == RID && r.TestField == TestEnum21.Value2)
					.Select(r => new NullableTestTable2
					{
						Id = r.Id,
						TestField = param
					})
					.Insert(db.GetTable<NullableTestTable2>(), r => r);

				Assert.AreEqual(1, result);
				Assert.AreEqual(1, db.GetTable<RawTable>().Where(r => r.Id == RID && r.TestField == VAL1).Count());
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

				Assert.True(1 == db.GetTable<TestTable1>().Delete(r => r.Id == RID && r.TestField.Equals(TestEnum1.Value2)));
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

				Assert.True(1 == db.GetTable<TestTable2>().Delete(r => r.Id == RID && r.TestField.Equals(TestEnum21.Value2)));
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

				Assert.True(1 == db.GetTable<NullableTestTable1>()
					.Delete(r => r.Id == RID && r.TestField.Equals(TestEnum1.Value2)));
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

				Assert.True(1 == db.GetTable<NullableTestTable2>()
					.Delete(r => r.Id == RID && r.TestField.Equals(TestEnum21.Value2)));
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

				Assert.AreEqual(1, result.Count);
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

				Assert.AreEqual(1, result.Count);
			}
		}

		[Table("LinqDataTypes")]
		class TestTable3
		{
			[PrimaryKey]            public int        ID;
			[Column("BigIntValue")] public TestEnum1? TargetType;
			[Column("IntValue")]    public int?       TargetID;
		}

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

				Assert.AreEqual(1, result.Length);
				Assert.NotNull(result[0].Target);
				Assert.AreEqual(10, result[0].Target.Value.TargetID);
				Assert.AreEqual(TestEnum1.Value2, result[0].Target.Value.TargetType);
			}
		}

		[Table("LinqDataTypes")]
		class TestTable4
		{
			[PrimaryKey]            public int        ID;
			[Column("BigIntValue")] public TestEnum2? TargetType;
			[Column("IntValue")]    public int?       TargetID;
		}

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

				Assert.AreEqual(1, result.Length);
				Assert.NotNull(result[0].Target);
				Assert.AreEqual(10, result[0].Target.Value.TargetID);
				Assert.AreEqual(TestEnum2.Value2, result[0].Target.Value.TargetType);
			}
		}

		class NullableResult
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
					.FirstOrDefault();

				Assert.NotNull(result);
				Assert.That(result.Value, Is.EqualTo(TestEnum1.Value2));
			}
		}

		[Flags]
		enum TestFlag
		{
			Value1 = 0x1,
			Value2 = 0x2
		}

		[Table("LinqDataTypes", IsColumnAttributeRequired = false)]
		class TestTable5
		{
			public int      ID;
			public TestFlag IntValue;
		}

		[Test]
		public void TestFlagEnum([DataSources(ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result =
					from t in db.GetTable<TestTable5>()
					where (t.IntValue & TestFlag.Value1) != 0
					select t;

				var sql = result.ToString();

				Assert.That(sql, Is.Not.Contains("Convert").And.Not.Contains("Int(").And.Not.Contains("Cast"));
			}
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
		class NullableTestTable01
		{
			[PrimaryKey, Column("ID")] public int?           Id;
			[Column("IntValue")]       public NullableEnum01 Value;
		}

		[Table("LinqDataTypes")]
		class NullableTestTable02
		{
			[PrimaryKey, Column("ID")] public int?            Id;
			[Column("IntValue")]       public NullableEnum01? Value;
		}

		[Table("LinqDataTypes")]
		class NullableTestTable03
		{
			[PrimaryKey, Column("ID")] public int?           Id;
			[Column("StringValue")]    public NullableEnum02 Value;
		}

		[Table("LinqDataTypes")]
		class NullableTestTable04
		{
			[PrimaryKey, Column("ID")] public int?            Id;
			[Column("StringValue")]    public NullableEnum02? Value;
		}

		[Table("LinqDataTypes")]
		class NullableTestTable05
		{
			[PrimaryKey, Column("ID")] public int?           Id;
			[Column("IntValue")]       public NullableEnum03 Value;
		}

		[Table("LinqDataTypes")]
		class NullableTestTable06
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
		class RawTable2
		{
			[PrimaryKey, Column("ID")] public int    Id;
			[Column("IntValue")]       public int?   Int32;
			[Column("StringValue")]    public string String;
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

				Assert.AreEqual(3, records.Length);
				Assert.AreEqual(3, rawRecords.Length);

				Assert.AreEqual(RID, records[0].Id);
				Assert.AreEqual(RID, rawRecords[0].Id);
				Assert.AreEqual(NullableEnum01.Value1, records[0].Value);
				Assert.AreEqual(11, rawRecords[0].Int32);

				Assert.AreEqual(RID + 1, records[1].Id);
				Assert.AreEqual(RID + 1, rawRecords[1].Id);
				Assert.AreEqual(NullableEnum01.Value2, records[1].Value);
				Assert.AreEqual(22, rawRecords[1].Int32);

				Assert.AreEqual(RID + 2, records[2].Id);
				Assert.AreEqual(RID + 2, rawRecords[2].Id);
				// for non-nullable enum on read null value mapped
				Assert.AreEqual(NullableEnum01.Value3, records[2].Value);
				Assert.IsNull(rawRecords[2].Int32);
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

				Assert.AreEqual(4, records.Length);
				Assert.AreEqual(4, rawRecords.Length);

				Assert.AreEqual(RID, records[0].Id);
				Assert.AreEqual(RID, rawRecords[0].Id);
				Assert.AreEqual(NullableEnum01.Value1, records[0].Value);
				Assert.AreEqual(11, rawRecords[0].Int32);

				Assert.AreEqual(RID + 1, records[1].Id);
				Assert.AreEqual(RID + 1, rawRecords[1].Id);
				Assert.AreEqual(NullableEnum01.Value2, records[1].Value);
				Assert.AreEqual(22, rawRecords[1].Int32);

				Assert.AreEqual(RID + 2, records[2].Id);
				Assert.AreEqual(RID + 2, rawRecords[2].Id);
				// for nullable enum on read null is preferred before mapped value
				Assert.IsNull(records[2].Value);
				Assert.IsNull(rawRecords[2].Int32);

				Assert.AreEqual(RID + 3, records[3].Id);
				Assert.AreEqual(RID + 3, rawRecords[3].Id);
				Assert.IsNull(records[3].Value);
				Assert.IsNull(rawRecords[3].Int32);
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

				Assert.AreEqual(3, records.Length);
				Assert.AreEqual(3, rawRecords.Length);

				Assert.AreEqual(RID, records[0].Id);
				Assert.AreEqual(RID, rawRecords[0].Id);
				Assert.AreEqual(NullableEnum02.Value1, records[0].Value);
				Assert.AreEqual("11", rawRecords[0].String);

				Assert.AreEqual(RID + 1, records[1].Id);
				Assert.AreEqual(RID + 1, rawRecords[1].Id);
				Assert.AreEqual(NullableEnum02.Value2, records[1].Value);
				Assert.AreEqual("22", rawRecords[1].String);

				Assert.AreEqual(RID + 2, records[2].Id);
				Assert.AreEqual(RID + 2, rawRecords[2].Id);
				// for non-nullable enum on read null value mapped
				Assert.AreEqual(NullableEnum02.Value3, records[2].Value);
				Assert.IsNull(rawRecords[2].String);
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

				Assert.AreEqual(4, records.Length);
				Assert.AreEqual(4, rawRecords.Length);

				Assert.AreEqual(RID, records[0].Id);
				Assert.AreEqual(RID, rawRecords[0].Id);
				Assert.AreEqual(NullableEnum02.Value1, records[0].Value);
				Assert.AreEqual("11", rawRecords[0].String);

				Assert.AreEqual(RID + 1, records[1].Id);
				Assert.AreEqual(RID + 1, rawRecords[1].Id);
				Assert.AreEqual(NullableEnum02.Value2, records[1].Value);
				Assert.AreEqual("22", rawRecords[1].String);

				Assert.AreEqual(RID + 2, records[2].Id);
				Assert.AreEqual(RID + 2, rawRecords[2].Id);
				// for nullable enum on read null is preferred before mapped value
				Assert.IsNull(records[2].Value);
				Assert.IsNull(rawRecords[2].String);

				Assert.AreEqual(RID + 3, records[3].Id);
				Assert.AreEqual(RID + 3, rawRecords[3].Id);
				Assert.IsNull(records[3].Value);
				Assert.IsNull(rawRecords[3].Int32);
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

				Assert.AreEqual(3, records.Length);
				Assert.AreEqual(3, rawRecords.Length);

				Assert.AreEqual(RID, records[0].Id);
				Assert.AreEqual(RID, rawRecords[0].Id);
				Assert.AreEqual(NullableEnum03.Value1, records[0].Value);
				Assert.AreEqual(11, rawRecords[0].Int32);

				Assert.AreEqual(RID + 1, records[1].Id);
				Assert.AreEqual(RID + 1, rawRecords[1].Id);
				Assert.AreEqual(NullableEnum03.Value2, records[1].Value);
				Assert.AreEqual(0, rawRecords[1].Int32);

				Assert.AreEqual(RID + 2, records[2].Id);
				Assert.AreEqual(RID + 2, rawRecords[2].Id);
				// for non-nullable enum on read null value mapped
				Assert.AreEqual(NullableEnum03.Value3, records[2].Value);
				Assert.IsNull(rawRecords[2].Int32);
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

				Assert.AreEqual(4, records.Length);
				Assert.AreEqual(4, rawRecords.Length);

				Assert.AreEqual(RID, records[0].Id);
				Assert.AreEqual(RID, rawRecords[0].Id);
				Assert.AreEqual(NullableEnum03.Value1, records[0].Value);
				Assert.AreEqual(11, rawRecords[0].Int32);

				Assert.AreEqual(RID + 1, records[1].Id);
				Assert.AreEqual(RID + 1, rawRecords[1].Id);
				Assert.AreEqual(NullableEnum03.Value2, records[1].Value);
				Assert.AreEqual(0, rawRecords[1].Int32);

				Assert.AreEqual(RID + 2, records[2].Id);
				Assert.AreEqual(RID + 2, rawRecords[2].Id);
				// for nullable enum on read null is preferred before mapped value
				Assert.IsNull(records[2].Value);
				Assert.IsNull(rawRecords[2].Int32);

				Assert.AreEqual(RID + 3, records[3].Id);
				Assert.AreEqual(RID + 3, rawRecords[3].Id);
				Assert.IsNull(records[3].Value);
				Assert.IsNull(rawRecords[3].Int32);
			}
		}

		[Test]
		public void EnumMappingWriteUndefinedValue([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
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

					Assert.AreEqual(1, result.Count);
					Assert.AreEqual(5, result[0].TestField);
				}
			}
		}

		[Test]
		public void EnumMappingReadUndefinedValue([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (new Cleaner(db))
				{
					db.GetTable<RawTable>().Insert(() => new RawTable
					{
						Id = RID,
						TestField = 5
					});

					Assert.Throws<LinqToDBConvertException>(() =>
						db.GetTable<UndefinedValueTest>()
							.Select(r => new { r.Id, r.TestField })
							.Where(r => r.Id == RID)
							.ToList());
				}
			}
		}

		[Table]
		public class Issue1622Table
		{
			[Identity, PrimaryKey]
			public int Id { get; set; }
			[Column]
			public string SomeText { get; set; }
		}

		public enum Issue1622Enum
		{
			Value1, Value2
		}

		[Sql.Expression("{0} = {1}", InlineParameters = true, ServerSideOnly = true, IsPredicate = true)]
		public static bool SomeComparison(string column, Issue1622Enum value) => throw new InvalidOperationException();

		[Test, ActiveIssue(1622)]
		public void Issue1622Test([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.MappingSchema.SetValueToSqlConverter(typeof(Issue1622Enum),
					(sb, dt, v) =>
					{
						sb.Append("'").Append(((Issue1622Enum)v).ToString()).Append("_suffix'");
					});

				using (var table = db.CreateLocalTable<Issue1622Table>())
				{
					var item = new Issue1622Table() { Id = 1 };
					db.Insert(item);

					//var res = table.Where(e => SomeComparison(e.SomeText, Issue1622Enum.Value1)).ToArray();
					var res2 = table.Where(e => e.Id == 1).FirstOrDefault();

					Assert.That(item.Id, Is.EqualTo(res2.Id));
				}
			}
		}
	}
}
