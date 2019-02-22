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
		static readonly DateTime _date = DateTime.Now;

		[Table("LinqDataTypes")]
		public class LinqDataTypesWithPK
		{
			[PrimaryKey]                         public int      ID;
			[Column]                             public decimal  MoneyValue;
			[Column(DataType=DataType.DateTime)] public DateTime DateTimeValue;
			[Column]                             public bool     BoolValue;
			[Column]                             public Guid     GuidValue;
			[Column]                             public Binary   BinaryValue;
			[Column]                             public short    SmallIntValue;
		}

		[AttributeUsage(AttributeTargets.Parameter)]
		class Issue256TestSourceAttribute : IncludeDataSourcesAttribute
		{
			// tests are provider-agnostic
			public Issue256TestSourceAttribute() : base(ProviderName.SQLiteClassic, ProviderName.SQLiteMS) {}
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

#if !MONO && !NETSTANDARD1_6

		[Test, Explicit("Demonstrates memory leak when fails")]
		[Category("Explicit")]
		public void SimpleTest(
			[Issue256TestSource] string context,
			[ValueSource(nameof(TestActions))] Action<ITestDataContext,byte[],int> action)
		{
			Test(context, action, 1);
		}

		[Test, Explicit("Demonstrates memory leak when fails")]
		[Category("Explicit")]
		public void RetryTest(
			[Issue256TestSource] string context,
			[ValueSource(nameof(TestActions))] Action<ITestDataContext,byte[],int> action)
		{
			Test(context, action, 3);
		}

#endif

		public void Test(string context, Action<ITestDataContext, byte[], int> testAction, int calls)
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

			Assert.False(value.IsAlive);
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

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(256, result[0].ID);

				calls--;
			}
		}

		private static void SelectWhere(ITestDataContext db, byte[] value, int calls)
		{
			var query = db.Types.Where(_ => _.BinaryValue == value);

			while (calls > 0)
			{
				var result = query.ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(256, result[0].ID);

				calls--;
			}
		}

		private static void SelectSelect(ITestDataContext db, byte[] value, int calls)
		{
			var query = db.Types.Where(_ => _.ID == 256).Select(_ => new { _.ID, Value = value });

			while (calls > 0)
			{
				var result = query.ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(256, result[0].ID);

				calls--;
			}
		}

		private static void SelectOrderBy(ITestDataContext db, byte[] value, int calls)
		{
			var query = db.Types.Where(_ => _.ID == 256).OrderBy(_ => _.BinaryValue == value);

			while (calls > 0)
			{
				var result = query.ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(256, result[0].ID);

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

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(256, result[0].ID);
				Assert.AreEqual(expected, result[0].BoolValue);

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

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(256, result[0].ID);
				Assert.IsNull(result[0].BinaryValue);

				query.Set(_ => _.BinaryValue, _ => value).Update();
				result = query.ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(256, result[0].ID);
				Assert.True(value.SequenceEqual(result[0].BinaryValue.ToArray()));

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

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(256, result[0].ID);
				Assert.IsNull(result[0].BinaryValue);

				query.Update(_ => new LinqDataTypes() { BinaryValue = value });
				result = query.ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(256, result[0].ID);
				Assert.True(value.SequenceEqual(result[0].BinaryValue.ToArray()));

				calls--;
			}
		}

		private static void InsertInsert(ITestDataContext db, byte[] value, int calls)
		{
			var query = db.Types.Where(_ => _.ID == 256);

			while (calls > 0)
			{
				Assert.AreEqual(1, query.Delete());
				db.Types.Insert(() => new LinqDataTypes() { ID = 256, BinaryValue = value });
				var result = query.ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(256, result[0].ID);
				Assert.True(value.SequenceEqual(result[0].BinaryValue.ToArray()));

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

				Assert.AreEqual(0, result.Count);

				calls--;
				db.Types.Insert(() => new LinqDataTypes() { ID = 256, BinaryValue = value });
			}
		}

		private static void NonLinqInsert(ITestDataContext db, byte[] value, int calls)
		{
			db.Insert(new LinqDataTypesWithPK() { ID = 10256, BinaryValue = value, DateTimeValue = _date });
			var result = db.Types.Where(_ => _.ID == 10256).ToList();
			db.Types.Where(_ => _.ID == 10256).Delete();

			Assert.AreEqual(1, result.Count);
			Assert.AreEqual(10256, result[0].ID);
			Assert.True(value.SequenceEqual(result[0].BinaryValue.ToArray()));
		}

		private static void NonLinqUpdate(ITestDataContext db, byte[] value, int calls)
		{
			var query = db.Types.Where(_ => _.ID == 256);

			while (calls > 0)
			{
				db.Update(new LinqDataTypesWithPK() { ID = 256, BinaryValue = null, DateTimeValue = _date });
				var result = query.ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(256, result[0].ID);
				Assert.IsNull(result[0].BinaryValue);

				db.Update(new LinqDataTypesWithPK() { ID = 256, BinaryValue = value, DateTimeValue = _date });
				result = query.ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(256, result[0].ID);
				Assert.True(value.SequenceEqual(result[0].BinaryValue.ToArray()));

				calls--;
			}
		}

		private static void NonLinqDelete(ITestDataContext db, byte[] value, int calls)
		{
			while (calls > 0)
			{
				db.Delete(new LinqDataTypesWithPK() { ID = 256, BinaryValue = value, DateTimeValue = _date });
				var result = db.Types.Where(_ => _.ID == 256).ToList();

				Assert.AreEqual(0, result.Count);

				calls--;
				db.Types.Insert(() => new LinqDataTypes() { ID = 256, BinaryValue = value });
			}
		}
	}
}
