using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;
using NUnit.Framework;

namespace Tests.Extensions
{
	[TestFixture]
	public class SQLiteTests : TestBase
	{
		[Test]
		public void IndexedByTest([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Person.TableHint(SQLiteHints.Hint.IndexedBy("IX_PersonDesc"))
				where p.ID > 0
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[Person] [p] INDEXED BY IX_PersonDesc"));
		}

		[Test]
		public void IndexedByTest2([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Person.AsSQLite().IndexedByHint("IX_PersonDesc")
				where p.ID > 0
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[Person] [p] INDEXED BY IX_PersonDesc"));
		}

		[Test]
		public void NotIndexedTest([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Person.TableHint(SQLiteHints.Hint.NotIndexed)
				where p.ID > 0
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[Person] [p] NOT INDEXED"));
		}

		[Test]
		public void NotIndexedTest2([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Person.AsSQLite().NotIndexedHint()
				where p.ID > 0
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[Person] [p] NOT INDEXED"));
		}

		[Test]
		public void DeleteIndexedByTest([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			(
				from p in db.Person.TableHint(SQLiteHints.Hint.IndexedBy("IX_PersonDesc"))
				where p.ID > 1000000
				select p
			)
			.Delete();

			Assert.That(LastQuery, Contains.Substring("[Person] INDEXED BY IX_PersonDesc"));
		}

		[Test]
		public void UpdateIndexedByTest([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			(
				from p in db.Person.TableHint(SQLiteHints.Hint.IndexedBy("IX_PersonDesc"))
				where p.ID > 1000000
				select p
			)
			.Set(p => p.FirstName, "")
			.Update();

			Assert.That(LastQuery, Contains.Substring("[Person] INDEXED BY IX_PersonDesc"));
		}

		sealed class GuidMapping
		{
			[Column]
			public Guid? BlobGuid1 { get; set; }

			[Column(DataType = DataType.Binary)]
			public Guid? BlobGuid2 { get; set; }

			[Column(DataType = DataType.Guid)]
			public Guid? BlobGuid3 { get; set; }

			[Column(DbType = "UNIQUEIDENTIFIER")]
			public Guid? BlobGuid4 { get; set; }

			[Column(DbType = "TEXT")]
			public Guid? TextGuid1 { get; set; }

			[Column(DataType = DataType.VarChar)]
			public Guid? TextGuid2 { get; set; }

		}

		[Test]
		public void GuidMappingTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			var interceptor = new SaveCommandInterceptor();
			db.AddInterceptor(interceptor);

			using var table = db.CreateLocalTable<GuidMapping>();

			var data = new GuidMapping[]
			{
				new ()
				{
					BlobGuid1 = TestData.Guid1,
					BlobGuid2 = TestData.Guid1,
					BlobGuid3 = TestData.Guid1,
					BlobGuid4 = TestData.Guid1,
					TextGuid1 = TestData.Guid1,
					TextGuid2 = TestData.Guid1,
				}
			};

			var comparer = ComparerBuilder.GetEqualityComparer<GuidMapping>();

			// test literal passed with proper type
			db.InlineParameters = true;
			table.Insert(() => new() { BlobGuid1 = TestData.NonReadonlyGuid1 });
			TestWrite("BlobGuid1", "blob", true);
			table.Insert(() => new() { BlobGuid2 = TestData.NonReadonlyGuid1 });
			TestWrite("BlobGuid2", "blob", true);
			table.Insert(() => new() { BlobGuid3 = TestData.NonReadonlyGuid1 });
			TestWrite("BlobGuid3", "blob", true);
			table.Insert(() => new() { BlobGuid4 = TestData.NonReadonlyGuid1 });
			TestWrite("BlobGuid4", "blob", true);
			table.Insert(() => new() { TextGuid1 = TestData.NonReadonlyGuid1 });
			TestWrite("TextGuid1", "text", true);
			table.Insert(() => new() { TextGuid2 = TestData.NonReadonlyGuid1 });
			TestWrite("TextGuid2", "text", true);

			// test parameter passed with proper type
			db.InlineParameters = false;
			table.Insert(() => new() { BlobGuid1 = TestData.NonReadonlyGuid1 });
			TestWrite("BlobGuid1", "blob", false);
			table.Insert(() => new() { BlobGuid2 = TestData.NonReadonlyGuid1 });
			TestWrite("BlobGuid2", "blob", false);
			table.Insert(() => new() { BlobGuid3 = TestData.NonReadonlyGuid1 });
			TestWrite("BlobGuid3", "blob", false);
			table.Insert(() => new() { BlobGuid4 = TestData.NonReadonlyGuid1 });
			TestWrite("BlobGuid4", "blob", false);
			table.Insert(() => new() { TextGuid1 = TestData.NonReadonlyGuid1 });
			TestWrite("TextGuid1", "text", false);
			table.Insert(() => new() { TextGuid2 = TestData.NonReadonlyGuid1 });
			TestWrite("TextGuid2", "text", false);

			// test bulk copy roundtrip
			table.BulkCopy(data);
			var result = table.ToArray();
			AreEqual(data, result, comparer);
			table.Delete();

			// test insert literals roundtrip
			db.InlineParameters = true;
			db.Insert(data[0]);
			Assert.That(interceptor.Parameters, Is.Empty);
			result = table.ToArray();
			AreEqual(data, result, comparer);
			table.Delete();

			// test insert parameters roundtrip
			db.InlineParameters = false;
			db.Insert(data[0]);
			Assert.That(interceptor.Parameters, Has.Length.EqualTo(6));
			result = table.ToArray();
			AreEqual(data, result, comparer);
			table.Delete();

			// test mixed values read
			var value = $"'{TestData.Guid1.ToString().ToUpperInvariant()}'";
			db.Execute($"INSERT INTO GuidMapping(BlobGuid1, BlobGuid2, BlobGuid3, BlobGuid4, TextGuid1, TextGuid2) VALUES({value}, {value}, {value}, {value}, {value}, {value})");
			value = $"x'{string.Join(string.Empty, TestData.Guid1.ToByteArray().Select(x => $"{x:X2}"))}'";
			db.Execute($"INSERT INTO GuidMapping(BlobGuid1, BlobGuid2, BlobGuid3, BlobGuid4, TextGuid1, TextGuid2) VALUES({value}, {value}, {value}, {value}, {value}, {value})");

			result = table.ToArray();
			AreEqual(data, new[] { result[0] }, comparer);
			AreEqual(data, new[] { result[1] }, comparer);

			void TestWrite(string FieldName, string expectedType, bool inline)
			{
				Assert.That(interceptor.Parameters, Has.Length.EqualTo(inline ? 0 : 1));
				var type = db.Execute<string>($"SELECT typeof({FieldName}) FROM GuidMapping");
				Assert.That(type, Is.EqualTo(expectedType));

				// assert literal is uppercased (M.D.SQLite format)
				if (expectedType == "text")
					Assert.That(db.Execute<string>($"SELECT {FieldName} FROM GuidMapping"), Is.EqualTo(TestData.Guid1.ToString().ToUpperInvariant()));

				table.Delete();
			}
		}
	}
}
