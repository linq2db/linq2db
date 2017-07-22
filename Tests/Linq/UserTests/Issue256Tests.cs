using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tests.Model;

namespace Tests.UserTests
{
	/// <summary>
	/// SimpleTest tests that parameter object released after use.
	/// RetryTest tests that parameter release do not break query re-execution.
	/// Oracle tests fail due to LOB types limitation in Oracle, like mising support for direct comparison and linq2db
	/// doesn't generate proper comparison for such cases.
	/// Tests executed against all providers, because they could also uncover memory leaks in providers.
	/// </summary>
	[TestFixture]
	public class Issue256Tests : TestBase
	{
		private readonly static DateTime Date = DateTime.Now;

		[Table("LinqDataTypes")]
		public class LinqDataTypesWithPK
		{
			[PrimaryKey]
			public int ID;
			[Column]
			public decimal MoneyValue;
			[Column(DataType = DataType.DateTime)]
			public DateTime DateTimeValue;
			[Column]
			public bool BoolValue;
			[Column]
			public Guid GuidValue;
			[Column]
			public Binary BinaryValue;
			[Column]
			public short SmallIntValue;
		}

		[AttributeUsage(AttributeTargets.Method)]
		class Issue256TestSourceAttribute : DataContextSourceAttribute
		{
			protected override IEnumerable<object[]> GetParameters(string provider)
			{
				yield return new object[] { provider, (Action<ITestDataContext, byte[], int>)Unused };

				yield return new object[] { provider, (Action<ITestDataContext, byte[], int>)SelectWhere };
				yield return new object[] { provider, (Action<ITestDataContext, byte[], int>)SelectSelect };
				yield return new object[] { provider, (Action<ITestDataContext, byte[], int>)SelectOrderBy };

				yield return new object[] { provider, (Action<ITestDataContext, byte[], int>)UpdateWhere };
				yield return new object[] { provider, (Action<ITestDataContext, byte[], int>)UpdateSet };
				yield return new object[] { provider, (Action<ITestDataContext, byte[], int>)UpdateUpdate };

				yield return new object[] { provider, (Action<ITestDataContext, byte[], int>)InsertInsert };

				yield return new object[] { provider, (Action<ITestDataContext, byte[], int>)DeleteWhere };

				yield return new object[] { provider, (Action<ITestDataContext, byte[], int>)NonLinqInsert };
				yield return new object[] { provider, (Action<ITestDataContext, byte[], int>)NonLinqUpdate };
				yield return new object[] { provider, (Action<ITestDataContext, byte[], int>)NonLinqDelete };

				// More test candidates:
				// BatchInsert, Merge, Sql.ExpressionAttribute
			}
		}

#if !MONO
		[Issue256TestSource, Explicit("Demonstrates memory leak when fails")]
		public void SimpleTest(string context, Action<ITestDataContext, byte[], int> action)
		{
			Test(context, action, 1);
		}

		[Issue256TestSource, Explicit("Demonstrates memory leak when fails")]
		public void RetryTest(string context, Action<ITestDataContext, byte[], int> action)
		{
			Test(context, action, 3);
		}
#endif

		public void Test(string context, Action<ITestDataContext, byte[], int> testAction, int calls)
		{
			using (var db = GetDataContext(context))
			{
				db.Insert(new LinqDataTypes()
				{
					ID = 256,
					BinaryValue = new byte[] { 1, 2, 3 },
					DateTimeValue = Date,
					BoolValue = false,
					GuidValue = Guid.Empty,
					MoneyValue = 0,
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
			db.Insert(new LinqDataTypesWithPK() { ID = 10256, BinaryValue = value, DateTimeValue = Date });
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
				db.Update(new LinqDataTypesWithPK() { ID = 256, BinaryValue = null, DateTimeValue = Date });
				var result = query.ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(256, result[0].ID);
				Assert.IsNull(result[0].BinaryValue);

				db.Update(new LinqDataTypesWithPK() { ID = 256, BinaryValue = value, DateTimeValue = Date });
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
				db.Delete(new LinqDataTypesWithPK() { ID = 256, BinaryValue = value, DateTimeValue = Date });
				var result = db.Types.Where(_ => _.ID == 256).ToList();

				Assert.AreEqual(0, result.Count);

				calls--;
				db.Types.Insert(() => new LinqDataTypes() { ID = 256, BinaryValue = value });
			}
		}
	}
}
