using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Internal.Async;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;

using NUnit.Framework;

namespace Tests.xUpdate
{
	[TestFixture]
	public class UpdateWithOutputTests : TestBase
	{
		private const string FeatureUpdateOutputWithOldSingle                      = $"{TestProvName.AllSqlServer},{TestProvName.AllFirebirdLess5}";
		private const string FeatureUpdateOutputWithOldSingleNoAlternateRewrite    = $"{TestProvName.AllSqlServer}";
		private const string FeatureUpdateOutputWithOldMultiple                    = $"{TestProvName.AllSqlServer},{TestProvName.AllFirebird5Plus}";
		private const string FeatureUpdateOutputWithoutOldSingle                   = $"{TestProvName.AllSqlServer},{TestProvName.AllFirebirdLess5},{TestProvName.AllPostgreSQL},{TestProvName.AllSQLite}";
		private const string FeatureUpdateOutputWithoutOldSingleNoAlternateRewrite = $"{TestProvName.AllSqlServer},{TestProvName.AllPostgreSQL}";
		private const string FeatureUpdateOutputWithoutOldMultiple                 = $"{TestProvName.AllSqlServer},{TestProvName.AllFirebird5Plus},{TestProvName.AllPostgreSQL},{TestProvName.AllSQLite}";
		private const string FeatureUpdateOutputInto                               = $"{TestProvName.AllSqlServer}";

		sealed class UpdateOutputComparer<T> : IEqualityComparer<UpdateOutput<T>>
			where T : notnull
		{
			readonly IEqualityComparer<T> _comparer = ComparerBuilder.GetEqualityComparer<T>();

			public bool Equals(UpdateOutput<T>? x, UpdateOutput<T>? y)
				=> _comparer.Equals(x!.Deleted, y!.Deleted) && _comparer.Equals(x!.Inserted, y!.Inserted);

			public int GetHashCode(UpdateOutput<T> obj)
				=> _comparer.GetHashCode(obj.Deleted) * -1521134295 + _comparer.GetHashCode(obj.Inserted);
		}

		[Table]
		sealed record TableWithData
		{
			[Column]              public int     Id       { get; set; }
			[Column]              public int     Value    { get; set; }
			[Column(Length = 50)] public string? ValueStr { get; set; }
		}

		[Table(Schema = "TestSchema")]
		sealed record TableWithDataAndSchema
		{
			[Column]              public int     Id       { get; set; }
			[Column]              public int     Value    { get; set; }
			[Column(Length = 50)] public string? ValueStr { get; set; }
		}

		[Table]
		sealed record DestinationTable
		{
			[Column]              public int     Id       { get; set; }
			[Column]              public int     Value    { get; set; }
			[Column(Length = 50)] public string? ValueStr { get; set; }
		}

		static TableWithData[] GetSourceData()
		{
			return Enumerable.Range(1, 9).Select(i =>
					new TableWithData { Id = i, Value = -i, ValueStr = "Str" + i.ToString() })
				.ToArray();
		}

		#region Update against ITable<T> target

		[Test]
		public void UpdateITableWithDefaultOutputTest([IncludeDataSources(true, FeatureUpdateOutputWithOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new UpdateOutput<DestinationTable>()
					{
						Inserted = new DestinationTable { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Deleted  = new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", },
					})
					.ToArray();

				var output = source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutput(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, })
					.ToArray();

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<DestinationTable>());
			}
		}

		[Test]
		public void UpdateITableWithDefaultOutputTestSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new UpdateOutput<DestinationTable>()
					{
						Inserted = new DestinationTable { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Deleted  = new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", },
					})
					.ToArray();

