using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class EagerLoadingTests : TestBase
	{
		[Table]
		class MasterClass
		{
			[Column] [PrimaryKey] public int Id    { get; set; }
			[Column] [PrimaryKey] public int Id2    { get; set; }
			[Column] public string Value { get; set; }

			public List<DetailClass> Details { get; set; }
		}

		[Table]
		class MasterManyId
		{
			[Column] public int Id    { get; set; }
			[Column] public int Id2    { get; set; }
			[Column] public int Id3    { get; set; }
			[Column] public int Id4    { get; set; }
			[Column] public int Id5    { get; set; }
			[Column] public int Id6    { get; set; }
			[Column] public int Id7    { get; set; }
			[Column] public int Id8    { get; set; }
			[Column] public int Id9    { get; set; }

			[Column] public string Value { get; set; }

			public List<DetailClass> Details { get; set; }
		}

		[Table]
		class DetailClass
		{
			[Column] [PrimaryKey] public int DetailId    { get; set; }
			[Column] public int? MasterId    { get; set; }
			[Column] public string DetailValue { get; set; }
		}

		(MasterClass[], DetailClass[]) GenerateData()
		{
			var master = Enumerable.Range(1, 10).Select(i => new MasterClass { Id = i, Id2 = i, Value = "Str" + i })
				.ToArray();

			var detail = master.SelectMany(m => Enumerable.Range(1, m.Id)
					.Select(i => new DetailClass
					{
						DetailId = m.Id * 1000 + i,
						DetailValue = "DetailValue" + m.Id * 1000 + i,
						MasterId = m.Id
					}))
				.ToArray();

			return (master, detail);
		}

		(MasterManyId[], DetailClass[]) GenerateDataManyId()
		{
			var master = Enumerable.Range(1, 10).Select(i => new MasterManyId
			{
				Id = i, Id2 = i + 2, Id3 = i + 3, Id4 = i + 4, Id5 = i + 5, Id6 = i + 6, Id7 = i + 7, Id8 = i + 8, Id9 = i + 9, 
				Value = "Str" + i
			}).ToArray();
			var detail = master.SelectMany(m => Enumerable.Range(1, m.Id)
					.Select(i => new DetailClass
					{
						DetailId = m.Id * 1000 + i,
						DetailValue = "DetailValue" + m.Id * 1000 + i,
						MasterId = m.Id
					}))
				.ToArray();

			return (master, detail);
		}

		[Test]
		public void TestSelectProjectionList([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			{
				var query = from m in master
					select new
					{
						MId = m.Id,
						MId2 = m.Id2,
						Details = detail.InnerJoin(d => d.MasterId == m.Id && d.MasterId == m.Id2).ToList()
					};

				var result = query.ToArray();
			}
		}

		[Test]
		public void SampleSelectTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable<MasterClass>(masterRecords))
			using (var detail = db.CreateLocalTable<DetailClass>(detailRecords))
			{
				var items = EagerLoadingProbes.QueryWithDetails(db, master, m => detail.Where(d => d.MasterId == m.Id),
					(m, d) => m.Details = d);
			}
		}

		[Test]
		public void TestWhenMasterHasNoId([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var (masterRecords, detailRecords) = GenerateDataManyId();

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			{
				var masterQuery = from m in master
					group m by m.Id
					into g
					select new
					{
						Id = g.Key,
						Count = g.Count(),
						Details = new List<DetailClass>()
					};

				var items = EagerLoadingProbes.QueryWithDetails(db, masterQuery, m => detail.Where(d => d.MasterId == m.Id),
					(m, d) => m.Details.AddRange(d));
			}
		}

	}
}
