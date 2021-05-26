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

		#endregion
	}
}
