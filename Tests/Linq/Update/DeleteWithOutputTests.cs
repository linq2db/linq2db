using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;

using NUnit.Framework;

namespace Tests.xUpdate
{
	[TestFixture]
	public class DeleteWithOutputTests : TestBase
	{
		private const string FeatureDeleteOutputMultipleWithExpressions = $"{TestProvName.AllSqlServer},{TestProvName.AllFirebird5Plus},{TestProvName.AllMariaDB},{TestProvName.AllPostgreSQL},{TestProvName.AllSQLite}";
		private const string FeatureDeleteOutputMultiple                = $"{TestProvName.AllSqlServer},{TestProvName.AllFirebird5Plus},{TestProvName.AllMariaDB},{TestProvName.AllPostgreSQL},{TestProvName.AllSQLite}";
		private const string FeatureDeleteOutputSingleWithExpressions   = $"{TestProvName.AllSqlServer},{TestProvName.AllFirebirdLess5},{TestProvName.AllMariaDB},{TestProvName.AllPostgreSQL},{TestProvName.AllSQLite}";
		private const string FeatureDeleteOutputSingle                  = $"{TestProvName.AllSqlServer},{TestProvName.AllFirebirdLess5},{TestProvName.AllMariaDB},{TestProvName.AllPostgreSQL},{TestProvName.AllSQLite}";
		private const string FeatureDeleteOutputInto                    = $"{TestProvName.AllSqlServer}";

		[Table]
		sealed class TableWithData
		{
			[PrimaryKey]          public int     Id       { get; set; }
			[Column]              public int     Value    { get; set; }
			[Column(Length = 50)] public string? ValueStr { get; set; }
		}

		[Table(Schema = "TestSchema")]
		sealed class TableWithDataAndSchema
		{
			[PrimaryKey]          public int     Id       { get; set; }
			[Column]              public int     Value    { get; set; }
			[Column(Length = 50)] public string? ValueStr { get; set; }
		}

		[Table]
		sealed class DestinationTable
		{
			[PrimaryKey]          public int     Id       { get; set; }
			[Column]              public int     Value    { get; set; }
			[Column(Length = 50)] public string? ValueStr { get; set; }
		}

		static TableWithData[] GetSourceData()
		{
			return Enumerable.Range(1, 10).Select(i =>
					new TableWithData { Id = i, Value = -i, ValueStr = "Str" + i.ToString() })
				.ToArray();
		}

		[Test]
		public void DeleteWithOutputTest([IncludeDataSources(true, FeatureDeleteOutputMultiple)] string context, [Values(100, 200)] int param)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = source
					.Where(s => s.Id > 3)
					.ToArray();

				var output = source
					.Where(s => s.Id > 3)
					.DeleteWithOutput()
					.ToArray();

