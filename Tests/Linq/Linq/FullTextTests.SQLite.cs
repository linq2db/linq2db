using LinqToDB;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Mapping;
using NUnit.Framework;
using System.Data.Common;
using System.Linq;
using Tests.Model;

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

			ms.GetFluentMappingBuilder()
				.Entity<FtsTable>()
				.HasTableName(type.ToString() + "_TABLE")
				.HasColumn(t => t.text1)
				.HasColumn(t => t.text2);

			return ms;
		}
		#endregion

		#region MATCH
		[Test]
		public void MatchByTable([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] SQLiteFTS type)
		{
			using (var db = GetDataContext(context, SetupFtsMapping(type)))
			{
				var query = db.GetTable<FtsTable>().Where(r => Sql.Ext.SQLite().Match(r, "something"));

				if (type != SQLiteFTS.FTS5)
				{
					var results = query.ToList();
					Assert.AreEqual(1, results.Count);
					Assert.AreEqual("looking for something?", results[0].text1);
					Assert.AreEqual("found it!", results[0].text2);
				}
				else
				{
					var sql = query.ToString()!;
					Assert.That(sql.Contains("[r].[FTS5_TABLE] MATCH 'something'"));
				}
			}
		}

		[Test]
		public void MatchByTableSubQueryOptimizationTest([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using (var db = GetDataContext(context, SetupFtsMapping(type)))
			{
				var subquery = db.GetTable<FtsTable>().Where(r => Sql.Ext.SQLite().Match(r, "something"));
				var query = db.GetTable<FtsTable>().Where(r => subquery.Select(_ => Sql.Ext.SQLite().RowId(_)).Contains(Sql.Ext.SQLite().RowId(r)));

				var results = query.ToList();
				Assert.AreEqual(1, results.Count);
				Assert.AreEqual("looking for something?", results[0].text1);
				Assert.AreEqual("found it!", results[0].text2);
			}
		}

		[Test]
		public void MatchByColumnSubQueryOptimizationTest([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using (var db = GetDataContext(context, SetupFtsMapping(type)))
			{
				var subquery = db.GetTable<FtsTable>().Where(r => Sql.Ext.SQLite().Match(r.text1, "found"));
				var query = db.GetTable<FtsTable>().Where(r => subquery.Select(_ => Sql.Ext.SQLite().RowId(_)).Contains(Sql.Ext.SQLite().RowId(r)));

				var results = query.ToList();
				Assert.AreEqual(1, results.Count);
				Assert.AreEqual("record not found", results[0].text1);
				Assert.AreEqual("empty", results[0].text2);
			}
		}

		[Test]
		public void MatchByColumn([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] SQLiteFTS type)
		{
			using (var db = GetDataContext(context, SetupFtsMapping(type)))
			{
				var query = db.GetTable<FtsTable>().Where(r => Sql.Ext.SQLite().Match(r.text1, "found"));

				if (type != SQLiteFTS.FTS5)
				{
					var results = query.ToList();
					Assert.AreEqual(1, results.Count);
					Assert.AreEqual("record not found", results[0].text1);
					Assert.AreEqual("empty", results[0].text2);
				}
				else
				{
					var sql = query.ToString()!;
					Assert.That(sql.Contains("[r].[text1] MATCH 'found'"));
				}
			}
		}

		[Test]
		public void MatchFromTable([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context, SetupFtsMapping(SQLiteFTS.FTS5)))
			{
				var query = Sql.Ext.SQLite().MatchTable(db.GetTable<FtsTable>(), "found");

				var sql = query.ToString()!;
				Assert.That(sql.Contains("p_1 = 'found'"));
				Assert.That(sql.Contains("[FTS5_TABLE](@p_1)"));
			}
		}

		[Test]
		public void RowId([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] SQLiteFTS type)
		{
			using (var db = GetDataContext(context, SetupFtsMapping(type)))
			{
				var query = db.GetTable<FtsTable>().Where(r => Sql.Ext.SQLite().RowId(r) == 3);

				if (type != SQLiteFTS.FTS5)
				{
					var results = query.ToList();
					Assert.AreEqual(1, results.Count);
					Assert.AreEqual("record not found", results[0].text1);
					Assert.AreEqual("empty", results[0].text2);
				}
				else
				{
					var sql = query.ToString()!;
					Assert.That(sql.Contains("[r].[rowid] = 3"));
				}
			}
		}

		[Test]
		public void Rank([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context, SetupFtsMapping(SQLiteFTS.FTS5)))
			{
				var query = db.GetTable<FtsTable>().OrderBy(r => Sql.Ext.SQLite().Rank(r));

				var sql = query.ToString()!;
				Assert.That(sql.Contains("ORDER BY"));
				Assert.That(sql.Contains("[t1].[rank]"));
			}
		}

		[Test]
		public void Fts3Offsets([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using (var db = GetDataContext(context, SetupFtsMapping(type)))
			{
				var query = db.GetTable<FtsTable>()
					.Where(r => Sql.Ext.SQLite().Match(r, "found"))
					.OrderBy(r => Sql.Ext.SQLite().RowId(r))
					.Select(r => new { r.text1, offsets = Sql.Ext.SQLite().FTS3Offsets(r) });

				var results = query.ToList();
				Assert.AreEqual(2, results.Count);
				Assert.AreEqual("looking for something?", results[0].text1);
				Assert.AreEqual("1 0 0 5", results[0].offsets);
				Assert.AreEqual("record not found", results[1].text1);
				Assert.AreEqual("0 0 11 5", results[1].offsets);
			}
		}

		[Test]
		public void Fts3MatchInfo([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using (var db = GetDataContext(context, SetupFtsMapping(type)))
			{
				var query = db.GetTable<FtsTable>()
					.Where(r => Sql.Ext.SQLite().Match(r, "found"))
					.OrderBy(r => Sql.Ext.SQLite().RowId(r))
					.Select(r => new { r.text1, matchInfo = Sql.Ext.SQLite().FTS3MatchInfo(r) });

				var results = query.ToList();
				Assert.AreEqual(2, results.Count);
				Assert.AreEqual("looking for something?", results[0].text1);
				AreEqual(new byte[] { 1, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0 }, results[0].matchInfo);
				Assert.AreEqual("record not found", results[1].text1);
				AreEqual(new byte[] { 1, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0 }, results[1].matchInfo);
			}
		}

		[Test]
		public void Fts3MatchInfoWithFormat([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using (var db = GetDataContext(context, SetupFtsMapping(type)))
			{
				var query = db.GetTable<FtsTable>()
					.Where(r => Sql.Ext.SQLite().Match(r, "found"))
					.OrderBy(r => Sql.Ext.SQLite().RowId(r))
					.Select(r => new { r.text1, matchInfo = Sql.Ext.SQLite().FTS3MatchInfo(r, "pc") });

				var results = query.ToList();
				Assert.AreEqual(2, results.Count);
				Assert.AreEqual("looking for something?", results[0].text1);
				AreEqual(new byte[] { 1, 0, 0, 0, 2, 0, 0, 0 }, results[0].matchInfo);
				Assert.AreEqual("record not found", results[1].text1);
				AreEqual(new byte[] { 1, 0, 0, 0, 2, 0, 0, 0 }, results[1].matchInfo);
			}
		}

		[Test]
		public void Fts3Snippet1([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using (var db = GetDataContext(context, SetupFtsMapping(type)))
			{
				var result = db.GetTable<FtsTable>()
					.Where(r => Sql.Ext.SQLite().Match(r, "something"))
					.Select(r => Sql.Ext.SQLite().FTS3Snippet(r))
					.Single();

				Assert.AreEqual("looking for <b>something</b>?", result);
			}
		}

		[Test]
		public void Fts3Snippet2([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using (var db = GetDataContext(context, SetupFtsMapping(type)))
			{
				var result = db.GetTable<FtsTable>()
					.Where(r => Sql.Ext.SQLite().Match(r, "looking"))
					.Select(r => Sql.Ext.SQLite().FTS3Snippet(r, "_"))
					.Single();

				Assert.AreEqual("_looking</b> for something?", result);
			}
		}

		[Test]
		public void Fts3Snippet3([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using (var db = GetDataContext(context, SetupFtsMapping(type)))
			{
				var result = db.GetTable<FtsTable>()
					.Where(r => Sql.Ext.SQLite().Match(r, "looking"))
					.Select(r => Sql.Ext.SQLite().FTS3Snippet(r, "->", "<-"))
					.Single();

				Assert.AreEqual("->looking<- for something?", result);
			}
		}

		[Test]
		public void Fts3Snippet4([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using (var db = GetDataContext(context, SetupFtsMapping(type)))
			{
				var result = db.GetTable<FtsTable>()
					.Where(r => Sql.Ext.SQLite().Match(r, "cool"))
					.Select(r => Sql.Ext.SQLite().FTS3Snippet(r, ">", "<", "[zzz]"))
					.Single();

				Assert.AreEqual("[zzz]3oC drops. >Cool< in the upper portion, minimum temperature 14-16oC and >cool< elsewhere, minimum[zzz]", result);
			}
		}

		[Test]
		public void Fts3Snippet5([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using (var db = GetDataContext(context, SetupFtsMapping(type)))
			{
				var result = db.GetTable<FtsTable>()
					.Where(r => Sql.Ext.SQLite().Match(r, "cool"))
					.Select(r => Sql.Ext.SQLite().FTS3Snippet(r, ">", "<", "[zzz]", 0))
					.Single();

				Assert.AreEqual("for snippet testing", result);
			}
		}

		[Test]
		public void Fts3Snippet6([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using (var db = GetDataContext(context, SetupFtsMapping(type)))
			{
				var result = db.GetTable<FtsTable>()
					.Where(r => Sql.Ext.SQLite().Match(r, "cool"))
					.Select(r => Sql.Ext.SQLite().FTS3Snippet(r, ">", "<", "[zzz]", 1, 1))
					.Single();

				Assert.AreEqual("[zzz]>Cool<[zzz]", result);
			}
		}

		[Test]
		public void Fts5bm25([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context, SetupFtsMapping(SQLiteFTS.FTS5)))
			{
				var query = db.GetTable<FtsTable>().Select(r => Sql.Ext.SQLite().FTS5bm25(r));

				var sql = query.ToString()!;
				Assert.That(sql.Contains("bm25([r].[FTS5_TABLE])"));
			}
		}

		[Test]
		public void Fts5bm25WithWeights([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context, SetupFtsMapping(SQLiteFTS.FTS5)))
			{
				var query = db.GetTable<FtsTable>().Select(r => Sql.Ext.SQLite().FTS5bm25(r, 1.4, 5.6));

				var sql = query.ToString()!;
				Assert.That(sql.Contains("bm25([r].[FTS5_TABLE], 1.3999999999999999, 5.5999999999999996)"));
			}
		}

		[Test]
		public void Fts5Highlight([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context, SetupFtsMapping(SQLiteFTS.FTS5)))
			{
				var query = db.GetTable<FtsTable>().Select(r => Sql.Ext.SQLite().FTS5Highlight(r, 2, "start", "end"));

				var sql = query.ToString()!;
				Assert.That(sql.Contains("highlight([r].[FTS5_TABLE], 2, 'start', 'end')"));
			}
		}

		[Test]
		public void Fts5Snippet([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context, SetupFtsMapping(SQLiteFTS.FTS5)))
			{
				var query = db.GetTable<FtsTable>().Select(r => Sql.Ext.SQLite().FTS5Snippet(r, 1, "->", "<-", "zzz", 4));

				var sql = query.ToString()!;
				Assert.That(sql.Contains("snippet([r].[FTS5_TABLE], 1, '->', '<-', 'zzz', 4)"));
			}
		}

		[Test]
		public void Fts3CommandOptimize([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using (var db = new TestDataConnection(context))
			{
				db.AddMappingSchema(SetupFtsMapping(type));

				db.FTS3Optimize(db.GetTable<FtsTable>());

				var tableName = type.ToString() + "_TABLE";

				Assert.AreEqual($"INSERT INTO [{tableName}]([{tableName}]) VALUES('optimize')", db.LastQuery);
			}
		}

		[Test]
		public void Fts3CommandRebuild([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using (var db = new TestDataConnection(context))
			{
				db.AddMappingSchema(SetupFtsMapping(type));

				db.FTS3Rebuild(db.GetTable<FtsTable>());

				var tableName = type.ToString() + "_TABLE";

				Assert.AreEqual($"INSERT INTO [{tableName}]([{tableName}]) VALUES('rebuild')", db.LastQuery);
			}
		}

		[Test]
		public void Fts3CommandIntegrityCheck([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using (var db = new TestDataConnection(context))
			{
				db.AddMappingSchema(SetupFtsMapping(type));

				db.FTS3IntegrityCheck(db.GetTable<FtsTable>());

				var tableName = type.ToString() + "_TABLE";

				Assert.AreEqual($"INSERT INTO [{tableName}]([{tableName}]) VALUES('integrity-check')", db.LastQuery);
			}
		}

		[Test]
		public void Fts3CommandMerge([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using (var db = new TestDataConnection(context))
			{
				db.AddMappingSchema(SetupFtsMapping(type));

				db.FTS3Merge(db.GetTable<FtsTable>(), 4, 3);

				var tableName = type.ToString() + "_TABLE";

				Assert.AreEqual($"INSERT INTO [{tableName}]([{tableName}]) VALUES('merge=4,3')", db.LastQuery);
			}
		}

		[Test]
		public void Fts3CommandAutoMerge([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values(SQLiteFTS.FTS3, SQLiteFTS.FTS4)] SQLiteFTS type)
		{
			using (var db = new TestDataConnection(context))
			{
				db.AddMappingSchema(SetupFtsMapping(type));

				db.FTS3AutoMerge(db.GetTable<FtsTable>(), 5);

				var tableName = type.ToString() + "_TABLE";

				Assert.AreEqual($"INSERT INTO [{tableName}]([{tableName}]) VALUES('automerge=5')", db.LastQuery);
			}
		}

		[Test]
		public void Fts5CommandAutoMerge([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
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
					Assert.AreEqual("INSERT INTO [FTS5_TABLE]([FTS5_TABLE], rank) VALUES('automerge', 5)", db.LastQuery);
				}
			}
		}

		[Test]
		public void Fts5CommandCrisisMerge([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
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
					Assert.AreEqual("INSERT INTO [FTS5_TABLE]([FTS5_TABLE], rank) VALUES('crisismerge', 2)", db.LastQuery);
				}
			}
		}

		[Test]
		public void Fts5CommandDelete([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				DbParameter[]? parameters = null!;
				db.OnCommandInitialized += args =>
				{
					parameters = args.Command.Parameters.Cast<DbParameter>().ToArray();
				};

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
					Assert.AreEqual("INSERT INTO [FTS5_TABLE]([FTS5_TABLE], rowid, [text1], [text2]) VALUES('delete', 2, @p0, @p1)", db.LastQuery);

					Assert.AreEqual(2, parameters.Length);
					Assert.True(parameters.Any(p => p.Value!.Equals("one")));
					Assert.True(parameters.Any(p => p.Value!.Equals("two")));
				}
			}
		}

		[Test]
		public void Fts5CommandDeleteAll([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
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
					Assert.AreEqual("INSERT INTO [FTS5_TABLE]([FTS5_TABLE]) VALUES('delete-all')", db.LastQuery);
				}
			}
		}

		[Test]
		public void Fts5CommandIntegrityCheck([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
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
					Assert.AreEqual("INSERT INTO [FTS5_TABLE]([FTS5_TABLE]) VALUES('integrity-check')", db.LastQuery);
				}
			}
		}

		[Test]
		public void Fts5CommandMerge([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
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
					Assert.AreEqual("INSERT INTO [FTS5_TABLE]([FTS5_TABLE], rank) VALUES('merge', 234)", db.LastQuery);
				}
			}
		}

		[Test]
		public void Fts5CommandOptimize([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
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
					Assert.AreEqual("INSERT INTO [FTS5_TABLE]([FTS5_TABLE]) VALUES('optimize')", db.LastQuery);
				}
			}
		}

		[Test]
		public void Fts5CommandPgsz([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
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
					Assert.AreEqual("INSERT INTO [FTS5_TABLE]([FTS5_TABLE], rank) VALUES('pgsz', 3333)", db.LastQuery);
				}
			}
		}

		[Test]
		public void Fts5CommandRank([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				DbParameter[]? parameters = null!;
				db.OnCommandInitialized += args =>
				{
					parameters = args.Command.Parameters.Cast<DbParameter>().ToArray();
				};

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
					Assert.AreEqual("INSERT INTO [FTS5_TABLE]([FTS5_TABLE], rank) VALUES('rank', @rank)", db.LastQuery);

					Assert.AreEqual(1, parameters.Length);
					Assert.AreEqual("strange('function\")", parameters[0].Value);
				}
			}
		}

		[Test]
		public void Fts5CommandRebuild([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
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
					Assert.AreEqual("INSERT INTO [FTS5_TABLE]([FTS5_TABLE]) VALUES('rebuild')", db.LastQuery);
				}
			}
		}

		[Test]
		public void Fts5CommandUserMerge([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
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
					Assert.AreEqual("INSERT INTO [FTS5_TABLE]([FTS5_TABLE], rank) VALUES('usermerge', 7)", db.LastQuery);
				}
			}
		}
		#endregion

		#region FTS shadow tables
		[Table]
		class FTS3_TABLE_segdir
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
			using (var db = new TestDataConnection(context))
			{
				db.GetTable<FTS3_TABLE_segdir>().ToList();
			}
		}
		#endregion
	}

}