				var output = source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.Where(_ => _.s.Id == 3)
					.UpdateWithOutput(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, })
					.ToArray();

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<DestinationTable>());
			}
		}

		[Test]
		public async Task UpdateITableWithDefaultOutputTestAsync([IncludeDataSources(true, FeatureUpdateOutputWithOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new UpdateOutput<DestinationTable>()
					{
						Inserted = new DestinationTable { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Deleted  = new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", },
					})
					.ToArray();

				var output = await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputAsync(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, })
					.ToArrayAsync();

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<DestinationTable>());
			}
		}

		[Test]
		public async Task UpdateITableWithDefaultOutputTestAsyncSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new UpdateOutput<DestinationTable>()
					{
						Inserted = new DestinationTable { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Deleted  = new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", },
					})
					.ToArray();

				var output = await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.Where(_ => _.s.Id == 3)
					.UpdateWithOutputAsync(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, })
					.ToArrayAsync();

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<DestinationTable>());
			}
		}

		[Test]
		public void UpdateITableWithProjectionOutputTest([IncludeDataSources(true, FeatureUpdateOutputWithOldSingleNoAlternateRewrite)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new
					{
						SourceStr = s.ValueStr,
						DeletedValue = s.Value + 1,
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutput(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							SourceStr = source.s.ValueStr,
							DeletedValue = deleted.Value,
							InsertedValue = inserted.Value,
						})
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateITableWithProjectionOutputTestAlternateUpdate([IncludeDataSources(true, FeatureUpdateOutputWithOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new
					{
						DeletedValue = s.Value + 1,
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutput(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							DeletedValue = deleted.Value,
							InsertedValue = inserted.Value,
						})
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateITableWithProjectionOutputTestAlternateUpdateSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Where(_ => _.Id == 3)
					.Select(s => new
					{
						DeletedValue = s.Value + 1,
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.Where(_ => _.s.Id == 3)
					.UpdateWithOutput(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							DeletedValue = deleted.Value,
							InsertedValue = inserted.Value,
						})
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateITableWithProjectionOutputTestWithoutOld([IncludeDataSources(true, FeatureUpdateOutputWithoutOldSingleNoAlternateRewrite)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new
					{
						SourceStr     = s.ValueStr,
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutput(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							SourceStr     = source.s.ValueStr,
							InsertedValue = inserted.Value,
						})
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateITableWithProjectionOutputTestWithoutOldAlternateUpdate([IncludeDataSources(true, FeatureUpdateOutputWithoutOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new
					{
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutput(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							InsertedValue = inserted.Value,
						})
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateITableWithProjectionOutputTestWithoutOldAlternateUpdateSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithoutOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Where(_ => _.Id == 3)
					.Select(s => new
					{
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.Where(_ => _.s.Id == 3)
					.UpdateWithOutput(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							InsertedValue = inserted.Value,
						})
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateITableWithProjectionOutputTestAsync([IncludeDataSources(true, FeatureUpdateOutputWithOldSingleNoAlternateRewrite)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new
					{
						SourceStr = s.ValueStr,
						DeletedValue = s.Value + 1,
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputAsync(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							SourceStr = source.s.ValueStr,
							DeletedValue = deleted.Value,
							InsertedValue = inserted.Value,
						})
					.ToListAsync();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateITableWithProjectionOutputTestAsyncAlternateUpdate([IncludeDataSources(true, FeatureUpdateOutputWithOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new
					{
						DeletedValue = s.Value + 1,
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputAsync(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							DeletedValue = deleted.Value,
							InsertedValue = inserted.Value,
						})
					.ToListAsync();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateITableWithProjectionOutputTestAsyncAlternateUpdateSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Where(_ => _.Id == 3)
					.Select(s => new
					{
						DeletedValue = s.Value + 1,
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.Where(_ => _.s.Id == 3)
					.UpdateWithOutputAsync(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							DeletedValue = deleted.Value,
							InsertedValue = inserted.Value,
						})
					.ToListAsync();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateITableWithProjectionOutputTestAsyncWithoutOld([IncludeDataSources(true, FeatureUpdateOutputWithoutOldSingleNoAlternateRewrite)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new
					{
						SourceStr     = s.ValueStr,
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputAsync(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							SourceStr     = source.s.ValueStr,
							InsertedValue = inserted.Value,
						})
					.ToListAsync();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateITableWithProjectionOutputTestAsyncWithoutOldAlternateUpdate([IncludeDataSources(true, FeatureUpdateOutputWithoutOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new
					{
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputAsync(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							InsertedValue = inserted.Value,
						})
					.ToListAsync();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateITableWithProjectionOutputTestAsyncWithoutOldAlternateUpdateSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithoutOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Where(_ => _.Id == 3)
					.Select(s => new
					{
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.Where(_ => _.s.Id == 3)
					.UpdateWithOutputAsync(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							InsertedValue = inserted.Value,
						})
					.ToListAsync();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateITableWithDefaultOutputIntoTest([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			using (var destination = db.CreateLocalTable<DestinationTable>(tableName: "Destination"))
			{
				source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputInto(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						destination);

				AreEqual(
					target.ToArray(),
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<DestinationTable>());
			}
		}

		[Test]
		public async Task UpdateITableWithDefaultOutputIntoTestAsync([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			using (var destination = db.CreateLocalTable<DestinationTable>(tableName: "Destination"))
			{
				await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputIntoAsync(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						destination);

				AreEqual(
					target.ToArray(),
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<DestinationTable>());
			}
		}

		[Test]
		public void UpdateITableWithOutputIntoTempTableByTableName([IncludeDataSources(FeatureUpdateOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable("TableWithData_source", sourceData);
			using var target = db.CreateLocalTable("DestinationTable_target", sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", }));
			using var destination = db.CreateTempTable<DestinationTable>(tableName: "DestinationTable_destination");
			var destRef = db.GetTable<DestinationTable>().TableOptions(TableOptions.IsTemporary).TableName(destination.TableName);

			source
				.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
				.UpdateWithOutputInto(
					target,
					s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
					destRef);

			AreEqual(
				target.ToArray(),
				destination.ToArray(),
				ComparerBuilder.GetEqualityComparer<DestinationTable>());
		}
		
		[Test]
		public async Task UpdateITableWithOutputIntoTempTableByTableNameAsync([IncludeDataSources(FeatureUpdateOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable("TableWithData_source", sourceData);
			using var target = db.CreateLocalTable("DestinationTable_target", sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", }));
			using var destination = db.CreateTempTable<DestinationTable>(tableName: "DestinationTable_destination");
			var destRef = db.GetTable<DestinationTable>().TableOptions(TableOptions.IsTemporary).TableName(destination.TableName);

			await source
				.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
				.UpdateWithOutputIntoAsync(
					target,
					s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
					destRef);

			AreEqual(
				target.ToArray(),
				destination.ToArray(),
				ComparerBuilder.GetEqualityComparer<DestinationTable>());
		}

		[Test]
		public void UpdateITableWithProjectionOutputIntoTest([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			using (var destination = db.CreateLocalTable<TableWithData>(tableName: "Destination"))
			{
				var expected = sourceData
					.Select(s => new TableWithData
					{
						Id = s.Id,
						Value = s.Value + 1,
						ValueStr = s.ValueStr,
					})
					.ToArray();

				source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputInto(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						destination,
						(source, deleted, inserted) => new TableWithData { Id = source.s.Id, Value = deleted.Value, ValueStr = inserted.ValueStr, });

				AreEqual(
					expected,
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<TableWithData>());
			}
		}

		[Test]
		public async Task UpdateITableWithProjectionOutputIntoTestAsync([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			using (var destination = db.CreateLocalTable<TableWithData>(tableName: "Destination"))
			{
				var expected = sourceData
					.Select(s => new TableWithData
					{
						Id = s.Id,
						Value = s.Value + 1,
						ValueStr = s.ValueStr,
					})
					.ToArray();

				await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputIntoAsync(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						destination,
						(source, deleted, inserted) => new TableWithData { Id = source.s.Id, Value = deleted.Value, ValueStr = inserted.ValueStr, });

				AreEqual(
					expected,
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<TableWithData>());
			}
		}
		
		[Test]
		public void UpdateITableWithProjectionOutputIntoTempTableByTableName([IncludeDataSources(FeatureUpdateOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable("TableWithData_source", sourceData);
			using var target = db.CreateLocalTable("DestinationTable_target", sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", }));
			using var destination = db.CreateTempTable<TableWithData>(tableName: "TableWithData_destination");
			var destRef = db.GetTable<TableWithData>().TableOptions(TableOptions.IsTemporary).TableName(destination.TableName);
				
			var expected = sourceData
				.Select(s => new TableWithData
				{
					Id = s.Id,
					Value = s.Value + 1,
					ValueStr = s.ValueStr,
				})
				.ToArray();

			source
				.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
				.UpdateWithOutputInto(
					target,
					s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
					destRef,
					(source, deleted, inserted) => new TableWithData { Id = source.s.Id, Value = deleted.Value, ValueStr = inserted.ValueStr, });

			AreEqual(
				expected,
				destination.ToArray(),
				ComparerBuilder.GetEqualityComparer<TableWithData>());
		}

		[Test]
		public async Task UpdateITableWithProjectionOutputIntoTempTableByTableNameAsync([IncludeDataSources(FeatureUpdateOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable("TableWithData_source", sourceData);
			using var target = db.CreateLocalTable("DestinationTable_target", sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", }));
			using var destination = db.CreateTempTable<TableWithData>(tableName: "TableWithData_destination");
			var destRef = db.GetTable<TableWithData>().TableOptions(TableOptions.IsTemporary).TableName(destination.TableName);
				
			var expected = sourceData
				.Select(s => new TableWithData
				{
					Id = s.Id,
					Value = s.Value + 1,
					ValueStr = s.ValueStr,
				})
				.ToArray();

			await source
				.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
				.UpdateWithOutputIntoAsync(
					target,
					s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
					destRef,
					(source, deleted, inserted) => new TableWithData { Id = source.s.Id, Value = deleted.Value, ValueStr = inserted.ValueStr, });

			AreEqual(
				expected,
				destination.ToArray(),
				ComparerBuilder.GetEqualityComparer<TableWithData>());
		}

		#endregion

		#region Update against Expression target

		[Test]
		public void UpdateExpressionWithDefaultOutputTest([IncludeDataSources(true, FeatureUpdateOutputWithOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new UpdateOutput<DestinationTable>()
					{
						Inserted = new DestinationTable { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Deleted  = new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", },
					})
					.ToArray();

				var output = source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutput(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, })
					.ToArray();

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<DestinationTable>());
			}
		}

		[Test]
		public void UpdateExpressionWithDefaultOutputTestSingleRecord([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Where(_ => _.Id == 3)
					.Select(s => new UpdateOutput<DestinationTable>()
					{
						Inserted = new DestinationTable { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Deleted  = new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", },
					})
					.ToArray();

				var output = source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.Where(_ => _.s.Id == 3)
					.UpdateWithOutput(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, })
					.ToArray();

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<DestinationTable>());
			}
		}

		[Test]
		public async Task UpdateExpressionWithDefaultOutputTestAsync([IncludeDataSources(true, FeatureUpdateOutputWithOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new UpdateOutput<DestinationTable>()
					{
						Inserted = new DestinationTable { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Deleted  = new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", },
					})
					.ToArray();

				var output = await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputAsync(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, })
					.ToArrayAsync();

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<DestinationTable>());
			}
		}

		[Test]
		public async Task UpdateExpressionWithDefaultOutputTestAsyncSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Where(_ => _.Id == 3)
					.Select(s => new UpdateOutput<DestinationTable>()
					{
						Inserted = new DestinationTable { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Deleted  = new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", },
					})
					.ToArray();

				var output = await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.Where(_ => _.s.Id == 3)
					.UpdateWithOutputAsync(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, })
					.ToArrayAsync();

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<DestinationTable>());
			}
		}

		[Test]
		public void UpdateExpressionWithProjectionOutputTest([IncludeDataSources(true, FeatureUpdateOutputWithOldSingleNoAlternateRewrite)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new
					{
						SourceStr = s.ValueStr,
						DeletedValue = s.Value + 1,
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutput(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							SourceStr = source.s.ValueStr,
							DeletedValue = deleted.Value,
							InsertedValue = inserted.Value,
						})
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateExpressionWithProjectionOutputTestAlternateUpdate([IncludeDataSources(true, FeatureUpdateOutputWithOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new
					{
						DeletedValue = s.Value + 1,
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutput(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							DeletedValue = deleted.Value,
							InsertedValue = inserted.Value,
						})
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateExpressionWithProjectionOutputTestAlternateUpdateSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Where(_ => _.Id == 3)
					.Select(s => new
					{
						DeletedValue = s.Value + 1,
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.Where(_ => _.s.Id == 3)
					.UpdateWithOutput(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							DeletedValue = deleted.Value,
							InsertedValue = inserted.Value,
						})
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateExpressionWithProjectionOutputTestWithoutOld([IncludeDataSources(true, FeatureUpdateOutputWithoutOldSingleNoAlternateRewrite)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new
					{
						SourceStr     = s.ValueStr,
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutput(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							SourceStr     = source.s.ValueStr,
							InsertedValue = inserted.Value,
						})
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateExpressionWithProjectionOutputTestWithoutOldAlternateUpdate([IncludeDataSources(true, FeatureUpdateOutputWithoutOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new
					{
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutput(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							InsertedValue = inserted.Value,
						})
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateExpressionWithProjectionOutputTestWithoutOldAlternateUpdateSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithoutOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Where(_ => _.Id == 3)
					.Select(s => new
					{
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.Where(_ => _.s.Id == 3)
					.UpdateWithOutput(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							InsertedValue = inserted.Value,
						})
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateExpressionWithProjectionOutputTestAsync([IncludeDataSources(true, FeatureUpdateOutputWithOldSingleNoAlternateRewrite)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new
					{
						SourceStr = s.ValueStr,
						DeletedValue = s.Value + 1,
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputAsync(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							SourceStr = source.s.ValueStr,
							DeletedValue = deleted.Value,
							InsertedValue = inserted.Value,
						})
					.ToListAsync();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateExpressionWithProjectionOutputTestAsyncAlternateUpdate([IncludeDataSources(true, FeatureUpdateOutputWithOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new
					{
						DeletedValue = s.Value + 1,
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputAsync(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							DeletedValue = deleted.Value,
							InsertedValue = inserted.Value,
						})
					.ToListAsync();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateExpressionWithProjectionOutputTestAsyncAlternateUpdateSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Where(_ => _.Id == 3)
					.Select(s => new
					{
						DeletedValue = s.Value + 1,
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.Where(_ => _.s.Id == 3)
					.UpdateWithOutputAsync(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							DeletedValue = deleted.Value,
							InsertedValue = inserted.Value,
						})
					.ToListAsync();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateExpressionWithProjectionOutputTestAsyncWithoutOld([IncludeDataSources(true, FeatureUpdateOutputWithoutOldSingleNoAlternateRewrite)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new
					{
						SourceStr     = s.ValueStr,
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputAsync(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							SourceStr     = source.s.ValueStr,
							InsertedValue = inserted.Value,
						})
					.ToListAsync();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateExpressionWithProjectionOutputTestAsyncWithoutOldAlternateUpdate([IncludeDataSources(true, FeatureUpdateOutputWithoutOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new
					{
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputAsync(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							InsertedValue = inserted.Value,
						})
					.ToListAsync();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateExpressionWithProjectionOutputTestAsyncWithoutOldAlternateUpdateSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithoutOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Where(_ => _.Id == 3)
					.Select(s => new
					{
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.Where(_ => _.s.Id == 3)
					.UpdateWithOutputAsync(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							InsertedValue = inserted.Value,
						})
					.ToListAsync();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateExpressionWithDefaultOutputIntoTest([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			using (var destination = db.CreateLocalTable<DestinationTable>(tableName: "Destination"))
			{
				source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputInto(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						destination);

				AreEqual(
					target.ToArray(),
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<DestinationTable>());
			}
		}

		[Test]
		public async Task UpdateExpressionWithDefaultOutputIntoTestAsync([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			using (var destination = db.CreateLocalTable<DestinationTable>(tableName: "Destination"))
			{
				await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputIntoAsync(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						destination);

				AreEqual(
					target.ToArray(),
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<DestinationTable>());
			}
		}

		[Test]
		public void UpdateExpressionWithDefaultOutputIntoTempTableByTableName([IncludeDataSources(FeatureUpdateOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable("TableWithData_source", sourceData);
			using var target = db.CreateLocalTable("DestinationTable_target", sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", }));
			using var destination = db.CreateTempTable<DestinationTable>(tableName: "DestinationTable_destination");
			var destRef = db.GetTable<DestinationTable>()
				.TableOptions(TableOptions.IsTemporary)
				.TableName(destination.TableName);

			source
				.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
				.UpdateWithOutputInto(
					s => s.t,
					s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
					destRef);

			AreEqual(
				target.ToArray(),
				destination.ToArray(),
				ComparerBuilder.GetEqualityComparer<DestinationTable>());
		}

		[Test]
		public async Task UpdateExpressionWithDefaultOutputIntoTempTableByTableNameAsync([IncludeDataSources(FeatureUpdateOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable("TableWithData_source", sourceData);
			using var target = db.CreateLocalTable("DestinationTable_target", sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", }));
			using var destination = db.CreateTempTable<DestinationTable>(tableName: "DestinationTable_destination");
			var destRef = db.GetTable<DestinationTable>()
				.TableOptions(TableOptions.IsTemporary)
				.TableName(destination.TableName);

			await source
				.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
				.UpdateWithOutputIntoAsync(
					s => s.t,
					s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
					destRef);

			AreEqual(
				target.ToArray(),
				destination.ToArray(),
				ComparerBuilder.GetEqualityComparer<DestinationTable>());
		}

		[Test]
		public void UpdateExpressionWithProjectionOutputIntoTest([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			using (var destination = db.CreateLocalTable<TableWithData>(tableName: "Destination"))
			{
				var expected = sourceData
					.Select(s => new TableWithData
					{
						Id = s.Id,
						Value = s.Value + 1,
						ValueStr = s.ValueStr,
					})
					.ToArray();

				source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputInto(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						destination,
						(source, deleted, inserted) => new TableWithData { Id = source.s.Id, Value = deleted.Value, ValueStr = inserted.ValueStr, });

				AreEqual(
					expected,
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<TableWithData>());
			}
		}

		[Test]
		public async Task UpdateExpressionWithProjectionOutputIntoTestAsync([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			using (var destination = db.CreateLocalTable<TableWithData>(tableName: "Destination"))
			{
				var expected = sourceData
					.Select(s => new TableWithData
					{
						Id = s.Id,
						Value = s.Value + 1,
						ValueStr = s.ValueStr,
					})
					.ToArray();

				await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputIntoAsync(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						destination,
						(source, deleted, inserted) => new TableWithData { Id = source.s.Id, Value = deleted.Value, ValueStr = inserted.ValueStr, });

				AreEqual(
					expected,
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<TableWithData>());
			}
		}
		
		[Test]
		public void UpdateExpressionWithProjectionOutputIntoTempTableByTableName([IncludeDataSources(FeatureUpdateOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable("TableWithData_source", sourceData);
			using var target = db.CreateLocalTable("DestinationTable_target", sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", }));
			using var destination = db.CreateTempTable<TableWithData>(tableName: "TableWithData_destination");
			var destRef = db.GetTable<TableWithData>()
				.TableOptions(TableOptions.IsTemporary)
				.TableName(destination.TableName);
				
			var expected = sourceData
				.Select(s => new TableWithData
				{
					Id = s.Id,
					Value = s.Value + 1,
					ValueStr = s.ValueStr,
				})
				.ToArray();

			source
				.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
				.UpdateWithOutputInto(
					s => s.t,
					s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
					destRef,
					(source, deleted, inserted) => new TableWithData { Id = source.s.Id, Value = deleted.Value, ValueStr = inserted.ValueStr, });

			AreEqual(
				expected,
				destination.ToArray(),
				ComparerBuilder.GetEqualityComparer<TableWithData>());
		}
		
		[Test]
		public async Task UpdateExpressionWithProjectionOutputIntoTempTableByTableNameAsync([IncludeDataSources(FeatureUpdateOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable("TableWithData_source", sourceData);
			using var target = db.CreateLocalTable("DestinationTable_target", sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", }));
			using var destination = db.CreateTempTable<TableWithData>(tableName: "TableWithData_destination");
			var destRef = db.GetTable<TableWithData>()
				.TableOptions(TableOptions.IsTemporary)
				.TableName(destination.TableName);
				
			var expected = sourceData
				.Select(s => new TableWithData
				{
					Id = s.Id,
					Value = s.Value + 1,
					ValueStr = s.ValueStr,
				})
				.ToArray();

			await source
				.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
				.UpdateWithOutputIntoAsync(
					s => s.t,
					s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
					destRef,
					(source, deleted, inserted) => new TableWithData { Id = source.s.Id, Value = deleted.Value, ValueStr = inserted.ValueStr, });

			AreEqual(
				expected,
				destination.ToArray(),
				ComparerBuilder.GetEqualityComparer<TableWithData>());
		}

		#endregion

		#region Update against Source

		[Test]
		public void UpdateSourceWithDefaultOutputTest([IncludeDataSources(true, FeatureUpdateOutputWithOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new UpdateOutput<TableWithData>()
					{
						Deleted  = new TableWithData { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Inserted = new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
					})
					.ToArray();

				var output = source
					.Where(s => s.Id > 3)
					.UpdateWithOutput(s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", })
					.ToArray();

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<TableWithData>());
			}
		}

		[Test]
		public void UpdateSourceWithDefaultOutputTestSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new UpdateOutput<TableWithData>()
					{
						Deleted  = new TableWithData { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Inserted = new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
					})
					.ToArray();

				var output = source
					.Where(s => s.Id == 3)
					.UpdateWithOutput(s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", })
					.ToArray();

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<TableWithData>());
			}
		}

		[Test]
		public async Task UpdateSourceWithDefaultOutputTestAsync([IncludeDataSources(true, FeatureUpdateOutputWithOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new UpdateOutput<TableWithData>()
					{
						Deleted  = new TableWithData { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Inserted = new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
					})
					.ToArray();

				var output = await source
					.Where(s => s.Id > 3)
					.UpdateWithOutputAsync(s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", })
					.ToArrayAsync();

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<TableWithData>());
			}
		}

		[Test]
		public async Task UpdateSourceWithDefaultOutputTestAsyncSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new UpdateOutput<TableWithData>()
					{
						Deleted  = new TableWithData { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Inserted = new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
					})
					.ToArray();

				var output = await source
					.Where(s => s.Id == 3)
					.UpdateWithOutputAsync(s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", })
					.ToArrayAsync();

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<TableWithData>());
			}
		}

		[Test]
		public void UpdateSourceWithProjectionOutputTest([IncludeDataSources(true, FeatureUpdateOutputWithOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new
					{
						DeletedValue  = s.Value,
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = source
					.Where(s => s.Id > 3)
					.UpdateWithOutput(
						s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
						(deleted, inserted) => new { DeletedValue = deleted.Value, InsertedValue = inserted.Value, })
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateSourceWithProjectionOutputTestSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new
					{
						DeletedValue  = s.Value,
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = source
					.Where(s => s.Id == 3)
					.UpdateWithOutput(
						s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
						(deleted, inserted) => new { DeletedValue = deleted.Value, InsertedValue = inserted.Value, })
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateSourceWithProjectionOutputTestWithoutOld([IncludeDataSources(true, FeatureUpdateOutputWithoutOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new
					{
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = source
					.Where(s => s.Id > 3)
					.UpdateWithOutput(
						s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
						(deleted, inserted) => new { InsertedValue = inserted.Value, })
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateSourceWithProjectionOutputTestWithoutOldSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithoutOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new
					{
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = source
					.Where(s => s.Id == 3)
					.UpdateWithOutput(
						s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
						(deleted, inserted) => new { InsertedValue = inserted.Value, })
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateSourceWithProjectionOutputTestAsync([IncludeDataSources(true, FeatureUpdateOutputWithOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new
					{
						DeletedValue  = s.Value,
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = await source
					.Where(s => s.Id > 3)
					.UpdateWithOutputAsync(
						s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
						(deleted, inserted) => new { DeletedValue = deleted.Value, InsertedValue = inserted.Value, })
					.ToListAsync();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateSourceWithProjectionOutputTestAsyncSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new
					{
						DeletedValue  = s.Value,
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = await source
					.Where(s => s.Id == 3)
					.UpdateWithOutputAsync(
						s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
						(deleted, inserted) => new { DeletedValue = deleted.Value, InsertedValue = inserted.Value, })
					.ToListAsync();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateSourceWithProjectionOutputTestAsyncWithoutOld([IncludeDataSources(true, FeatureUpdateOutputWithoutOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new
					{
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = await source
					.Where(s => s.Id > 3)
					.UpdateWithOutputAsync(
						s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
						(deleted, inserted) => new { InsertedValue = inserted.Value, })
					.ToListAsync();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateSourceWithProjectionOutputTestAsyncWithoutOldSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithoutOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new
					{
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = await source
					.Where(s => s.Id == 3)
					.UpdateWithOutputAsync(
						s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
						(deleted, inserted) => new { InsertedValue = inserted.Value, })
					.ToListAsync();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateSourceWithDefaultOutputIntoTest([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData         = GetSourceData();
			using (var db          = GetDataContext(context))
			using (var source      = db.CreateLocalTable(sourceData))
			using (var destination = db.CreateLocalTable<TableWithData>("destination"))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", })
					.ToArray();

				source
					.Where(s => s.Id > 3)
					.UpdateWithOutputInto(
						s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
						destination);

				AreEqual(
					expected,
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<TableWithData>());
			}
		}

		[Test]
		public async Task UpdateSourceWithDefaultOutputIntoTestAsync([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData         = GetSourceData();
			using (var db          = GetDataContext(context))
			using (var source      = db.CreateLocalTable(sourceData))
			using (var destination = db.CreateLocalTable<TableWithData>("destination"))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", })
					.ToArray();

				await source
					.Where(s => s.Id > 3)
					.UpdateWithOutputIntoAsync(
						s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
						destination);

				AreEqual(
					expected,
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<TableWithData>());
			}
		}

		[Test]
		public void UpdateSourceWithDefaultOutputIntoTempTableByTableName([IncludeDataSources(FeatureUpdateOutputInto)] string context)
		{
			var sourceData         = GetSourceData();
			using var db = GetDataContext(context);
			using var source = db.CreateLocalTable("TableWithData_source", sourceData);
			using var destination = db.CreateTempTable<TableWithData>("TableWithData_destination");
			var destRef = db.GetTable<TableWithData>()
				.TableOptions(TableOptions.IsTemporary)
				.TableName(destination.TableName);
				
			var expected = sourceData
				.Where(s => s.Id > 3)
				.Select(s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", })
				.ToArray();

			source
				.Where(s => s.Id > 3)
				.UpdateWithOutputInto(
					s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
					destRef);

			AreEqual(
				expected,
				destination.ToArray(),
				ComparerBuilder.GetEqualityComparer<TableWithData>());
		}

		[Test]
		public async Task UpdateSourceWithDefaultOutputIntoTempTableByTableNameAsync([IncludeDataSources(FeatureUpdateOutputInto)] string context)
		{
			var sourceData         = GetSourceData();
			using var db = GetDataContext(context);
			using var source = db.CreateLocalTable("TableWithData_source", sourceData);
			using var destination = db.CreateTempTable<TableWithData>("TableWithData_destination");
			var destRef = db.GetTable<TableWithData>()
				.TableOptions(TableOptions.IsTemporary)
				.TableName(destination.TableName);
				
			var expected = sourceData
				.Where(s => s.Id > 3)
				.Select(s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", })
				.ToArray();

			await source
				.Where(s => s.Id > 3)
				.UpdateWithOutputIntoAsync(
					s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
					destRef);

			AreEqual(
				expected,
				destination.ToArray(),
				ComparerBuilder.GetEqualityComparer<TableWithData>());
		}

		[Test]
		public void UpdateSourceWithProjectionOutputIntoTest([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData         = GetSourceData();
			using (var db          = GetDataContext(context))
			using (var source      = db.CreateLocalTable(sourceData))
			using (var destination = db.CreateLocalTable<DestinationTable>("destination"))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new DestinationTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr + "Upd", })
					.ToArray();

				source
					.Where(s => s.Id > 3)
					.UpdateWithOutputInto(
						s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
						destination,
						(deleted, inserted) => new DestinationTable { Id = inserted.Id, Value = deleted.Value, ValueStr = inserted.ValueStr, });

				AreEqual(
					expected,
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<DestinationTable>());
			}
		}

		[Test]
		public async Task UpdateSourceWithProjectionOutputTestIntoAsync([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData         = GetSourceData();
			using (var db          = GetDataContext(context))
			using (var source      = db.CreateLocalTable(sourceData))
			using (var destination = db.CreateLocalTable<DestinationTable>("destination"))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new DestinationTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr + "Upd", })
					.ToArray();

				await source
					.Where(s => s.Id > 3)
					.UpdateWithOutputIntoAsync(
						s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
						destination,
						(deleted, inserted) => new DestinationTable { Id = inserted.Id, Value = deleted.Value, ValueStr = inserted.ValueStr, });

				AreEqual(
					expected,
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<DestinationTable>());
			}
		}

		[Test]
		public void UpdateSourceWithProjectionOutputIntoTempTableByTableName([IncludeDataSources(FeatureUpdateOutputInto)] string context)
		{
			var sourceData        = GetSourceData();
			var db                = GetDataContext(context);
			using var source      = db.CreateLocalTable("TableWithData_source", sourceData);
			using var destination = db.CreateTempTable<DestinationTable>("DestinationTable_destination");
			var destRef           = db.GetTable<DestinationTable>()
				.TableOptions(TableOptions.IsTemporary)
				.TableName(destination.TableName);

			var expected = sourceData
				.Where(s => s.Id > 3)
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr + "Upd", })
				.ToArray();

			source
				.Where(s => s.Id > 3)
				.UpdateWithOutputInto(
					s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
					destRef,
					(deleted, inserted) => new DestinationTable { Id = inserted.Id, Value = deleted.Value, ValueStr = inserted.ValueStr, });

			AreEqual(
				expected,
				destination.ToArray(),
				ComparerBuilder.GetEqualityComparer<DestinationTable>());
		}

		[Test]
		public async Task UpdateSourceWithProjectionOutputIntoTempTableByTableNameAsync([IncludeDataSources(FeatureUpdateOutputInto)] string context)
		{
			var sourceData         = GetSourceData();
			var db                 = GetDataContext(context);
			using var source       = db.CreateLocalTable("TableWithData_source", sourceData);
			using var destination  = db.CreateTempTable<DestinationTable>("DestinationTable_destination");
			var destRef            = db.GetTable<DestinationTable>()
				.TableOptions(TableOptions.IsTemporary)
				.TableName(destination.TableName);

			var expected = sourceData
				.Where(s => s.Id > 3)
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr + "Upd", })
				.ToArray();

			await source
				.Where(s => s.Id > 3)
				.UpdateWithOutputIntoAsync(
					s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
					destRef,
					(deleted, inserted) => new DestinationTable { Id = inserted.Id, Value = deleted.Value, ValueStr = inserted.ValueStr, });

			AreEqual(
				expected,
				destination.ToArray(),
				ComparerBuilder.GetEqualityComparer<DestinationTable>());
		}

		#endregion

		#region Update against Source

		[Test]
		public void UpdateIUpdatableWithDefaultOutputTest([IncludeDataSources(true, FeatureUpdateOutputWithOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new UpdateOutput<TableWithData>()
					{
						Deleted  = new TableWithData { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Inserted = new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
					})
					.ToArray();

				var output = source
					.Where(s => s.Id > 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutput()
					.ToArray();

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<TableWithData>());
			}
		}

		[Test]
		public void UpdateIUpdatableWithDefaultOutputTestSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new UpdateOutput<TableWithData>()
					{
						Deleted  = new TableWithData { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Inserted = new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
					})
					.ToArray();

				var output = source
					.Where(s => s.Id == 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutput()
					.ToArray();

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<TableWithData>());
			}
		}

		[Test]
		public async Task UpdateIUpdatableWithDefaultOutputTestAsync([IncludeDataSources(true, FeatureUpdateOutputWithOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new UpdateOutput<TableWithData>()
					{
						Deleted  = new TableWithData { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Inserted = new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
					})
					.ToArray();

				var output = await source
					.Where(s => s.Id > 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutputAsync()
					.ToArrayAsync();

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<TableWithData>());
			}
		}

		[Test]
		public async Task UpdateIUpdatableWithDefaultOutputTestAsyncSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new UpdateOutput<TableWithData>()
					{
						Deleted  = new TableWithData { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Inserted = new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
					})
					.ToArray();

				var output = await source
					.Where(s => s.Id == 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutputAsync()
					.ToArrayAsync();

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<TableWithData>());
			}
		}

		[Test]
		public void UpdateIUpdatableWithProjectionOutputTest([IncludeDataSources(true, FeatureUpdateOutputWithOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new
					{
						DeletedValue  = s.Value,
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = source
					.Where(s => s.Id > 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutput(
						(deleted, inserted) => new { DeletedValue = deleted.Value, InsertedValue = inserted.Value, })
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateIUpdatableWithProjectionOutputTestSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new
					{
						DeletedValue  = s.Value,
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = source
					.Where(s => s.Id == 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutput(
						(deleted, inserted) => new { DeletedValue = deleted.Value, InsertedValue = inserted.Value, })
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateIUpdatableWithProjectionOutputTestWithoutOld([IncludeDataSources(true, FeatureUpdateOutputWithoutOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new
					{
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = source
					.Where(s => s.Id > 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutput(
						(deleted, inserted) => new { InsertedValue = inserted.Value, })
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateIUpdatableWithProjectionOutputTestWithoutOldSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithoutOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new
					{
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = source
					.Where(s => s.Id == 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutput(
						(deleted, inserted) => new { InsertedValue = inserted.Value, })
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateIUpdatableWithProjectionOutputTestAsync([IncludeDataSources(true, FeatureUpdateOutputWithOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new
					{
						DeletedValue  = s.Value,
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = await source
					.Where(s => s.Id > 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutputAsync(
						(deleted, inserted) => new { DeletedValue = deleted.Value, InsertedValue = inserted.Value, })
					.ToListAsync();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateIUpdatableWithProjectionOutputTestAsyncSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new
					{
						DeletedValue  = s.Value,
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = await source
					.Where(s => s.Id == 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutputAsync(
						(deleted, inserted) => new { DeletedValue = deleted.Value, InsertedValue = inserted.Value, })
					.ToListAsync();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateIUpdatableWithProjectionOutputTestAsyncWithoutOld([IncludeDataSources(true, FeatureUpdateOutputWithoutOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new
					{
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = await source
					.Where(s => s.Id > 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutputAsync(
						(deleted, inserted) => new { InsertedValue = inserted.Value, })
					.ToListAsync();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateIUpdatableWithProjectionOutputTestAsyncWithoutOldSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithoutOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new
					{
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = await source
					.Where(s => s.Id == 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutputAsync(
						(deleted, inserted) => new { InsertedValue = inserted.Value, })
					.ToListAsync();

				AreEqual(
					expected,
					output);
			}
		}
		
		[Test]
		public void UpdateIUpdatableWithDefaultOutputIntoTest([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData         = GetSourceData();
			using (var db          = GetDataContext(context))
			using (var source      = db.CreateLocalTable(sourceData))
			using (var destination = db.CreateLocalTable<TableWithData>("destination"))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", })
					.ToArray();

				source
					.Where(s => s.Id > 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutputInto(
						destination);

				AreEqual(
					expected,
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<TableWithData>());
			}
		}

		[Test]
		public async Task UpdateIUpdatableWithDefaultOutputIntoTestAsync([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData         = GetSourceData();
			using (var db          = GetDataContext(context))
			using (var source      = db.CreateLocalTable(sourceData))
			using (var destination = db.CreateLocalTable<TableWithData>("destination"))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", })
					.ToArray();

				await source
					.Where(s => s.Id > 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutputIntoAsync(
						destination);

				AreEqual(
					expected,
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<TableWithData>());
			}
		}

		[Test]
		public void UpdateIUpdateableWithOutputIntoTempTableByTableName([IncludeDataSources(FeatureUpdateOutputInto)] string context)
		{
			var sourceData         = GetSourceData();
			using var db = GetDataContext(context);
			using var source = db.CreateLocalTable("TableWithData_source", sourceData);
			using var destination = db.CreateTempTable<TableWithData>("TableWithData_destination");
			var destRef = db.GetTable<TableWithData>().TableOptions(TableOptions.IsTemporary).TableName(destination.TableName);
			
			var expected = sourceData
				.Where(s => s.Id > 3)
				.Select(s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", })
				.ToArray();

			source
				.Where(s => s.Id > 3)
				.AsUpdatable()
				.Set(s => s.Value, s => s.Value + 1)
				.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
				.UpdateWithOutputInto(
					destRef);

			AreEqual(
				expected,
				destination.ToArray(),
				ComparerBuilder.GetEqualityComparer<TableWithData>());
		}

		[Test]
		public async Task UpdateIUpdateableWithOutputIntoTempTableByTableNameAsync([IncludeDataSources(FeatureUpdateOutputInto)] string context)
		{
			var sourceData         = GetSourceData();
			using var db = GetDataContext(context);
			using var source = db.CreateLocalTable("TableWithData_source", sourceData);
			using var destination = db.CreateTempTable<TableWithData>("TableWithData_destination");
			var destRef = db.GetTable<TableWithData>().TableOptions(TableOptions.IsTemporary).TableName(destination.TableName);
			
			var expected = sourceData
				.Where(s => s.Id > 3)
				.Select(s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", })
				.ToArray();

			await source
				.Where(s => s.Id > 3)
				.AsUpdatable()
				.Set(s => s.Value, s => s.Value + 1)
				.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
				.UpdateWithOutputIntoAsync(
					destRef);

			AreEqual(
				expected,
				destination.ToArray(),
				ComparerBuilder.GetEqualityComparer<TableWithData>());
		}

		[Test]
		public void UpdateIUpdatableWithProjectionOutputIntoTest([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData         = GetSourceData();
			using (var db          = GetDataContext(context))
			using (var source      = db.CreateLocalTable(sourceData))
			using (var destination = db.CreateLocalTable<DestinationTable>("destination"))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new DestinationTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr + "Upd", })
					.ToArray();

				source
					.Where(s => s.Id > 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutputInto(
						destination,
						(deleted, inserted) => new DestinationTable { Id = inserted.Id, Value = deleted.Value, ValueStr = inserted.ValueStr, });

				AreEqual(
					expected,
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<DestinationTable>());
			}
		}

		[Test]
		public async Task UpdateIUpdatableWithProjectionOutputTestIntoAsync([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData         = GetSourceData();
			using (var db          = GetDataContext(context))
			using (var source      = db.CreateLocalTable(sourceData))
			using (var destination = db.CreateLocalTable<DestinationTable>("destination"))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new DestinationTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr + "Upd", })
					.ToArray();

				await source
					.Where(s => s.Id > 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutputIntoAsync(
						destination,
						(deleted, inserted) => new DestinationTable { Id = inserted.Id, Value = deleted.Value, ValueStr = inserted.ValueStr, });

				AreEqual(
					expected,
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<DestinationTable>());
			}
		}

		[Test]
		public void UpdateIUpdatableWithProjectionOutputIntoTempTableByTableName([IncludeDataSources(FeatureUpdateOutputInto)] string context)
		{
			var sourceData         = GetSourceData();
			using var db          = GetDataContext(context);
			using var source      = db.CreateLocalTable("TableWithData_source", sourceData);
			using var destination = db.CreateTempTable<DestinationTable>("DestinationTable_destination");
			var destRef = db.GetTable<DestinationTable>()
				.TableOptions(TableOptions.IsTemporary)
				.TableName(destination.TableName);

			var expected = sourceData
				.Where(s => s.Id > 3)
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr + "Upd", })
				.ToArray();

			source
				.Where(s => s.Id > 3)
				.AsUpdatable()
				.Set(s => s.Value, s => s.Value + 1)
				.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
				.UpdateWithOutputInto(
					destRef,
					(deleted, inserted) => new DestinationTable { Id = inserted.Id, Value = deleted.Value, ValueStr = inserted.ValueStr, });

			AreEqual(
				expected,
				destination.ToArray(),
				ComparerBuilder.GetEqualityComparer<DestinationTable>());
		}

		[Test]
		public async Task UpdateIUpdatableWithProjectionOutputIntoTempTableByTableNameAsync([IncludeDataSources(FeatureUpdateOutputInto)] string context)
		{
			var sourceData         = GetSourceData();
			using var db          = GetDataContext(context);
			using var source      = db.CreateLocalTable("TableWithData_source", sourceData);
			using var destination = db.CreateTempTable<DestinationTable>("DestinationTable_destination");
			var destRef = db.GetTable<DestinationTable>()
				.TableOptions(TableOptions.IsTemporary)
				.TableName(destination.TableName);

			var expected = sourceData
				.Where(s => s.Id > 3)
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr + "Upd", })
				.ToArray();

			await source
				.Where(s => s.Id > 3)
				.AsUpdatable()
				.Set(s => s.Value, s => s.Value + 1)
				.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
				.UpdateWithOutputIntoAsync(
					destRef,
					(deleted, inserted) => new DestinationTable { Id = inserted.Id, Value = deleted.Value, ValueStr = inserted.ValueStr, });

			AreEqual(
				expected,
				destination.ToArray(),
				ComparerBuilder.GetEqualityComparer<DestinationTable>());
		}

		[Test]
		public async Task UpdateWithOutputIntoTempTable([IncludeDataSources(FeatureUpdateOutputInto)] string context)
		{
			var sourceData        = GetSourceData();
			using var db          = GetDataContext(context);
			using var source      = db.CreateLocalTable(sourceData);
			using var destination = db.CreateLocalTable<DestinationTable>("destination", tableOptions: TableOptions.IsTemporary);

			var expected = sourceData
				.Where(s => s.Id > 3)
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr + "Upd", })
				.ToArray();

			await source
				.Where(s => s.Id > 3)
				.AsUpdatable()
				.Set(s => s.Value, s => s.Value + 1)
				.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
				.UpdateWithOutputIntoAsync(
					destination,
					(deleted, inserted) => new DestinationTable { Id = inserted.Id, Value = deleted.Value, ValueStr = inserted.ValueStr, });

			AreEqual(
				expected,
				destination.ToArray(),
				ComparerBuilder.GetEqualityComparer<DestinationTable>());
		}

		#endregion

		#region Issues
		[Test]
		public void Issue3044UpdateOutputWithTake([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllFirebird)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var output = source
					.Where(i => i.Id >= 7)
					.OrderBy(i => i.Id)
					.Take(1)
					.UpdateWithOutput(x => new TableWithData { Id = 20, Value = x.Value, ValueStr = x.ValueStr });

				AreEqual(
					new[]
					{
						new UpdateOutput<TableWithData>
						{
							Deleted  = sourceData[6],
							Inserted = sourceData[6] with { Id = 20 },
						}
					},
					output,
					new UpdateOutputComparer<TableWithData>());
			}
		}

		[Test]
		public void Issue3044UpdateOutputWithTake2([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllFirebird)] string context)
		{
			var sourceData = GetSourceData();

			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable(sourceData);

			var output = source
				.Where(i => i.Id == 7)
				.OrderBy(i => i.Id)
				.Take(1)
				.UpdateWithOutput(x => new TableWithData { Value = 20, ValueStr = x.ValueStr });

			AreEqual(
				new[]
				{
					new UpdateOutput<TableWithData>
					{
						Deleted  = sourceData[6],
						Inserted = sourceData[6] with { Value = 20 },
					}
				},
				output,
				new UpdateOutputComparer<TableWithData>());
		}

		[Test]
		public void Issue3044UpdateOutputWithTakeSubquery([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllFirebird)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var output = source
					.Where(i => i.Id >= 7)
					.OrderBy(i => i.Id)
					.Take(1)
					.UpdateWithOutput(x => new TableWithData { Id = 20, Value = x.Value, ValueStr = x.ValueStr });

				AreEqual(
					new[]
					{
						new UpdateOutput<TableWithData>
						{
							Deleted = sourceData[6],
							Inserted = sourceData[6] with { Id = 20 },
						}
					},
					output,
					new UpdateOutputComparer<TableWithData>());
			}
		}

		[Test]
		public void Issue3044UpdateOutputWithTakeCte([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var output = source
					.Where(i => i.Id == 7)
					.OrderBy(i => i.Id)
					.Take(1)
					.AsCte()
					.UpdateWithOutput(x => new TableWithData { Value = 20, ValueStr = x.ValueStr });

				AreEqual(
					new[]
					{
						new UpdateOutput<TableWithData>
						{
							Deleted = sourceData[6],
							Inserted = sourceData[6] with { Value = 20 },
						}
					},
					output,
					new UpdateOutputComparer<TableWithData>());
			}
		}

		[Table]
		public class Test3697
		{
			[Identity, PrimaryKey] public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Test3697Item.TestId))]
			public List<Test3697Item> Items { get; set; } = null!;
		}

		[Table]
		public class Test3697Item
		{
			[Identity, PrimaryKey] public int Id     { get; set; }
			[Column              ] public int Value  { get; set; }
			[Column              ] public int TestId { get; set; }
		}

		[Test]
		public void Issue3697Test([IncludeDataSources(true, FeatureUpdateOutputWithoutOldSingle)] string context)
		{
			using var db      = GetDataContext(context);
			using var records = db.CreateLocalTable<Test3697>();
			db.Insert(new Test3697() { Id = 1 });
			using var items   = db.CreateLocalTable(new[] { new Test3697Item() { Id = 2, Value = 3, TestId = 1 } });

			var result = records.SelectMany(a => a.Items)
				.UpdateWithOutput(a => new Test3697Item() { Value = 1 }, (d, i) => i.Id)
				.ToArray();

			Assert.That(result, Has.Length.EqualTo(1));
			Assert.That(result[0], Is.EqualTo(1));

			result = records.InnerJoin(items, (a, b) => a.Id == b.TestId, (a, b) => b)
				.UpdateWithOutput(a => new Test3697Item() { Value = 1 }, (d, i) => i.Id)
				.ToArray();

			Assert.That(result, Has.Length.EqualTo(1));
			Assert.That(result[0], Is.EqualTo(1));
		}

		sealed class Issue4135Table
		{
			[PrimaryKey] public int     Id          { get; set; }
			[Column    ] public string? Name        { get; set; }
			[Column    ] public bool    NeedsUpdate { get; set; }
		}

		[Test]
		public void Issue4135Test([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(
			[
				new Issue4135Table() { Id = 1, Name = "name1", NeedsUpdate = true },
				new Issue4135Table() { Id = 2, Name = "name2", NeedsUpdate = false },
			]);

			var people = tb
				.Where(e => e.NeedsUpdate)
				.OrderBy(e => e.Id)
				.Take(4)
				.UpdateWithOutput(e => new Issue4135Table()
				{
					NeedsUpdate = false
				}, (d, _) => new Issue4135Table()
				{
					Id           = d.Id,
					Name         = d.Name,
					NeedsUpdate  = d.NeedsUpdate
				})
				.ToArray();

			Assert.That(people, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(people[0].Id, Is.EqualTo(1));
				Assert.That(people[0].Name, Is.EqualTo("name1"));
				Assert.That(people[0].NeedsUpdate, Is.True);
			}
		}

		[Table]
		sealed class Issue4193Person
		{
			[Column(CanBeNull = false)] public string Name { get; set; } = null!;
			[Column] public int? EmployeeId { get; set; }

			[Association(ThisKey = nameof(EmployeeId), OtherKey = nameof(Issue4193Employee.Id))]
			public Issue4193Employee? Employee { get; set; }
		}

		[Table]
		sealed class Issue4193Employee
		{
			[Column] public int SalaryId { get; set; }
			[PrimaryKey] public int Id { get; set; }

			[Association(CanBeNull = false, ThisKey = nameof(SalaryId), OtherKey = nameof(Issue4193Salary.Id))]
			public Issue4193Salary Salary { get; set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Issue4193Person.EmployeeId))]
			public IEnumerable<Issue4193Person> People { get; set; } = null!;
		}

		[Table]
		sealed class Issue4193Salary
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public int? Amount { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Issue4193Employee.SalaryId))]
			public IEnumerable<Issue4193Employee> Employees { get; set; } = null!;
		}

		[Test]
		public void Issue4193Test([IncludeDataSources(true, FeatureUpdateOutputWithoutOldSingle)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable([new Issue4193Person() { EmployeeId = 1, Name = "foo" }]);
			using var t2 = db.CreateLocalTable([new Issue4193Employee() { Id = 1, SalaryId = 1 }]);
			using var t3 = db.CreateLocalTable([new Issue4193Salary { Id = 1, Amount = 10 }]);

			var salary = t1.Where(e => e.Name == "foo").Select(e => e.Employee!.Salary);
			var newAmount = salary.UpdateWithOutput(
				s => new Issue4193Salary { Amount = s.Amount + 15 },
				(_, inserted) => inserted.Amount)
				.Single();

			Assert.That(newAmount, Is.EqualTo(25));
		}

		[Test]
		public void Issue4414Test([IncludeDataSources(true, FeatureUpdateOutputWithoutOldSingle)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable([new Issue4193Person() { EmployeeId = 1, Name = "foo" }]);

			var result = t1
				.Where(x => x.EmployeeId == 1)
				.OrderBy(x => x.EmployeeId)
				.Take(3)
				.UpdateWithOutput(
					_ => new Issue4193Person() { Name = "new_name" },
					(d, i) => new { i.EmployeeId, i.Name })
				.ToArray();

			Assert.That(result, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].EmployeeId, Is.EqualTo(1));
				Assert.That(result[0].Name, Is.EqualTo("new_name"));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4253")]
		public void Issue4253Test([IncludeDataSources(true, FeatureUpdateOutputWithoutOldSingle)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable([new Issue4193Person() { EmployeeId = 1, Name = "foo" }]);
			using var t2 = db.CreateLocalTable([new Issue4193Employee() { Id = 1, SalaryId = 1 }]);
			using var t3 = db.CreateLocalTable([new Issue4193Salary { Id = 1, Amount = 10 }]);

			var result = t1
					.Join(t2, SqlJoinType.Inner,
						(p, r) => p.EmployeeId == r.Id,
						(p, r) => Tuple.Create(p, r))
					.Set(tup => tup.Item1.Name, tup => tup.Item1.Name + tup.Item2.SalaryId)
					.UpdateWithOutput((_, tup) => tup.Item1.EmployeeId)
					.ToArray();

			Assert.That(result, Has.Length.EqualTo(1));
			Assert.That(result[0], Is.EqualTo(1));
		}
		#endregion
	}
}
