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

		[Test]
		public void DeleteWithOutputTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context, [Values(100, 200)] int param)
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

				AreEqual(expected, output);
			}
		}


		[Test]
		public void DeleteWithOutputProjectionFromQueryTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context, [Values(100, 200)] int param)
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
							Id       = Sql.AsSql(deleted.Id + 1),
							ValueStr = Sql.AsSql(deleted.ValueStr + 1),
						})
					.ToArray();

				AreEqual(expected.Select(t => new
				{
					Id = t.Id + 1,
					ValueStr = t.ValueStr + 1,
				}),
					output);
			}
		}

		[Test]
		public void DeleteWithOutputFromQueryTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context, [Values(100, 200)] int param)
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
							Id       = s.Id + param,
							Value    = s.Value + param,
							ValueStr = s.ValueStr + param
						})
					.ToArray();

				AreEqual(expected.Select(s => new DestinationTable
					{
						Id       = s.Id + param,
						Value    = s.Value + param,
						ValueStr = s.ValueStr + param,
					}),
					output, ComparerBuilder.GetEqualityComparer<DestinationTable>());
			}
		}

		[Test]
		public async Task DeleteWithOutputFromQueryTestAsync([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context, [Values(100, 200)] int param)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = source
					.Where(s => s.Id > 3)
					.ToArray();

				var output = await source
					.Where(s => s.Id > 3)
					.DeleteWithOutputAsync(
						s => new DestinationTable
						{
							Id       = s.Id       + param,
							Value    = s.Value    + param,
							ValueStr = s.ValueStr + param
						});

				AreEqual(expected.Select(s => new DestinationTable
					{
						Id       = s.Id       + param,
						Value    = s.Value    + param,
						ValueStr = s.ValueStr + param,
					}),
					output, ComparerBuilder.GetEqualityComparer<DestinationTable>());
			}
		}

		[Test]
		public void DeleteWithOutputIntoFromQueryTest([IncludeDataSources(false, TestProvName.AllSqlServer2008Plus)] string context, [Values(100, 200)] int param)
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
							Id       = s.Id + param,
							Value    = s.Value + param,
							ValueStr = s.ValueStr + param
						});

				AreEqual(expected.Select(s => new DestinationTable
					{
						Id       = s.Id + param,
						Value    = s.Value + param,
						ValueStr = s.ValueStr + param,
					}),
					target.ToArray(), ComparerBuilder.GetEqualityComparer<DestinationTable>());
			}
		}
	}
}
