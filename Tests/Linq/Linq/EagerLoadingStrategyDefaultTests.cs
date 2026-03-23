using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class EagerLoadingStrategyDefaultTests : TestBase
	{
		#region Entities

		[Table]
		sealed class Master
		{
			[Column, PrimaryKey] public int    Id    { get; set; }
			[Column]             public string? Name  { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Detail.MasterId))]
			public List<Detail> Details { get; set; } = null!;
		}

		[Table]
		sealed class Detail
		{
			[Column, PrimaryKey] public int    Id       { get; set; }
			[Column]             public int?   MasterId { get; set; }
			[Column]             public string? Value    { get; set; }
		}

		static (Master[], Detail[]) GenerateData()
		{
			var masters = Enumerable.Range(1, 5)
				.Select(i => new Master { Id = i, Name = "Master" + i })
				.ToArray();

			var details = masters
				.SelectMany(m => Enumerable.Range(1, m.Id)
					.Select(j => new Detail { Id = m.Id * 100 + j, MasterId = m.Id, Value = "Detail" + m.Id + "_" + j }))
				.ToArray();

			return (masters, details);
		}

		#endregion

		[Test]
		public void LoadWith_DefaultStrategy_ReturnsCorrectResults(
			[IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();

			using var db     = GetDataContext(context);
			using var master = db.CreateLocalTable(masterRecords);
			using var detail = db.CreateLocalTable(detailRecords);

			var result = master
				.LoadWith(m => m.Details.AsSeparateQuery())
				.OrderBy(m => m.Id)
				.ToList();

			var expected = masterRecords
				.Select(m => new Master
				{
					Id      = m.Id,
					Name    = m.Name,
					Details = detailRecords.Where(d => d.MasterId == m.Id).ToList(),
				})
				.OrderBy(m => m.Id)
				.ToList();

			foreach (var item in result)
				item.Details = item.Details.OrderBy(d => d.Id).ToList();

			foreach (var item in expected)
				item.Details = item.Details.OrderBy(d => d.Id).ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		[Test]
		public void AsEagerLoading_DefaultStrategy_ReturnsCorrectResults(
			[IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();

			using var db     = GetDataContext(context);
			using var master = db.CreateLocalTable(masterRecords);
			using var detail = db.CreateLocalTable(detailRecords);

			var result = (
				from m in master
				orderby m.Id
				select new
				{
					m.Id,
					m.Name,
					Details = detail
						.Where(d => d.MasterId == m.Id)
						.AsSeparateQuery()
						.OrderBy(d => d.Id)
						.ToList(),
				}
			).ToList();

			var expected = masterRecords
				.OrderBy(m => m.Id)
				.Select(m => new
				{
					m.Id,
					m.Name,
					Details = detailRecords
						.Where(d => d.MasterId == m.Id)
						.OrderBy(d => d.Id)
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		[Test]
		public void GlobalDefaultStrategy_UsedWhenNoPerAssociationStrategy(
			[IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();

			// GlobalDefaultStrategy = Default is the out-of-the-box setting; just verify correctness
			using var db     = GetDataContext(context);
			using var master = db.CreateLocalTable(masterRecords);
			using var detail = db.CreateLocalTable(detailRecords);

			var result = master
				.LoadWith(m => m.Details)
				.OrderBy(m => m.Id)
				.ToList();

			Assert.That(result, Has.Count.EqualTo(masterRecords.Length));

			foreach (var m in result)
			{
				var expectedCount = detailRecords.Count(d => d.MasterId == m.Id);
				Assert.That(m.Details, Has.Count.EqualTo(expectedCount));
			}

		}
	}
}
