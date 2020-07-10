using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;
using Tests.Model;
using LinqToDB.Tools.Comparers;

namespace Tests.Playground
{
	[TestFixture]
	public class UpdateWithOutputTests : TestBase
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
		public void UpdateWithOutputProjectionFromQueryTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context, [Values(100, 200)] int param)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var output = source
					.Where(s => s.Id > 3)
					.SelectMany(_ => target, (s, t) => new { s, t, })
					.UpdateWithOutput(
						x => x.t,
						x => new DestinationTable
						{
							Id = x.s.Id,
							Value = x.s.Value + 2,
							ValueStr = x.t.ValueStr,
						},
						(source, deleted, inserted) => new
						{
							SourceStr = source.s.ValueStr,
							DeletedValue = deleted.Value,
							InsertedValue = inserted.Value,
						})
					.ToArray();
			}
		}
	}
}
