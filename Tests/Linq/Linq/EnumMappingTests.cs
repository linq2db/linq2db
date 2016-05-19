using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
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

		[Table("LinqDataTypes")]
		class TestTable1
		{
			[PrimaryKey, Column("ID")] public int Id;
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

		class Cleaner : IDisposable
		{
			readonly ITestDataContext _db;

			public Cleaner(ITestDataContext db)
			{
				_db = db;
				Clean();
			}

			private void Clean()
			{
				_db.GetTable<RawTable>().Where(r => r.Id == RID).Delete();
			}

			public void Dispose()
			{
				try
				{
					// rollback emulation for WCF
					Clean();
				}
				catch (Exception)
				{
				}
			}
		}

		const long VAL2 = 12;
		const long VAL1 = 11;
		const int  RID  = 101;

		[Test, DataContextSource]
		public void EnumMapInsert1(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapInsert2(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapInsert3(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapInsert4(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapWhere1(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapWhere2(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapWhere3(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapWhere4(string context)
		{
			using (var db = GetDataContext(context))
			{
				using (new Cleaner(db))
				{
					db.GetTable<RawTable>().Insert(() => new RawTable
					{
						Id        = RID,
						TestField = VAL2
					});

					var result = db.GetTable<NullableTestTable2>()
						.Where(r => r.Id == RID && r.TestField == TestEnum21.Value2)
						.Select(r => r.TestField).FirstOrDefault();
					Assert.That(result, Is.EqualTo(TestEnum21.Value2));
				}
			}
		}

		[Test, DataContextSource]
		public void EnumMapUpdate1(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapUpdate2(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapUpdate3(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapUpdate4(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapSelectAnon1(string context)
		{
			using (var db  = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapSelectAnon2(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapSelectAnon3(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapSelectAnon4(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapDelete1(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapDelete2(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapDelete3(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapDelete4(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapSet1(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapSet2(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapSet3(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapSet4(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapSet5(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapSet6(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapContains1(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapContains2(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapContains3(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapContains4(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapSelectNull1(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapSelectNull2(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapWhereNull1(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapWhereNull2(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapInsertObject1(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapInsertObject2(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapInsertObject3(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapInsertObject4(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapInsertFromSelectWithParam1(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapInsertFromSelectWithParam2(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapInsertFromSelectWithParam3(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapInsertFromSelectWithParam4(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapDeleteEquals1(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapDeleteEquals2(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapDeleteEquals3(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapDeleteEquals4(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapCustomPredicate1(string context)
		{
			using (var db = GetDataContext(context))
			{
				using (new Cleaner(db))
				{
					db.GetTable<RawTable>().Insert(() => new RawTable
					{
						Id = RID,
						TestField = VAL2
					});

					var entityParameter  = Expression.Parameter(typeof(TestTable1), "entity"); // parameter name required for BLToolkit
					var filterExpression = Expression.Equal(Expression.Field(entityParameter, "TestField"), Expression.Constant(TestEnum1.Value2));
					var filterPredicate  = Expression.Lambda<Func<TestTable1, bool>>(filterExpression, entityParameter);
					var result = db.GetTable<TestTable1>().Where(filterPredicate).ToList();

					Assert.AreEqual(1, result.Count);
				}
			}
		}

		[Test, DataContextSource]
		public void EnumMapCustomPredicate2(string context)
		{
			using (var db = GetDataContext(context))
			{
				using (new Cleaner(db))
				{
					db.GetTable<RawTable>().Insert(() => new RawTable
					{
						Id = RID,
						TestField = VAL2
					});

					var entityParameter  = Expression.Parameter(typeof(TestTable2), "entity"); // parameter name required for BLToolkit
					var filterExpression = Expression.Equal(Expression.Field(entityParameter, "TestField"), Expression.Constant(TestEnum21.Value2));
					var filterPredicate  = Expression.Lambda<Func<TestTable2, bool>>(filterExpression, entityParameter);
					var result = db.GetTable<TestTable2>().Where(filterPredicate).ToList();

					Assert.AreEqual(1, result.Count);
				}
			}
		}

		[Table("LinqDataTypes")]
		class TestTable3
		{
			[PrimaryKey]
			public int ID;
			
			[Column("BigIntValue")]
			public TestEnum1? TargetType;

			[Column("IntValue")]
			public int? TargetID;
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

		[Test, DataContextSource]
		public void Test_4_1_18_Regression1(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Table("LinqDataTypes")]
		class TestTable4
		{
			[PrimaryKey]
			public int ID;

			[Column("BigIntValue")]
			public TestEnum2? TargetType;

			[Column("IntValue")]
			public int? TargetID;
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

		[Test, DataContextSource]
		public void Test_4_1_18_Regression2(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		class NullableResult
		{
			public TestEnum1? Value;
		}

		TestEnum1 Convert(TestEnum1 val)
		{
			return val;
		}

		[Test, DataContextSource]
		public void EnumMapSelectNull_Regression(string context)
		{
			using (var db = GetDataContext(context))
			{
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

		[Test, DataContextSource(ProviderName.Access)]
		public void TestFlagEnum(string context)
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

		[Test, DataContextSource]
		public void EnumMapIntermediateObject1(string context)
		{
			using (var db = GetDataContext(context))
			{
				using (new Cleaner(db))
				{
					db.GetTable<RawTable>().Insert(() => new RawTable
					{
						Id = RID,
						TestField = VAL2
					});

					Assert.That(
						db.GetTable<TestTable1>()
						.Select(r => new {r.Id, r.TestField})
						.Where(r => r.Id == RID && r.TestField == TestEnum1.Value2).Count(), Is.EqualTo(1));
				}
			}
		}

		[Test, DataContextSource]
		public void EnumMapIntermediateObject2(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapIntermediateObject3(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test, DataContextSource]
		public void EnumMapIntermediateObject4(string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

	}
}
