using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;

using NUnit.Framework;

namespace Tests.Linq
{
	/// <summary>
	/// Tests for <see cref="EagerLoadingStrategy.CteUnion"/> — combines multiple eager-load preambles
	/// into a single CTE + UNION ALL query.  Requires a provider that supports CTEs.
	/// </summary>
	[TestFixture]
	public class EagerLoadingStrategyCteUnionTests : TestBase
	{
		#region Entities

		[Table]
		sealed class Master
		{
			[Column, PrimaryKey] public int    Id    { get; set; }
			[Column]             public string? Name  { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Detail.MasterId))]
			public List<Detail> Details { get; set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(SubDetail.MasterId))]
			public List<SubDetail> SubDetails { get; set; } = null!;
		}

		[Table]
		sealed class Detail
		{
			[Column, PrimaryKey] public int    Id       { get; set; }
			[Column]             public int?   MasterId { get; set; }
			[Column]             public string? Value    { get; set; }
		}

		[Table]
		sealed class SubDetail
		{
			[Column, PrimaryKey] public int    Id       { get; set; }
			[Column]             public int?   MasterId { get; set; }
			[Column]             public string? Note     { get; set; }
		}

		static (Master[], Detail[], SubDetail[]) GenerateData()
		{
			var masters = Enumerable.Range(1, 5)
				.Select(i => new Master { Id = i, Name = "Master" + i })
				.ToArray();

			var details = masters
				.SelectMany(m => Enumerable.Range(1, m.Id)
					.Select(j => new Detail { Id = m.Id * 100 + j, MasterId = m.Id, Value = "Detail" + m.Id + "_" + j }))
				.ToArray();

			var subDetails = masters
				.SelectMany(m => Enumerable.Range(1, m.Id + 1)
					.Select(j => new SubDetail { Id = m.Id * 200 + j, MasterId = m.Id, Note = "Note" + m.Id + "_" + j }))
				.ToArray();

			return (masters, details, subDetails);
		}

		#endregion

		[Test]
		public void LoadWith_CteUnionStrategy_SingleAssociation(
			[CteContextSource] string context)
		{
			var (masterRecords, detailRecords, _) = GenerateData();

			using var db     = GetDataContext(context);
			using var master = db.CreateLocalTable(masterRecords);
			using var detail = db.CreateLocalTable(detailRecords);

			var result = master
				.LoadWith(m => m.Details.AsUnionQuery())
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
		public void LoadWith_CteUnionStrategy_TwoAssociations_CombinedIntoOneQuery(
			[CteContextSource] string context)
		{
			var (masterRecords, detailRecords, subDetailRecords) = GenerateData();

			using var db        = GetDataContext(context);
			using var master    = db.CreateLocalTable(masterRecords);
			using var detail    = db.CreateLocalTable(detailRecords);
			using var subDetail = db.CreateLocalTable(subDetailRecords);

			var result = master
				.LoadWith(m => m.Details.AsUnionQuery())
				.LoadWith(m => m.SubDetails.AsUnionQuery())
				.OrderBy(m => m.Id)
				.ToList();

			var expected = masterRecords
				.Select(m => new Master
				{
					Id         = m.Id,
					Name       = m.Name,
					Details    = detailRecords.Where(d => d.MasterId == m.Id).ToList(),
					SubDetails = subDetailRecords.Where(s => s.MasterId == m.Id).ToList(),
				})
				.OrderBy(m => m.Id)
				.ToList();

			foreach (var item in result)
			{
				item.Details    = item.Details.OrderBy(d => d.Id).ToList();
				item.SubDetails = item.SubDetails.OrderBy(s => s.Id).ToList();
			}

			foreach (var item in expected)
			{
				item.Details    = item.Details.OrderBy(d => d.Id).ToList();
				item.SubDetails = item.SubDetails.OrderBy(s => s.Id).ToList();
			}

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		[Test]
		public void AsEagerLoading_CteUnionStrategy_SingleCollection(
			[CteContextSource] string context)
		{
			var (masterRecords, detailRecords, _) = GenerateData();

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
						.AsUnionQuery()
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
		public void AsEagerLoading_CteUnionStrategy_TwoCollections_CombinedIntoOneQuery(
			[CteContextSource] string context)
		{
			var (masterRecords, detailRecords, subDetailRecords) = GenerateData();

			using var db        = GetDataContext(context);
			using var master    = db.CreateLocalTable(masterRecords);
			using var detail    = db.CreateLocalTable(detailRecords);
			using var subDetail = db.CreateLocalTable(subDetailRecords);

			var result = (
				from m in master
				orderby m.Id
				select new
				{
					m.Id,
					m.Name,
					Details = detail
						.Where(d => d.MasterId == m.Id)
						.AsUnionQuery()
						.OrderBy(d => d.Id)
						.ToList(),
					SubDetails = subDetail
						.Where(s => s.MasterId == m.Id)
						.AsUnionQuery()
						.OrderBy(s => s.Id)
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
					SubDetails = subDetailRecords
						.Where(s => s.MasterId == m.Id)
						.OrderBy(s => s.Id)
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		[Test]
		public void CteUnionStrategy_ExplicitlySet_TwoAssociationsLoaded(
			[CteContextSource] string context)
		{
			var (masterRecords, detailRecords, subDetailRecords) = GenerateData();

			using var db        = GetDataContext(context);
			using var master    = db.CreateLocalTable(masterRecords);
			using var detail    = db.CreateLocalTable(detailRecords);
			using var subDetail = db.CreateLocalTable(subDetailRecords);

			var result = master
				.LoadWith(m => m.Details.AsUnionQuery())
				.LoadWith(m => m.SubDetails.AsUnionQuery())
				.OrderBy(m => m.Id)
				.ToList();

			Assert.That(result, Has.Count.EqualTo(masterRecords.Length));

			foreach (var m in result)
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(m.Details,    Has.Count.EqualTo(detailRecords.Count(d => d.MasterId == m.Id)));
					Assert.That(m.SubDetails, Has.Count.EqualTo(subDetailRecords.Count(s => s.MasterId == m.Id)));
				}
			}
		}
	}
}
