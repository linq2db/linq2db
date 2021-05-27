using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;
using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class UpdateWithOutputTests : TestBase
	{
		private class UpdateOutputComparer<T> : IEqualityComparer<UpdateOutput<T>>
			where T : notnull
		{
			private readonly IEqualityComparer<T> tComparer;

			public UpdateOutputComparer()
			{
				tComparer = ComparerBuilder.GetEqualityComparer<T>();
			}

			public bool Equals(UpdateOutput<T>? x, UpdateOutput<T>? y)
				=> tComparer.Equals(x!.Deleted, y!.Deleted) && tComparer.Equals(x!.Inserted, y!.Inserted);

			public int GetHashCode(UpdateOutput<T> obj)
				=> tComparer.GetHashCode(obj.Deleted) * -1521134295 + tComparer.GetHashCode(obj.Inserted);
		}

		[Table]
		class TableWithData
		{
			[Column]              public int     Id       { get; set; }
			[Column]              public int     Value    { get; set; }
			[Column(Length = 50)] public string? ValueStr { get; set; }
		}

		[Table(Schema = "TestSchema")]
		class TableWithDataAndSchema
		{
			[Column]              public int     Id       { get; set; }
			[Column]              public int     Value    { get; set; }
			[Column(Length = 50)] public string? ValueStr { get; set; }
		}

		[Table]
		class DestinationTable
		{
			[Column]              public int     Id       { get; set; }
			[Column]              public int     Value    { get; set; }
			[Column(Length = 50)] public string? ValueStr { get; set; }
		}

		static TableWithData[] GetSourceData()
		{
			return Enumerable.Range(1, 10).Select(i =>
					new TableWithData { Id = i, Value = -i, ValueStr = "Str" + i.ToString() })
				.ToArray();
		}

		#region Update against ITable<T> target

		[Test]
		public void UpdateITableWithDefaultOutputTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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
		public async Task UpdateITableWithDefaultOutputTestAsync([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, });

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<DestinationTable>());
			}
		}

		[Test]
		public void UpdateITableWithProjectionOutputTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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
		public async Task UpdateITableWithProjectionOutputTestAsync([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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
						});

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateITableWithDefaultOutputIntoTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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
		public async Task UpdateITableWithDefaultOutputIntoTestAsync([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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
		public void UpdateITableWithProjectionOutputIntoTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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
		public async Task UpdateITableWithProjectionOutputIntoTestAsync([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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

		#endregion

		#region Update against Expression target

		[Test]
		public void UpdateExpressionWithDefaultOutputTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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
		public async Task UpdateExpressionWithDefaultOutputTestAsync([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, });

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<DestinationTable>());
			}
		}

		[Test]
		public void UpdateExpressionWithProjectionOutputTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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
		public async Task UpdateExpressionWithProjectionOutputTestAsync([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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
						});

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateExpressionWithDefaultOutputIntoTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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
		public async Task UpdateExpressionWithDefaultOutputIntoTestAsync([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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
		public void UpdateExpressionWithProjectionOutputIntoTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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
		public async Task UpdateExpressionWithProjectionOutputIntoTestAsync([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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

		#endregion

		#region Update against Source

		[Test]
		public void UpdateSourceWithDefaultOutputTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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
		public async Task UpdateSourceWithDefaultOutputTestAsync([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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
					.UpdateWithOutputAsync(s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", });

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<TableWithData>());
			}
		}

		[Test]
		public void UpdateSourceWithProjectionOutputTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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
		public async Task UpdateSourceWithProjectionOutputTestAsync([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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
						(deleted, inserted) => new { DeletedValue = deleted.Value, InsertedValue = inserted.Value, });

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateSourceWithDefaultOutputIntoTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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
		public async Task UpdateSourceWithDefaultOutputIntoTestAsync([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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
		public void UpdateSourceWithProjectionOutputIntoTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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
		public async Task UpdateSourceWithProjectionOutputTestIntoAsync([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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
		#endregion
	}
}
