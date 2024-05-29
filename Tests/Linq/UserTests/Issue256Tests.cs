using System;
using System.Data.Linq;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	/// <summary>
	/// SimpleTest tests that parameter object released after use.
	/// RetryTest tests that parameter release do not break query re-execution.
	/// Oracle tests fail due to LOB types limitation in Oracle, like missing support for direct comparison and linq2db
	/// doesn't generate proper comparison for such cases.
	/// Tests executed against all providers, because they could also uncover memory leaks in providers.
	/// </summary>
	[TestFixture]
	public class Issue256Tests : TestBase
	{
		static readonly DateTime _date = TestData.DateTime;

		[Table("LinqDataTypes")]
		public class LinqDataTypesWithPK
		{
			[PrimaryKey]                         public int      ID;
			[Column]                             public decimal  MoneyValue;
			[Column(DataType=DataType.DateTime)] public DateTime DateTimeValue;
			[Column]                             public bool     BoolValue;
			[Column]                             public Guid     GuidValue;
			[Column]                             public Binary?  BinaryValue;
			[Column]                             public short    SmallIntValue;
		}

		static Action<ITestDataContext,byte[],int>[] TestActions => new Action<ITestDataContext,byte[],int>[]
		{
			Unused,
			SelectWhere,
			SelectSelect,
			SelectOrderBy,

			UpdateWhere,
			UpdateSet,
			UpdateUpdate,

			InsertInsert,

			DeleteWhere,

			NonLinqInsert,
			NonLinqUpdate,
			NonLinqDelete,
		};

		[Test(Description = "Demonstrates memory leak when fails")]
		public void SimpleTest(
			[IncludeDataSources(TestProvName.AllSQLite)] string                              context,
			[ValueSource(nameof(TestActions))]           Action<ITestDataContext,byte[],int> action)
		{
			using var _ = new DisableBaseline("test name conflicts");
			Test(context, action, 1);
		}

		[Test(Description = "Demonstrates memory leak when fails")]
		public void RetryTest(
			[IncludeDataSources(TestProvName.AllSQLite)] string                              context,
			[ValueSource(nameof(TestActions))]           Action<ITestDataContext,byte[],int> action)
		{
			using var _ = new DisableBaseline("test name conflicts");
			Test(context, action, 3);
		}

		private void Test(string context, Action<ITestDataContext, byte[], int> testAction, int calls)
		{
			using (var db = GetDataContext(context))
			{
				db.Insert(new LinqDataTypes
				{
					ID            = 256,
					BinaryValue   = new byte[] { 1, 2, 3 },
					DateTimeValue = _date,
					BoolValue     = false,
					GuidValue     = Guid.Empty,
					MoneyValue    = 0,
					SmallIntValue = 0
				});

				try
				{
					TestWrapper(db, testAction, calls);
				}
				finally
				{
					db.Types.Delete(_ => _.ID == 256);
				}
			}
		}

		private void TestWrapper(ITestDataContext db, Action<ITestDataContext, byte[], int> test, int calls)
		{
			var value = RunTest(db, test, calls);

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			Assert.That(value.IsAlive, Is.False);
		}

		private static WeakReference RunTest(ITestDataContext db, Action<ITestDataContext, byte[], int> test, int calls)
		{
			var value = new byte[] { 1, 2, 3 };
			test(db, value, calls);
			return new WeakReference(value);
		}

		/// <summary>
		/// Value is not used and shouldn't produce memory leaks.
		/// This one used to detect situations when testing approach in broken in general due to runtime/GC behavior
		/// changes.
		/// </summary>
		private static void Unused(ITestDataContext db, byte[] value, int calls)
		{
			var query = db.Types.Where(_ => _.ID == 256);

			while (calls > 0)
			{
				var result = query.ToList();

				Assert.That(result, Has.Count.EqualTo(1));
				Assert.That(result[0].ID, Is.EqualTo(256));

				calls--;
			}
		}

		private static void SelectWhere(ITestDataContext db, byte[] value, int calls)
		{
			var query = db.Types.Where(_ => _.BinaryValue == value);

			while (calls > 0)
			{
				var result = query.ToList();

				Assert.That(result, Has.Count.EqualTo(1));
				Assert.That(result[0].ID, Is.EqualTo(256));

				calls--;
			}
		}

		private static void SelectSelect(ITestDataContext db, byte[] value, int calls)
		{
			var query = db.Types.Where(_ => _.ID == 256).Select(_ => new { _.ID, Value = value });

			while (calls > 0)
			{
				var result = query.ToList();

				Assert.That(result, Has.Count.EqualTo(1));
				Assert.That(result[0].ID, Is.EqualTo(256));

				calls--;
			}
		}

		private static void SelectOrderBy(ITestDataContext db, byte[] value, int calls)
		{
			var query = db.Types.Where(_ => _.ID == 256).OrderBy(_ => _.BinaryValue == value);

			while (calls > 0)
			{
				var result = query.ToList();

				Assert.That(result, Has.Count.EqualTo(1));
				Assert.That(result[0].ID, Is.EqualTo(256));

				calls--;
			}
		}

		private static void UpdateWhere(ITestDataContext db, byte[] value, int calls)
		{
			var query = db.Types.Where(_ => _.BinaryValue == value);

			var expected = true;
			while (calls > 0)
			{
				query.Set(_ => _.BoolValue, _ => !_.BoolValue).Update();
				var result = db.Types.Where(_ => _.ID == 256).ToList();

				Assert.That(result, Has.Count.EqualTo(1));
				Assert.Multiple(() =>
				{
					Assert.That(result[0].ID, Is.EqualTo(256));
					Assert.That(result[0].BoolValue, Is.EqualTo(expected));
				});

				calls--;
				expected = !expected;
			}
		}

		private static void UpdateSet(ITestDataContext db, byte[] value, int calls)
		{
			var query = db.Types.Where(_ => _.ID == 256);

			while (calls > 0)
			{
				query.Set(_ => _.BinaryValue, _ => null).Update();
				var result = query.ToList();

				Assert.That(result, Has.Count.EqualTo(1));
				Assert.Multiple(() =>
				{
					Assert.That(result[0].ID, Is.EqualTo(256));
					Assert.That(result[0].BinaryValue, Is.Null);
				});

				query.Set(_ => _.BinaryValue, _ => value).Update();
				result = query.ToList();

				Assert.That(result, Has.Count.EqualTo(1));
				Assert.Multiple(() =>
				{
					Assert.That(result[0].ID, Is.EqualTo(256));
					Assert.That(value.SequenceEqual(result[0].BinaryValue!.ToArray()), Is.True);
				});

				calls--;
			}
		}

		private static void UpdateUpdate(ITestDataContext db, byte[] value, int calls)
		{
			var query = db.Types.Where(_ => _.ID == 256);

			while (calls > 0)
			{
				query.Update(_ => new LinqDataTypes() { BinaryValue = null });
				var result = query.ToList();

				Assert.That(result, Has.Count.EqualTo(1));
				Assert.Multiple(() =>
				{
					Assert.That(result[0].ID, Is.EqualTo(256));
					Assert.That(result[0].BinaryValue, Is.Null);
				});

				query.Update(_ => new LinqDataTypes() { BinaryValue = value });
				result = query.ToList();

				Assert.That(result, Has.Count.EqualTo(1));
				Assert.Multiple(() =>
				{
					Assert.That(result[0].ID, Is.EqualTo(256));
					Assert.That(value.SequenceEqual(result[0].BinaryValue!.ToArray()), Is.True);
				});

				calls--;
			}
		}

		private static void InsertInsert(ITestDataContext db, byte[] value, int calls)
		{
			var query = db.Types.Where(_ => _.ID == 256);

			while (calls > 0)
			{
				Assert.That(query.Delete(), Is.EqualTo(1));
				db.Types.Insert(() => new LinqDataTypes() { ID = 256, BinaryValue = value });
				var result = query.ToList();

				Assert.That(result, Has.Count.EqualTo(1));
				Assert.Multiple(() =>
				{
					Assert.That(result[0].ID, Is.EqualTo(256));
					Assert.That(value.SequenceEqual(result[0].BinaryValue!.ToArray()), Is.True);
				});

				calls--;
			}
		}

		private static void DeleteWhere(ITestDataContext db, byte[] value, int calls)
		{
			var query = db.Types.Where(_ => _.BinaryValue == value);

			while (calls > 0)
			{
				query.Delete();
				var result = db.Types.Where(_ => _.ID == 256).ToList();

				Assert.That(result, Is.Empty);

				calls--;
				db.Types.Insert(() => new LinqDataTypes() { ID = 256, BinaryValue = value });
			}
		}

		private static void NonLinqInsert(ITestDataContext db, byte[] value, int calls)
		{
			db.Insert(new LinqDataTypesWithPK() { ID = 10256, BinaryValue = value, DateTimeValue = _date });
			var result = db.Types.Where(_ => _.ID == 10256).ToList();
			db.Types.Where(_ => _.ID == 10256).Delete();

			Assert.That(result, Has.Count.EqualTo(1));
			Assert.Multiple(() =>
			{
				Assert.That(result[0].ID, Is.EqualTo(10256));
				Assert.That(value.SequenceEqual(result[0].BinaryValue!.ToArray()), Is.True);
			});
		}

		private static void NonLinqUpdate(ITestDataContext db, byte[] value, int calls)
		{
			var query = db.Types.Where(_ => _.ID == 256);

			while (calls > 0)
			{
				db.Update(new LinqDataTypesWithPK() { ID = 256, BinaryValue = null, DateTimeValue = _date });
				var result = query.ToList();

				Assert.That(result, Has.Count.EqualTo(1));
				Assert.Multiple(() =>
				{
					Assert.That(result[0].ID, Is.EqualTo(256));
					Assert.That(result[0].BinaryValue, Is.Null);
				});

				db.Update(new LinqDataTypesWithPK() { ID = 256, BinaryValue = value, DateTimeValue = _date });
				result = query.ToList();

				Assert.That(result, Has.Count.EqualTo(1));
				Assert.Multiple(() =>
				{
					Assert.That(result[0].ID, Is.EqualTo(256));
					Assert.That(value.SequenceEqual(result[0].BinaryValue!.ToArray()), Is.True);
				});

				calls--;
			}
		}

		private static void NonLinqDelete(ITestDataContext db, byte[] value, int calls)
		{
			while (calls > 0)
			{
				db.Delete(new LinqDataTypesWithPK() { ID = 256, BinaryValue = value, DateTimeValue = _date });
				var result = db.Types.Where(_ => _.ID == 256).ToList();

				Assert.That(result, Is.Empty);

				calls--;
				db.Types.Insert(() => new LinqDataTypes() { ID = 256, BinaryValue = value });
			}
		}
	}
}