				AreEqual(
					expected,
					output,
					ComparerBuilder.GetEqualityComparer<TableWithData>());
			}
		}

		[Test]
		public void DeleteWithOutputTestSingleRecord([IncludeDataSources(true, FeatureDeleteOutputSingle)] string context, [Values(100, 200)] int param)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = source
					.Where(s => s.Id == 3)
					.ToArray();

				var output = source
					.Where(s => s.Id == 3)
					.DeleteWithOutput()
					.ToArray();

				AreEqual(
					expected,
					output,
					ComparerBuilder.GetEqualityComparer<TableWithData>());
			}
		}

		[Test]
		public async Task DeleteWithOutputAsyncTest([IncludeDataSources(true, FeatureDeleteOutputMultiple)] string context, [Values(100, 200)] int param)
		{
			var sourceData = GetSourceData();

			await using var db     = GetDataContext(context);
			await using var source = db.CreateLocalTable(sourceData);

			var expected = source
				.Where(s => s.Id > 3)
				.ToList();

			var output = await AsyncEnumerableToListAsync(
				source
					.Where(s => s.Id > 3)
					.DeleteWithOutputAsync());

			AreEqual(
				expected,
				output,
				ComparerBuilder.GetEqualityComparer<TableWithData>());
		}

		[Test]
		public async Task DeleteWithOutputAsyncTestSingleRecord([IncludeDataSources(true, FeatureDeleteOutputSingle)] string context, [Values(100, 200)] int param)
		{
			var sourceData    = GetSourceData();
			using (var db = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = source
					.Where(s => s.Id == 3)
					.ToArray();

				var output = await AsyncEnumerableToListAsync(
					source
						.Where(s => s.Id == 3)
						.DeleteWithOutputAsync());

				AreEqual(
					expected,
					output,
					ComparerBuilder.GetEqualityComparer<TableWithData>());
			}
		}

		[Test]
		public void DeleteWithOutputProjectionFromQueryTest([IncludeDataSources(true, FeatureDeleteOutputMultipleWithExpressions)] string context, [Values(100, 200)] int param)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = source
					.Where(s => s.Id > 3)
					.ToArray();

				var output = source
					.Where(s => s.Id > 3)
					.DeleteWithOutput(
						deleted => new
						{
							Id       = Sql.AsSql(deleted.Id       + 1),
							ValueStr = Sql.AsSql(deleted.ValueStr + 1),
						})
					.ToArray();

				AreEqual(
					expected
						.Select(t => new
						{
							Id       = t.Id       + 1,
							ValueStr = t.ValueStr + 1,
						}),
					output);
			}
		}

		[Test]
		public void DeleteWithOutputProjectionFromQueryTestSingleRecord([IncludeDataSources(true, FeatureDeleteOutputSingleWithExpressions)] string context, [Values(100, 200)] int param)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = source
					.Where(s => s.Id == 3)
					.ToArray();

				var output = source
					.Where(s => s.Id == 3)
					.DeleteWithOutput(
						deleted => new
						{
							Id       = Sql.AsSql(deleted.Id       + 1),
							ValueStr = Sql.AsSql(deleted.ValueStr + 1),
							Bool = deleted.ValueStr != null
						})
					.ToArray();

				AreEqual(
					expected
						.Select(t => new
						{
							Id       = t.Id       + 1,
							ValueStr = t.ValueStr + 1,
							Bool     = t.ValueStr != null
						}),
					output);
			}
		}

		[Test]
		public async Task DeleteWithOutputProjectionFromQueryAsyncTest([IncludeDataSources(true, FeatureDeleteOutputMultipleWithExpressions)] string context, [Values(100, 200)] int param)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = source
					.Where(s => s.Id > 3)
					.ToArray();

				var output = await AsyncEnumerableToListAsync(
					source
						.Where(s => s.Id > 3)
						.DeleteWithOutputAsync(
							deleted => new
							{
								Id       = Sql.AsSql(deleted.Id       + 1),
								ValueStr = Sql.AsSql(deleted.ValueStr + 1),
							}));

				AreEqual(
					expected
						.Select(t => new
						{
							Id       = t.Id       + 1,
							ValueStr = t.ValueStr + 1,
						}),
					output);
			}
		}

		[Test]
		public async Task DeleteWithOutputProjectionFromQueryAsyncTestSingleRecord([IncludeDataSources(true, FeatureDeleteOutputSingleWithExpressions)] string context, [Values(100, 200)] int param)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = source
					.Where(s => s.Id == 3)
					.ToArray();

				var output = await AsyncEnumerableToListAsync(
					source
						.Where(s => s.Id == 3)
						.DeleteWithOutputAsync(
							deleted => new
							{
								Id       = Sql.AsSql(deleted.Id       + 1),
								ValueStr = Sql.AsSql(deleted.ValueStr + 1),
							}));

				AreEqual(
					expected
						.Select(t => new
						{
							Id       = t.Id       + 1,
							ValueStr = t.ValueStr + 1,
						}),
					output);
			}
		}

		[Test]
		public void DeleteWithOutputFromQueryTest([IncludeDataSources(true, FeatureDeleteOutputMultipleWithExpressions)] string context, [Values(100, 200)] int param)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = source
					.Where(s => s.Id > 3)
					.ToArray();

				var output = source
					.Where(s => s.Id > 3)
					.DeleteWithOutput(
						s => new DestinationTable
						{
							Id       = s.Id       + param,
							Value    = s.Value    + param,
							ValueStr = s.ValueStr + param
						})
					.ToArray();

				AreEqual(
					expected
						.Select(s => new DestinationTable
						{
							Id       = s.Id       + param,
							Value    = s.Value    + param,
							ValueStr = s.ValueStr + param,
						}),
					output,
					ComparerBuilder.GetEqualityComparer<DestinationTable>());
			}
		}

		[Test]
		public void DeleteWithOutputFromQueryTestSingleRecord([IncludeDataSources(true, FeatureDeleteOutputSingleWithExpressions)] string context, [Values(100, 200)] int param)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = source
					.Where(s => s.Id == 3)
					.ToArray();

				var output = source
					.Where(s => s.Id == 3)
					.DeleteWithOutput(
						s => new DestinationTable
						{
							Id       = s.Id       + param,
							Value    = s.Value    + param,
							ValueStr = s.ValueStr + param
						})
					.ToArray();

				AreEqual(
					expected
						.Select(s => new DestinationTable
						{
							Id       = s.Id       + param,
							Value    = s.Value    + param,
							ValueStr = s.ValueStr + param,
						}),
					output,
					ComparerBuilder.GetEqualityComparer<DestinationTable>());
			}
		}

		[Test]
		public async Task DeleteWithOutputFromQueryAsyncTest([IncludeDataSources(true, FeatureDeleteOutputMultipleWithExpressions)] string context, [Values(100, 200)] int param)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = source
					.Where(s => s.Id > 3)
					.ToArray();

				var output = await AsyncEnumerableToListAsync(
					source
						.Where(s => s.Id > 3)
						.DeleteWithOutputAsync(
							s => new DestinationTable
							{
								Id       = s.Id       + param,
								Value    = s.Value    + param,
								ValueStr = s.ValueStr + param
							}));

				AreEqual(
					expected
						.Select(s => new DestinationTable
						{
							Id       = s.Id       + param,
							Value    = s.Value    + param,
							ValueStr = s.ValueStr + param,
						}),
					output,
					ComparerBuilder.GetEqualityComparer<DestinationTable>());
			}
		}

		[Test]
		public async Task DeleteWithOutputFromQueryAsyncTestSingleRecord([IncludeDataSources(true, FeatureDeleteOutputSingleWithExpressions)] string context, [Values(100, 200)] int param)
		{
			var sourceData    = GetSourceData();
			using (var db = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = source
					.Where(s => s.Id == 3)
					.ToArray();

				var output = await AsyncEnumerableToListAsync(
					source
						.Where(s => s.Id == 3)
						.DeleteWithOutputAsync(
							s => new DestinationTable
							{
								Id       = s.Id       + param,
								Value    = s.Value    + param,
								ValueStr = s.ValueStr + param
							}));

				AreEqual(
					expected
						.Select(s => new DestinationTable
						{
							Id       = s.Id + param,
							Value    = s.Value + param,
							ValueStr = s.ValueStr + param,
						}),
					output,
					ComparerBuilder.GetEqualityComparer<DestinationTable>());
			}
		}

		[Test]
		public void DeleteWithOutputIntoFromQueryTest([IncludeDataSources(true, FeatureDeleteOutputInto)] string context, [Values(100, 200)] int param)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable<DestinationTable>())
			{
				var expected = source
					.Where(s => s.Id > 3)
					.ToArray();

				var output = source
					.Where(s => s.Id > 3)
					.DeleteWithOutputInto(
						target,
						s => new DestinationTable
						{
							Id       = s.Id       + param,
							Value    = s.Value    + param,
							ValueStr = s.ValueStr + param
						});

				AreEqual(
					expected
						.Select(s => new DestinationTable
						{
							Id       = s.Id       + param,
							Value    = s.Value    + param,
							ValueStr = s.ValueStr + param,
						}),
					target.ToArray(),
					ComparerBuilder.GetEqualityComparer<DestinationTable>());
			}
		}

		[Test]
		public async Task DeleteWithOutputIntoFromQueryAsyncTest([IncludeDataSources(true, FeatureDeleteOutputInto)] string context, [Values(100, 200)] int param)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable<DestinationTable>())
			{
				var expected = source
					.Where(s => s.Id > 3)
					.ToArray();

				var output = await source
					.Where(s => s.Id > 3)
					.DeleteWithOutputIntoAsync(
						target,
						s => new DestinationTable
						{
							Id       = s.Id       + param,
							Value    = s.Value    + param,
							ValueStr = s.ValueStr + param
						});

				AreEqual(
					expected
						.Select(s => new DestinationTable
						{
							Id       = s.Id       + param,
							Value    = s.Value    + param,
							ValueStr = s.ValueStr + param,
						}),
					target.ToArray(),
					ComparerBuilder.GetEqualityComparer<DestinationTable>());
			}
		}

		[Test]
		public void DeleteWithOutputIntoTempTable([IncludeDataSources(FeatureDeleteOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable(sourceData);
			using var target = db.CreateLocalTable<DestinationTable>(tableOptions: TableOptions.IsTemporary);
			var expected = source
				.Where(s => s.Id > 3)
				.ToArray();

			var param = 100500;
			var output = source
				.Where(s => s.Id > 3)
				.DeleteWithOutputInto(
					target,
					s => new DestinationTable()
					{
						Id       = s.Id       + param,
						Value    = s.Value    + param,
						ValueStr = s.ValueStr + param
					});

			AreEqual(
				expected
					.Select(s => new DestinationTable()
					{
						Id       = s.Id       + param,
						Value    = s.Value    + param,
						ValueStr = s.ValueStr + param,
					}),
				target.ToArray(),
				ComparerBuilder.GetEqualityComparer<DestinationTable>());
		}

		[Test]
		public void DeleteWithOutputIntoTempTableProjByTableName([IncludeDataSources(FeatureDeleteOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable("TableWithData_source", sourceData);
			using var target = db.CreateTempTable<DestinationTable>("DestinationTable_target");
			var targetRef = db.GetTable<DestinationTable>()
				.TableOptions(TableOptions.IsTemporary)
				.TableName(target.TableName);

			var expected = source
				.Where(s => s.Id > 3)
				.ToArray();

			var param = 100500;
			var output = source
				.Where(s => s.Id > 3)
				.DeleteWithOutputInto(
					targetRef,
					s => new DestinationTable()
					{
						Id       = s.Id       + param,
						Value    = s.Value    + param,
						ValueStr = s.ValueStr + param
					});

			AreEqual(
				expected
					.Select(s => new DestinationTable()
					{
						Id       = s.Id       + param,
						Value    = s.Value    + param,
						ValueStr = s.ValueStr + param,
					}),
				target.ToArray(),
				ComparerBuilder.GetEqualityComparer<DestinationTable>());
		}

		[Test]
		public async Task DeleteWithOutputIntoTempTableProjByTableNameAsync([IncludeDataSources(FeatureDeleteOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable("TableWithData_source", sourceData);
			using var target = db.CreateTempTable<DestinationTable>("DestinationTable_target");
			var targetRef = db.GetTable<DestinationTable>()
				.TableOptions(TableOptions.IsTemporary)
				.TableName(target.TableName);

			var expected = source
				.Where(s => s.Id > 3)
				.ToArray();

			var param = 100500;
			var output = await source
				.Where(s => s.Id > 3)
				.DeleteWithOutputIntoAsync(
					targetRef,
					s => new DestinationTable()
					{
						Id       = s.Id       + param,
						Value    = s.Value    + param,
						ValueStr = s.ValueStr + param
					});

			AreEqual(
				expected
					.Select(s => new DestinationTable()
					{
						Id       = s.Id       + param,
						Value    = s.Value    + param,
						ValueStr = s.ValueStr + param,
					}),
				target.ToArray(),
				ComparerBuilder.GetEqualityComparer<DestinationTable>());
		}

		[Test]
		public void DeleteWithOutputIntoTempTableByTableName([IncludeDataSources(FeatureDeleteOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable("TableWithData_source", sourceData);
			using var target = db.CreateTempTable<TableWithData>("TableWithData_target");
			var targetRef = db.GetTable<TableWithData>()
				.TableOptions(TableOptions.IsTemporary)
				.TableName(target.TableName);

			var expected = source
				.Where(s => s.Id > 3)
				.ToArray();

			var output = source
				.Where(s => s.Id > 3)
				.DeleteWithOutputInto(
					targetRef);

			AreEqual(
				expected,
				target.ToArray(),
				ComparerBuilder.GetEqualityComparer<TableWithData>());
		}

		[Test]
		public async Task DeleteWithOutputIntoTempTableByTableNameAsync([IncludeDataSources(FeatureDeleteOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable("TableWithData_source", sourceData);
			using var target = db.CreateTempTable<TableWithData>("TableWithData_target");
			var targetRef = db.GetTable<TableWithData>()
				.TableOptions(TableOptions.IsTemporary)
				.TableName(target.TableName);

			var expected = source
				.Where(s => s.Id > 3)
				.ToArray();

			var output = await source
				.Where(s => s.Id > 3)
				.DeleteWithOutputIntoAsync(
					targetRef);

			AreEqual(
				expected,
				target.ToArray(),
				ComparerBuilder.GetEqualityComparer<TableWithData>());
		}
	}
}
