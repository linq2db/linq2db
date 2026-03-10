using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	[Category(TestCategory.FTS)]
	public partial class FullTextTests : TestBase
	{
		// TODO: FTS5 tests not executed against database due to missing support in used providers

		#region Mappings
		public class FtsTable
		{
			public string? text1 { get; set; }

			public string? text2 { get; set; }
		}

		public enum SQLiteFTS
		{
			FTS3,
			FTS4,
			FTS5
		}

		private MappingSchema SetupFtsMapping(SQLiteFTS type)
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<FtsTable>()
				.HasTableName(type.ToString() + "_TABLE")
				.HasColumn(t => t.text1)
				.HasColumn(t => t.text2)
				.Build();

			return ms;
		}
		#endregion

		#region MATCH
		[Test]
		public void MatchByTable([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] SQLiteFTS type)
		{
			using var db = GetDataContext(context, SetupFtsMapping(type));
			var query = db.GetTable<FtsTable>().Where(r => Sql.Ext.SQLite().Match(r, "something"));

			if (type != SQLiteFTS.FTS5)
			{
				var results = query.ToList();
				Assert.That(results, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(results[0].text1, Is.EqualTo("looking for something?"));
					Assert.That(results[0].text2, Is.EqualTo("found it!"));
				}
			}
			else
			{
				// FTS5 required
				//query.ToArray();

				var sql = query.ToSqlQuery().Sql;

				BaselinesManager.LogQuery(sql);

				Assert.That(sql, Does.Contain("[r].[FTS5_TABLE] MATCH 'something'"));
			}
		}

		[Test]
		public void MatchByTableSubQueryOptimizationTest([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using var db = GetDataContext(context, SetupFtsMapping(type));
			var subquery = db.GetTable<FtsTable>()
					.Where(r => Sql.Ext.SQLite().Match(r, "something"));

			var query = db.GetTable<FtsTable>()
					.Where(r => subquery.Select(_ => Sql.Ext.SQLite().RowId(_)).Contains(Sql.Ext.SQLite().RowId(r)));

			var results = query.ToList();
			Assert.That(results, Has.Count.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(results[0].text1, Is.EqualTo("looking for something?"));
				Assert.That(results[0].text2, Is.EqualTo("found it!"));
			}
		}

		[Test]
		public void MatchByColumnSubQueryOptimizationTest([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using var db = GetDataContext(context, SetupFtsMapping(type));
			var subquery = db.GetTable<FtsTable>().Where(r => Sql.Ext.SQLite().Match(r.text1, "found"));
			var query = db.GetTable<FtsTable>().Where(r => subquery.Select(_ => Sql.Ext.SQLite().RowId(_)).Contains(Sql.Ext.SQLite().RowId(r)));

			var results = query.ToList();
			Assert.That(results, Has.Count.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(results[0].text1, Is.EqualTo("record not found"));
				Assert.That(results[0].text2, Is.EqualTo("empty"));
			}
		}

		[Test]
		public void MatchByColumn([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] SQLiteFTS type)
		{
			using var db = GetDataContext(context, SetupFtsMapping(type));
			var query = db.GetTable<FtsTable>().Where(r => Sql.Ext.SQLite().Match(r.text1, "found"));

			if (type != SQLiteFTS.FTS5)
			{
				var results = query.ToList();
				Assert.That(results, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(results[0].text1, Is.EqualTo("record not found"));
					Assert.That(results[0].text2, Is.EqualTo("empty"));
				}
			}
			else
			{
				// FTS5 required
				//query.ToArray();

				var sql = query.ToSqlQuery().Sql;
				Assert.That(sql, Does.Contain("[r].[text1] MATCH 'found'"));
			}
		}

		[Test]
		public void MatchFromTable([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context, SetupFtsMapping(SQLiteFTS.FTS5));
			var query = Sql.Ext.SQLite().MatchTable(db.GetTable<FtsTable>(), "found");

			// FTS5 required
			//query.ToArray();

			var command = query.ToSqlQuery();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(command.Sql, Does.Contain("[FTS5_TABLE](@"));
				Assert.That(command.Parameters, Has.Count.EqualTo(1));
			}

			Assert.That(command.Parameters[0].Value, Is.EqualTo("found"));
		}

		[Test]
		public void RowId([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] SQLiteFTS type)
		{
			using var db = GetDataContext(context, SetupFtsMapping(type));
			var query = db.GetTable<FtsTable>().Where(r => Sql.Ext.SQLite().RowId(r) == 3);

			if (type != SQLiteFTS.FTS5)
			{
				var results = query.ToList();
				Assert.That(results, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(results[0].text1, Is.EqualTo("record not found"));
					Assert.That(results[0].text2, Is.EqualTo("empty"));
				}
			}
			else
			{
				// FTS5 required
				//query.ToArray();

				var sql = query.ToSqlQuery().Sql;
				Assert.That(sql, Does.Contain("[r].[rowid] = 3"));
			}
		}

		[Test]
		public void Rank([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context, SetupFtsMapping(SQLiteFTS.FTS5));
			var query = db.GetTable<FtsTable>().OrderBy(r => Sql.Ext.SQLite().Rank(r));

			// FTS5 required
			//query.ToArray();

			var sql = query.ToSqlQuery().Sql;
			Assert.That(sql, Does.Contain("ORDER BY"));
			Assert.That(sql, Does.Contain("[t1].[rank]"));
		}

		[Test]
		public void Fts3Offsets([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using var db = GetDataContext(context, SetupFtsMapping(type));
			var query = db.GetTable<FtsTable>()
					.Where(r => Sql.Ext.SQLite().Match(r, "found"))
					.OrderBy(r => Sql.Ext.SQLite().RowId(r))
					.Select(r => new { r.text1, offsets = Sql.Ext.SQLite().FTS3Offsets(r) });

			var results = query.ToList();
			Assert.That(results, Has.Count.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(results[0].text1, Is.EqualTo("looking for something?"));
				Assert.That(results[0].offsets, Is.EqualTo("1 0 0 5"));
				Assert.That(results[1].text1, Is.EqualTo("record not found"));
				Assert.That(results[1].offsets, Is.EqualTo("0 0 11 5"));
			}
		}

		[Test]
		public void Fts3MatchInfo([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using var db = GetDataContext(context, SetupFtsMapping(type));
			var query = db.GetTable<FtsTable>()
					.Where(r => Sql.Ext.SQLite().Match(r, "found"))
					.OrderBy(r => Sql.Ext.SQLite().RowId(r))
					.Select(r => new { r.text1, matchInfo = Sql.Ext.SQLite().FTS3MatchInfo(r) });

			var results = query.ToList();
			Assert.That(results, Has.Count.EqualTo(2));
			Assert.That(results[0].text1, Is.EqualTo("looking for something?"));
			AreEqual(new byte[] { 1, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0 }, results[0].matchInfo);
			Assert.That(results[1].text1, Is.EqualTo("record not found"));
			AreEqual(new byte[] { 1, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0 }, results[1].matchInfo);
		}

		[Test]
		public void Fts3MatchInfoWithFormat([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using var db = GetDataContext(context, SetupFtsMapping(type));
			var query = db.GetTable<FtsTable>()
					.Where(r => Sql.Ext.SQLite().Match(r, "found"))
					.OrderBy(r => Sql.Ext.SQLite().RowId(r))
					.Select(r => new { r.text1, matchInfo = Sql.Ext.SQLite().FTS3MatchInfo(r, "pc") });

			var results = query.ToList();
			Assert.That(results, Has.Count.EqualTo(2));
			Assert.That(results[0].text1, Is.EqualTo("looking for something?"));
			AreEqual(new byte[] { 1, 0, 0, 0, 2, 0, 0, 0 }, results[0].matchInfo);
			Assert.That(results[1].text1, Is.EqualTo("record not found"));
			AreEqual(new byte[] { 1, 0, 0, 0, 2, 0, 0, 0 }, results[1].matchInfo);
		}

		[Test]
		public void Fts3Snippet1([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using var db = GetDataContext(context, SetupFtsMapping(type));
			var result = db.GetTable<FtsTable>()
					.Where(r => Sql.Ext.SQLite().Match(r, "something"))
					.Select(r => Sql.Ext.SQLite().FTS3Snippet(r))
					.Single();

			Assert.That(result, Is.EqualTo("looking for <b>something</b>?"));
		}

		[Test]
		public void Fts3Snippet2([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using var db = GetDataContext(context, SetupFtsMapping(type));
			var result = db.GetTable<FtsTable>()
					.Where(r => Sql.Ext.SQLite().Match(r, "looking"))
					.Select(r => Sql.Ext.SQLite().FTS3Snippet(r, "_"))
					.Single();

			Assert.That(result, Is.EqualTo("_looking</b> for something?"));
		}

		[Test]
		public void Fts3Snippet3([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using var db = GetDataContext(context, SetupFtsMapping(type));
			var result = db.GetTable<FtsTable>()
					.Where(r => Sql.Ext.SQLite().Match(r, "looking"))
					.Select(r => Sql.Ext.SQLite().FTS3Snippet(r, "->", "<-"))
					.Single();

			Assert.That(result, Is.EqualTo("->looking<- for something?"));
		}

		[Test]
		public void Fts3Snippet4([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using var db = GetDataContext(context, SetupFtsMapping(type));
			var result = db.GetTable<FtsTable>()
					.Where(r => Sql.Ext.SQLite().Match(r, "cool"))
					.Select(r => Sql.Ext.SQLite().FTS3Snippet(r, ">", "<", "[zzz]"))
					.Single();

			Assert.That(result, Is.EqualTo("[zzz]3oC drops. >Cool< in the upper portion, minimum temperature 14-16oC and >cool< elsewhere, minimum[zzz]"));
		}

		[Test]
		public void Fts3Snippet5([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using var db = GetDataContext(context, SetupFtsMapping(type));
			var result = db.GetTable<FtsTable>()
					.Where(r => Sql.Ext.SQLite().Match(r, "cool"))
					.Select(r => Sql.Ext.SQLite().FTS3Snippet(r, ">", "<", "[zzz]", 0))
					.Single();

			Assert.That(result, Is.EqualTo("for snippet testing"));
		}

		[Test]
		public void Fts3Snippet6([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using var db = GetDataContext(context, SetupFtsMapping(type));
			var result = db.GetTable<FtsTable>()
					.Where(r => Sql.Ext.SQLite().Match(r, "cool"))
					.Select(r => Sql.Ext.SQLite().FTS3Snippet(r, ">", "<", "[zzz]", 1, 1))
					.Single();

			Assert.That(result, Is.EqualTo("[zzz]>Cool<[zzz]"));
		}

		[Test]
		public void Fts5bm25([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context, SetupFtsMapping(SQLiteFTS.FTS5));
			var query = db.GetTable<FtsTable>().Select(r => Sql.Ext.SQLite().FTS5bm25(r));

			// FTS5 required
			//query.ToArray();

			var sql = query.ToSqlQuery().Sql;
			Assert.That(sql, Does.Contain("bm25([r].[FTS5_TABLE])"));
		}

		[Test]
		public void Fts5bm25WithWeights([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context, SetupFtsMapping(SQLiteFTS.FTS5));
			var query = db.GetTable<FtsTable>().Select(r => Sql.Ext.SQLite().FTS5bm25(r, 1.4, 5.6));

			// FTS5 required
			//query.ToArray();

			var sql = query.ToSqlQuery().Sql;
			Assert.That(sql, Does.Contain("bm25([r].[FTS5_TABLE], 1.3999999999999999, 5.5999999999999996)"));
		}

		[Test]
		public void Fts5Highlight([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context, SetupFtsMapping(SQLiteFTS.FTS5));
			var query = db.GetTable<FtsTable>().Select(r => Sql.Ext.SQLite().FTS5Highlight(r, 2, "start", "end"));

			// FTS5 required
			//query.ToArray();

			var sql = query.ToSqlQuery().Sql;
			Assert.That(sql, Does.Contain("highlight([r].[FTS5_TABLE], 2, 'start', 'end')"));
		}

		[Test]
		public void Fts5Snippet([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context, SetupFtsMapping(SQLiteFTS.FTS5));
			var query = db.GetTable<FtsTable>().Select(r => Sql.Ext.SQLite().FTS5Snippet(r, 1, "->", "<-", "zzz", 4));

			// FTS5 required
			//query.ToArray();

			var sql = query.ToSqlQuery().Sql;
			Assert.That(sql, Does.Contain("snippet([r].[FTS5_TABLE], 1, '->', '<-', 'zzz', 4)"));
		}

		[Test]
		public void Fts3CommandOptimize([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(type));

			db.FTS3Optimize(db.GetTable<FtsTable>());

			var tableName = type.ToString() + "_TABLE";

			Assert.That(db.LastQuery, Is.EqualTo($"INSERT INTO [{tableName}]([{tableName}]) VALUES('optimize')"));
		}

		[Test]
		public async ValueTask Fts3CommandOptimizeAsync([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(type));

			await db.FTS3OptimizeAsync(db.GetTable<FtsTable>());

			var tableName = type.ToString() + "_TABLE";

			Assert.That(db.LastQuery, Is.EqualTo($"INSERT INTO [{tableName}]([{tableName}]) VALUES('optimize')"));
		}

		[Test]
		public void Fts3CommandRebuild([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(type));

			db.FTS3Rebuild(db.GetTable<FtsTable>());

			var tableName = type.ToString() + "_TABLE";

			Assert.That(db.LastQuery, Is.EqualTo($"INSERT INTO [{tableName}]([{tableName}]) VALUES('rebuild')"));
		}

		[Test]
		public async ValueTask Fts3CommandRebuildAsync([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(type));

			await db.FTS3RebuildAsync(db.GetTable<FtsTable>());

			var tableName = type.ToString() + "_TABLE";

			Assert.That(db.LastQuery, Is.EqualTo($"INSERT INTO [{tableName}]([{tableName}]) VALUES('rebuild')"));
		}

		[Test]
		public void Fts3CommandIntegrityCheck([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(type));

			db.FTS3IntegrityCheck(db.GetTable<FtsTable>());

			var tableName = type.ToString() + "_TABLE";

			Assert.That(db.LastQuery, Is.EqualTo($"INSERT INTO [{tableName}]([{tableName}]) VALUES('integrity-check')"));
		}

		[Test]
		public async ValueTask Fts3CommandIntegrityCheckAsync([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(type));

			await db.FTS3IntegrityCheckAsync(db.GetTable<FtsTable>());

			var tableName = type.ToString() + "_TABLE";

			Assert.That(db.LastQuery, Is.EqualTo($"INSERT INTO [{tableName}]([{tableName}]) VALUES('integrity-check')"));
		}

		[Test]
		public void Fts3CommandMerge([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(type));

			db.FTS3Merge(db.GetTable<FtsTable>(), 4, 3);

			var tableName = type.ToString() + "_TABLE";

			Assert.That(db.LastQuery, Is.EqualTo($"INSERT INTO [{tableName}]([{tableName}]) VALUES('merge=4,3')"));
		}

		[Test]
		public async ValueTask Fts3CommandMergeAsync([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(type));

			await db.FTS3MergeAsync(db.GetTable<FtsTable>(), 4, 3);

			var tableName = type.ToString() + "_TABLE";

			Assert.That(db.LastQuery, Is.EqualTo($"INSERT INTO [{tableName}]([{tableName}]) VALUES('merge=4,3')"));
		}

		[Test]
		public void Fts3CommandAutoMerge([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(type));

			db.FTS3AutoMerge(db.GetTable<FtsTable>(), 5);

			var tableName = type.ToString() + "_TABLE";

			Assert.That(db.LastQuery, Is.EqualTo($"INSERT INTO [{tableName}]([{tableName}]) VALUES('automerge=5')"));
		}

		[Test]
		public async ValueTask Fts3CommandAutoMergeAsync([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(type));

			await db.FTS3AutoMergeAsync(db.GetTable<FtsTable>(), 5);

			var tableName = type.ToString() + "_TABLE";

			Assert.That(db.LastQuery, Is.EqualTo($"INSERT INTO [{tableName}]([{tableName}]) VALUES('automerge=5')"));
		}

		[Test]
		public void Fts5CommandAutoMerge([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(SQLiteFTS.FTS5));

			try
			{
				db.FTS5AutoMerge(db.GetTable<FtsTable>(), 5);
			}
			catch
			{
				// we don't have FTS5 table, but we need to get sql for validation
			}
			finally
			{
				Assert.That(db.LastQuery, Is.EqualTo("INSERT INTO [FTS5_TABLE]([FTS5_TABLE], rank) VALUES('automerge', 5)"));
			}
		}

		[Test]
		public async ValueTask Fts5CommandAutoMergeAsync([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(SQLiteFTS.FTS5));

			try
			{
				await db.FTS5AutoMergeAsync(db.GetTable<FtsTable>(), 5);
			}
			catch
			{
				// we don't have FTS5 table, but we need to get sql for validation
			}
			finally
			{
				Assert.That(db.LastQuery, Is.EqualTo("INSERT INTO [FTS5_TABLE]([FTS5_TABLE], rank) VALUES('automerge', 5)"));
			}
		}

		[Test]
		public void Fts5CommandCrisisMerge([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(SQLiteFTS.FTS5));

			try
			{
				db.FTS5CrisisMerge(db.GetTable<FtsTable>(), 2);
			}
			catch
			{
				// we don't have FTS5 table, but we need to get sql for validation
			}
			finally
			{
				Assert.That(db.LastQuery, Is.EqualTo("INSERT INTO [FTS5_TABLE]([FTS5_TABLE], rank) VALUES('crisismerge', 2)"));
			}
		}

		[Test]
		public async ValueTask Fts5CommandCrisisMergeAsync([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(SQLiteFTS.FTS5));

			try
			{
				await db.FTS5CrisisMergeAsync(db.GetTable<FtsTable>(), 2);
			}
			catch
			{
				// we don't have FTS5 table, but we need to get sql for validation
			}
			finally
			{
				Assert.That(db.LastQuery, Is.EqualTo("INSERT INTO [FTS5_TABLE]([FTS5_TABLE], rank) VALUES('crisismerge', 2)"));
			}
		}

		[Test]
		public void Fts5CommandDelete([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			var commandInterceptor = new SaveCommandInterceptor();
			db.AddInterceptor(commandInterceptor);

			db.AddMappingSchema(SetupFtsMapping(SQLiteFTS.FTS5));

			try
			{
				var record = new FtsTable()
				{
					text1 = "one",
					text2 = "two"
				};

				db.FTS5Delete(db.GetTable<FtsTable>(), 2, record);
			}
			catch
			{
				// we don't have FTS5 table, but we need to get sql for validation
			}
			finally
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(db.LastQuery, Is.EqualTo("INSERT INTO [FTS5_TABLE]([FTS5_TABLE], rowid, [text1], [text2]) VALUES('delete', 2, @p0, @p1)"));

					Assert.That(commandInterceptor.Parameters, Has.Length.EqualTo(2));
					Assert.That(commandInterceptor.Parameters.Any(p => p.Value!.Equals("one")), Is.True);
					Assert.That(commandInterceptor.Parameters.Any(p => p.Value!.Equals("two")), Is.True);
				}
			}
		}

		[Test]
		public async ValueTask Fts5CommandDeleteAsync([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			var commandInterceptor = new SaveCommandInterceptor();
			db.AddInterceptor(commandInterceptor);

			db.AddMappingSchema(SetupFtsMapping(SQLiteFTS.FTS5));

			try
			{
				var record = new FtsTable()
				{
					text1 = "one",
					text2 = "two"
				};

				await db.FTS5DeleteAsync(db.GetTable<FtsTable>(), 2, record);
			}
			catch
			{
				// we don't have FTS5 table, but we need to get sql for validation
			}
			finally
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(db.LastQuery, Is.EqualTo("INSERT INTO [FTS5_TABLE]([FTS5_TABLE], rowid, [text1], [text2]) VALUES('delete', 2, @p0, @p1)"));

					Assert.That(commandInterceptor.Parameters, Has.Length.EqualTo(2));
					Assert.That(commandInterceptor.Parameters.Any(p => p.Value!.Equals("one")), Is.True);
					Assert.That(commandInterceptor.Parameters.Any(p => p.Value!.Equals("two")), Is.True);
				}
			}
		}

		[Test]
		public void Fts5CommandDeleteAll([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(SQLiteFTS.FTS5));

			try
			{
				db.FTS5DeleteAll(db.GetTable<FtsTable>());
			}
			catch
			{
				// we don't have FTS5 table, but we need to get sql for validation
			}
			finally
			{
				Assert.That(db.LastQuery, Is.EqualTo("INSERT INTO [FTS5_TABLE]([FTS5_TABLE]) VALUES('delete-all')"));
			}
		}

		[Test]
		public async ValueTask Fts5CommandDeleteAllAsync([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(SQLiteFTS.FTS5));

			try
			{
				await db.FTS5DeleteAllAsync(db.GetTable<FtsTable>());
			}
			catch
			{
				// we don't have FTS5 table, but we need to get sql for validation
			}
			finally
			{
				Assert.That(db.LastQuery, Is.EqualTo("INSERT INTO [FTS5_TABLE]([FTS5_TABLE]) VALUES('delete-all')"));
			}
		}

		[Test]
		public void Fts5CommandIntegrityCheck([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(SQLiteFTS.FTS5));

			try
			{
				db.FTS5IntegrityCheck(db.GetTable<FtsTable>());
			}
			catch
			{
				// we don't have FTS5 table, but we need to get sql for validation
			}
			finally
			{
				Assert.That(db.LastQuery, Is.EqualTo("INSERT INTO [FTS5_TABLE]([FTS5_TABLE]) VALUES('integrity-check')"));
			}
		}

		[Test]
		public async ValueTask Fts5CommandIntegrityCheckAsync([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(SQLiteFTS.FTS5));

			try
			{
				await db.FTS5IntegrityCheckAsync(db.GetTable<FtsTable>());
			}
			catch
			{
				// we don't have FTS5 table, but we need to get sql for validation
			}
			finally
			{
				Assert.That(db.LastQuery, Is.EqualTo("INSERT INTO [FTS5_TABLE]([FTS5_TABLE]) VALUES('integrity-check')"));
			}
		}

		[Test]
		public void Fts5CommandMerge([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(SQLiteFTS.FTS5));

			try
			{
				db.FTS5Merge(db.GetTable<FtsTable>(), 234);
			}
			catch
			{
				// we don't have FTS5 table, but we need to get sql for validation
			}
			finally
			{
				Assert.That(db.LastQuery, Is.EqualTo("INSERT INTO [FTS5_TABLE]([FTS5_TABLE], rank) VALUES('merge', 234)"));
			}
		}

		[Test]
		public async ValueTask Fts5CommandMergeAsync([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(SQLiteFTS.FTS5));

			try
			{
				await db.FTS5MergeAsync(db.GetTable<FtsTable>(), 234);
			}
			catch
			{
				// we don't have FTS5 table, but we need to get sql for validation
			}
			finally
			{
				Assert.That(db.LastQuery, Is.EqualTo("INSERT INTO [FTS5_TABLE]([FTS5_TABLE], rank) VALUES('merge', 234)"));
			}
		}

		[Test]
		public void Fts5CommandOptimize([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(SQLiteFTS.FTS5));

			try
			{
				db.FTS5Optimize(db.GetTable<FtsTable>());
			}
			catch
			{
				// we don't have FTS5 table, but we need to get sql for validation
			}
			finally
			{
				Assert.That(db.LastQuery, Is.EqualTo("INSERT INTO [FTS5_TABLE]([FTS5_TABLE]) VALUES('optimize')"));
			}
		}

		[Test]
		public async ValueTask Fts5CommandOptimizeAsync([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(SQLiteFTS.FTS5));

			try
			{
				await db.FTS5OptimizeAsync(db.GetTable<FtsTable>());
			}
			catch
			{
				// we don't have FTS5 table, but we need to get sql for validation
			}
			finally
			{
				Assert.That(db.LastQuery, Is.EqualTo("INSERT INTO [FTS5_TABLE]([FTS5_TABLE]) VALUES('optimize')"));
			}
		}

		[Test]
		public void Fts5CommandPgsz([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(SQLiteFTS.FTS5));

			try
			{
				db.FTS5Pgsz(db.GetTable<FtsTable>(), 3333);
			}
			catch
			{
				// we don't have FTS5 table, but we need to get sql for validation
			}
			finally
			{
				Assert.That(db.LastQuery, Is.EqualTo("INSERT INTO [FTS5_TABLE]([FTS5_TABLE], rank) VALUES('pgsz', 3333)"));
			}
		}

		[Test]
		public async ValueTask Fts5CommandPgszAsync([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(SQLiteFTS.FTS5));

			try
			{
				await db.FTS5PgszAsync(db.GetTable<FtsTable>(), 3333);
			}
			catch
			{
				// we don't have FTS5 table, but we need to get sql for validation
			}
			finally
			{
				Assert.That(db.LastQuery, Is.EqualTo("INSERT INTO [FTS5_TABLE]([FTS5_TABLE], rank) VALUES('pgsz', 3333)"));
			}
		}

		[Test]
		public void Fts5CommandRank([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			var commandInterceptor = new SaveCommandInterceptor();
			db.AddInterceptor(commandInterceptor);

			db.AddMappingSchema(SetupFtsMapping(SQLiteFTS.FTS5));

			try
			{
				db.FTS5Rank(db.GetTable<FtsTable>(), "strange('function\")");
			}
			catch
			{
				// we don't have FTS5 table, but we need to get sql for validation
			}
			finally
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(db.LastQuery, Is.EqualTo("INSERT INTO [FTS5_TABLE]([FTS5_TABLE], rank) VALUES('rank', @rank)"));

					Assert.That(commandInterceptor.Parameters, Has.Length.EqualTo(1));
				}

				Assert.That(commandInterceptor.Parameters[0].Value, Is.EqualTo("strange('function\")"));
			}
		}

		[Test]
		public async ValueTask Fts5CommandRankasync([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			var commandInterceptor = new SaveCommandInterceptor();
			db.AddInterceptor(commandInterceptor);

			db.AddMappingSchema(SetupFtsMapping(SQLiteFTS.FTS5));

			try
			{
				await db.FTS5RankAsync(db.GetTable<FtsTable>(), "strange('function\")");
			}
			catch
			{
				// we don't have FTS5 table, but we need to get sql for validation
			}
			finally
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(db.LastQuery, Is.EqualTo("INSERT INTO [FTS5_TABLE]([FTS5_TABLE], rank) VALUES('rank', @rank)"));

					Assert.That(commandInterceptor.Parameters, Has.Length.EqualTo(1));
				}

				Assert.That(commandInterceptor.Parameters[0].Value, Is.EqualTo("strange('function\")"));
			}
		}

		[Test]
		public void Fts5CommandRebuild([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(SQLiteFTS.FTS5));

			try
			{
				db.FTS5Rebuild(db.GetTable<FtsTable>());
			}
			catch
			{
				// we don't have FTS5 table, but we need to get sql for validation
			}
			finally
			{
				Assert.That(db.LastQuery, Is.EqualTo("INSERT INTO [FTS5_TABLE]([FTS5_TABLE]) VALUES('rebuild')"));
			}
		}

		[Test]
		public async ValueTask Fts5CommandRebuildAsync([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(SQLiteFTS.FTS5));

			try
			{
				await db.FTS5RebuildAsync(db.GetTable<FtsTable>());
			}
			catch
			{
				// we don't have FTS5 table, but we need to get sql for validation
			}
			finally
			{
				Assert.That(db.LastQuery, Is.EqualTo("INSERT INTO [FTS5_TABLE]([FTS5_TABLE]) VALUES('rebuild')"));
			}
		}

		[Test]
		public void Fts5CommandUserMerge([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(SQLiteFTS.FTS5));

			try
			{
				db.FTS5UserMerge(db.GetTable<FtsTable>(), 7);
			}
			catch
			{
				// we don't have FTS5 table, but we need to get sql for validation
			}
			finally
			{
				Assert.That(db.LastQuery, Is.EqualTo("INSERT INTO [FTS5_TABLE]([FTS5_TABLE], rank) VALUES('usermerge', 7)"));
			}
		}

		[Test]
		public async ValueTask Fts5CommandUserMergeAsync([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			db.AddMappingSchema(SetupFtsMapping(SQLiteFTS.FTS5));

			try
			{
				await db.FTS5UserMergeAsync(db.GetTable<FtsTable>(), 7);
			}
			catch
			{
				// we don't have FTS5 table, but we need to get sql for validation
			}
			finally
			{
				Assert.That(db.LastQuery, Is.EqualTo("INSERT INTO [FTS5_TABLE]([FTS5_TABLE], rank) VALUES('usermerge', 7)"));
			}
		}

		#endregion

		#region FTS shadow tables
		[Table]
		sealed class FTS3_TABLE_segdir
		{
			[Column] public long    level;
			[Column] public long    idx;
			[Column] public long?   start_block;
			[Column] public long?   leaves_end_block;
			//[Column] public long?   end_block;
			// from documentation:
			// This field may contain either an integer or a text field consisting of two integers separated by a space character
			[Column] public string?   end_block;
			[Column] public byte[]? root;
		}

		[ActiveIssue(Configuration = TestProvName.AllSQLiteClassic, Details = "Make hybrid fields work for classic provider too")]
		[Test]
		public void Fts3SegDirTableQuery([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			db.GetTable<FTS3_TABLE_segdir>().ToList();
		}
		#endregion
	}

}
