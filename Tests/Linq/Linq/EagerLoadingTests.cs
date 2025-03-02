using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using FluentAssertions;

using LinqToDB;
using LinqToDB.Internal;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class EagerLoadingTests : TestBase
	{
		[Table]
		sealed class MasterClass
		{
			[Column] [PrimaryKey] public int Id1    { get; set; }
			[Column] [PrimaryKey] public int Id2    { get; set; }
			[Column] public string? Value { get; set; }

			[Column] public byte[]? ByteValues        { get; set; }

			[Association(ThisKey = nameof(Id1), OtherKey = nameof(DetailClass.MasterId))]
			public List<DetailClass> Details { get; set; } = null!;

			[Association(QueryExpressionMethod = nameof(DetailsQueryImpl))]
			public DetailClass[] DetailsQuery { get; set; } = null!;

			static Expression<Func<MasterClass, IDataContext, IQueryable<DetailClass>>> DetailsQueryImpl()
			{
				return (m, dc) => dc.GetTable<DetailClass>().Where(d => d.MasterId == m.Id1 && d.MasterId == m.Id2 && d.DetailId % 2 == 0);
			}
		}

		[Table]
		sealed class MasterManyId
		{
			[Column] public int Id1    { get; set; }
			[Column] public int Id2    { get; set; }
			[Column] public int Id3    { get; set; }
			[Column] public int Id4    { get; set; }
			[Column] public int Id5    { get; set; }
			[Column] public int Id6    { get; set; }
			[Column] public int Id7    { get; set; }
			[Column] public int Id8    { get; set; }
			[Column] public int Id9    { get; set; }

			[Column] public string? Value { get; set; }

			public List<DetailClass> Details { get; set; } = null!;
}

		[Table]
		sealed class DetailClass
		{
			[Column] [PrimaryKey] public int DetailId    { get; set; }
			[Column] public int? MasterId    { get; set; }
			[Column] public string? DetailValue { get; set; }

			[Association(ThisKey = nameof(DetailId), OtherKey = nameof(SubDetailClass.DetailId))]
			public SubDetailClass[] SubDetails { get; set; } = null!;
}

		[Table]
		sealed class SubDetailClass
		{
			[Column] [PrimaryKey] public int SubDetailId    { get; set; }
			[Column] public int? DetailId    { get; set; }
			[Column] public string? SubDetailValue { get; set; }

			[Association(ThisKey = nameof(DetailId), OtherKey = nameof(DetailClass.DetailId))]
			public SubDetailClass? Detail { get; set; } = null!;

		}

		sealed class SubDetailDTO
		{
			public int SubDetailId    { get; set; }
			public int? DetailId    { get; set; }
			public string? SubDetailValue { get; set; }
		}

		static IQueryable<SubDetailDTO> MakeDTO(IQueryable<SubDetailClass> details)
		{
			return details.Select(d => new SubDetailDTO
			{
				//DetailId = d.DetailId,
				//SubDetailId = d.SubDetailId,
				SubDetailValue = d.SubDetailValue + "_Projected"
			});
		}

		(MasterClass[], DetailClass[]) GenerateData()
		{
			var master = Enumerable.Range(1, 10).Select(i => new MasterClass { Id1 = i, Id2 = i, Value = "Str" + i })
				.ToArray();

			var detail = master.SelectMany(m => m.Id1 % 2 == 0 ? Enumerable.Empty<DetailClass>() : Enumerable.Range(1, m.Id1)
					.Select(i => new DetailClass
					{
						DetailId = m.Id1 * 1000 + i,
						DetailValue = "DetailValue" + m.Id1 * 1000 + i,
						MasterId = m.Id1
					}))
				.ToArray();

			return (master, detail);
		}

		(MasterClass[], DetailClass[], SubDetailClass[]) GenerateDataWithSubDetail()
		{
			var (masterRecords, detailRecords) = GenerateData();

			var subdetail = detailRecords.SelectMany(m => Enumerable.Range(1, m.DetailId / 100)
					.Select(i => new SubDetailClass
					{
						DetailId = m.DetailId,
						SubDetailValue = "SubDetailValue" + m.DetailId * 1000 + i,
						SubDetailId = m.DetailId * 1000 + i
					}))
				.ToArray();

			return (masterRecords, detailRecords, subdetail);
		}

		(MasterManyId[], DetailClass[]) GenerateDataManyId()
		{
			var master = Enumerable.Range(1, 10).Select(i => new MasterManyId
			{
				Id1 = i, Id2 = i + 2, Id3 = i + 3, Id4 = i + 4, Id5 = i + 5, Id6 = i + 6, Id7 = i + 7, Id8 = i + 8, Id9 = i + 9, 
				Value = "Str" + i
			}).ToArray();
			var detail = master.SelectMany(m => Enumerable.Range(1, m.Id1)
					.Select(i => new DetailClass
					{
						DetailId = m.Id1 * 1000 + i,
						DetailValue = "DetailValue" + m.Id1 * 1000 + i,
						MasterId = m.Id1
					}))
				.ToArray();

			return (master, detail);
		}

		[Test]
		public void TestLoadWith([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();
			var intParam = 0;

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			{
				var query = from m in master.LoadWith(m => m.Details).LoadWith(m => m.DetailsQuery)
					where m.Id1 >= intParam
					select m;

				var expectedQuery = from m in masterRecords
					where m.Id1 >= intParam
					select new MasterClass
					{
						Id1          = m.Id1,
						Id2          = m.Id2,
						Value        = m.Value,
						Details      = detailRecords.Where(d => d.MasterId == m.Id1).ToList(),
						DetailsQuery = detailRecords.Where(d => d.MasterId == m.Id1 && d.MasterId == m.Id2 && d.DetailId % 2 == 0).ToArray(),
					};

				var result = query.ToList();

				var expected = expectedQuery.ToList();

				foreach (var item in result.Concat(expected))
				{
					item.Details      = item.Details.OrderBy(_ => _.DetailId).ToList();
					item.DetailsQuery = item.DetailsQuery.OrderBy(_ => _.DetailId).ToArray();
				}

				AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2442")]
		public async Task TestLoadWithAsyncEnumerator([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();
			var intParam = 0;

			using (var db     = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			{
				var query = from m in master.LoadWith(m => m.Details).LoadWith(m => m.DetailsQuery)
							where m.Id1 >= intParam
							select m;

				var expectedQuery = from m in masterRecords
									where m.Id1 >= intParam
									select new MasterClass
									{
										Id1          = m.Id1,
										Id2          = m.Id2,
										Value        = m.Value,
										Details      = detailRecords.Where(d => d.MasterId == m.Id1).ToList(),
										DetailsQuery = detailRecords.Where(d => d.MasterId == m.Id1 && d.MasterId == m.Id2 && d.DetailId % 2 == 0).ToArray(),
									};

				var result = new List<MasterClass>();

				await foreach (var item in (IAsyncEnumerable<MasterClass>)query)
					result.Add(item);

				var expected = expectedQuery.ToList();

				foreach (var item in result.Concat(expected))
				{
					item.Details      = item.Details.OrderBy(_ => _.DetailId).ToList();
					item.DetailsQuery = item.DetailsQuery.OrderBy(_ => _.DetailId).ToArray();
				}

				AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
			}
		}

		[Test]
		public void TestLoadWithAndExtensions([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var (masterRecords, detailRecords, subDetailRecords) = GenerateDataWithSubDetail();
			
			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			using (var subDetail = db.CreateLocalTable(subDetailRecords))
			{
				var query = 
					from d in detail
					from m in master.InnerJoin(m => m.Id1 == d.MasterId)
					where m.Id1.In(1, 2)
					select d;

				query = query.LoadWith(d => d.SubDetails).ThenLoad(sd => sd.Detail);
				var result = query.ToArray();

				Assert.That(result, Has.Length.EqualTo(1));
				Assert.That(result[0].SubDetails, Has.Length.EqualTo(100));
				Assert.That(result[0].SubDetails[0].Detail, Is.Not.Null);
			}
		}

		[Test]
		public void TestLoadWithAndDuplications([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();
			
			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			{
				var query = 
					from m in master
					from d in detail.InnerJoin(d => m.Id1 == d.MasterId)
					select m;

				query = query.LoadWith(d => d.Details);

				var expectedQuery = from m in masterRecords
					join dd in detailRecords on m.Id1 equals dd.MasterId
					select new MasterClass
					{
						Id1 = m.Id1,
						Id2 = m.Id2,
						Value = m.Value,
						Details = detailRecords.Where(d => m.Id1 == d.MasterId).ToList(),
					};

				var result = query.ToList();
				var expected = expectedQuery.ToList();

				foreach (var item in result.Concat(expected))
					item.Details = item.Details.OrderBy(_ => _.DetailId).ToList();

				AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
			}
		}

		[Test]
		public void TestLoadWithFromProjection([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var (masterRecords, detailRecords, subDetailRecords) = GenerateDataWithSubDetail();
			
			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			using (var subDetail = db.CreateLocalTable(subDetailRecords))
			{
				var subQuery = 
					from m in master
					from d in detail.InnerJoin(d => m.Id1 == d.MasterId)
					select new {m, d};

				var query = subQuery.Select(r => new { One = r, Two = r.d });

				query = query.LoadWith(a => a.One.m.Details).ThenLoad(d => d.SubDetails)
					.LoadWith(a => a.One.d.SubDetails)
					.LoadWith(b => b.Two.SubDetails).ThenLoad(sd => sd.Detail);

				var result = query.ToArray();

				foreach (var item in result)
				{
					Assert.Multiple(() =>
					{
						Assert.That(ReferenceEquals(item.One.d, item.Two), Is.True);
						Assert.That(item.Two.SubDetails, Is.Not.Empty);
					});
					Assert.That(item.Two.SubDetails[0].Detail, Is.Not.Null);
				}
			}
		}

		[Test]
		public void TestLoadWithToString1([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.Parent.LoadWith(p => p.Children);
				query.ToArray();

				var sql = query.ToSqlQuery().Sql;

				Assert.That(sql, Does.Not.Contain("LoadWithQueryable"));

				// two queries generated, now returns sql for main query
				CompareSql(@"SELECT
	[t1].[ParentID],
	[t1].[Value1]
FROM
	[Parent] [t1]", sql);
			}
		}

		[Test]
		public void TestLoadWithToString2([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Person.LoadWith(p => p.Patient).AsQueryable();
			query.ToArray();

			var sql = query.ToSqlQuery().Sql;

			sql.Should().NotContain("LoadWithQueryable");

			var select = query.GetSelectQuery();

				// one query with join generated

			select.From.Tables.Should().HaveCount(1);
			select.From.Tables[0].Joins.Should().HaveCount(1);
			select.From.Tables[0].Joins[0].JoinType.Should().Be(JoinType.Left);
		}

		[Test]
		public void TestLoadWithDeep([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords, subDetailRecords) = GenerateDataWithSubDetail();
			var intParam = 1;

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			using (var subDetail = db.CreateLocalTable(subDetailRecords))
			{
				var query = from m in master.LoadWith(m => m.Details).LoadWith(m => m.Details[0].SubDetails)
					where m.Id1 >= intParam
					select m;

				var expectedQuery = from m in masterRecords
					where m.Id1 >= intParam
					select new MasterClass
					{
						Id1 = m.Id1,
						Id2 = m.Id2,
						Value = m.Value,
						Details = detailRecords.Where(d => d.MasterId == m.Id1).Select(d => new DetailClass
						{
							DetailId = d.DetailId,
							DetailValue = d.DetailValue,
							MasterId = d.MasterId,
							SubDetails = subDetailRecords.Where(s => s.DetailId == d.DetailId).ToArray()
						}).ToList(),
					};

				var result = query.ToList();
				var expected = expectedQuery.ToList();

				foreach (var item in result.Concat(expected))
				{
					item.Details = item.Details.OrderBy(_ => _.DetailId).ToList();
					foreach (var subItem in item.Details)
						subItem.SubDetails = subItem.SubDetails.OrderBy(_ => _.SubDetailId).ToArray();
				}

				AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
			}
		}

		[Test]
		public void TestMethodMappedProjection([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords, subDetailRecords) = GenerateDataWithSubDetail();
			var intParam = 1;

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			using (var subDetail = db.CreateLocalTable(subDetailRecords))
			{
				var query = from m in master
					where m.Id1 >= intParam
					select new 
					{
						Id1 = m.Id1,
						Id2 = m.Id2,
						Value = m.Value,
						Details = m.Details.Select(d => new 
						{
							DetailId = d.DetailId,
							DetailValue = d.DetailValue,
							MasterId = d.MasterId,
							SubDetails = MakeDTO(d.SubDetails.AsQueryable()).ToArray()
						}).ToList(),
					};;

				var result = query.ToList();
			}
		}

		[Test]
		public void TestSelectProjectionList([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();
			var intParam = 0;

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			{
				var query = from m in master
					where m.Id1 >= intParam
					select new
					{
						MId = m.Id1,
						Details1 = detail.InnerJoin(d => d.MasterId == m.Id1 && d.MasterId == m.Id2).ToList(),
						Details2 = detail.InnerJoin(d => d.MasterId == m.Id1 && d.MasterId % 2 == 0).ToList()
					};

				var result = query.ToArray();
			}
		}

		[Test]
		public async Task TestSelectProjectionListAsync([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();
			var intParam = 0;

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			{
				var query = from m in master
					where m.Id1 >= intParam
					select new
					{
						MId = m.Id1,
						MId2 = m.Id2,
						Details1 = detail.InnerJoin(d => d.MasterId == m.Id1 && d.MasterId == m.Id2).ToList(),
						Details2 = detail.InnerJoin(d => d.MasterId == m.Id1 && d.MasterId % 2 == 0).ToArray(),
					};

				var result = await query.ToArrayAsync();
			}
		}

		[Test]
		public async Task TestSelectAssociationProjectionListAsync([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();
			var intParam = 0;

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			{
				var query = from m in master
					orderby m.Id2 descending 
					where m.Id1 >= intParam
					select new
					{
						IdSum = m.Id1 + 100,
						Association1 = m.Details.ToArray(),
						Association2 = m.Details.Where(d => d.DetailId % 2 == 0).ToArray(),
						Association3 = m.Details.Where(d => d.DetailId % 2 == 0).Select(d => d.DetailId).ToArray(),
						Association4 = m.Details.Where(d => d.DetailId % 2 == 0).ToDictionary(d => d.DetailId),
					};

				var result = await query.ToArrayAsync();
			}
		}

		[Test]
		public void TestWhenMasterConnectedViaExpression([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var (masterRecords, detailRecords) = GenerateDataManyId();

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			{
				var masterQuery = from m in master
					group m by m.Id1
					into g
					select new
					{
						Count = g.Count(),
						Details1 = detail.Where(d => d.MasterId == g.Key).ToArray(),
						Details2 = detail.Where(d => d.MasterId > g.Key).ToArray()
					};

				var result = masterQuery.ToArray();
			}
		}

		[Test]
		public void TestQueryableAssociation([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			{
				var masterQuery = from m in master
					where m.Id1 > 5
					select new
					{
						m.Id1,
						Details1 = m.DetailsQuery,
						Details2 = m.DetailsQuery.Where(d => d.DetailId % 2 == 0).ToArray(),
						Details3 = m.DetailsQuery.Where(d => d.DetailId % 2 == 0).Select(c => c.DetailValue).ToArray(),
					};

				var result = masterQuery.ToArray();
			}
		}

		[Test]
		public void TestRecursive([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords, subDetailRecords) = GenerateDataWithSubDetail();

			var masterFilter = 5;
			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			using (var subDetail = db.CreateLocalTable(subDetailRecords))
			{
				var masterQuery = from master_1 in master
					where master_1.Id1 > masterFilter
					select new
					{
						master_1.Id1,
						Details = detail.Where(d_1 => d_1.MasterId == master_1.Id1).Select(masterP_1 => new
						{
							SubDetails = subDetail.Where(d_b => d_b.DetailId == masterP_1.DetailId).ToArray(),
							Another = masterP_1.SubDetails
						}).ToArray()
					};

				var expectedQuery = from master_1 in masterRecords
					where master_1.Id1 > masterFilter
					select new
					{
						master_1.Id1,
						Details = detailRecords.Where(d_1 => d_1.MasterId == master_1.Id1).Select(masterP_1 => new
						{
							SubDetails = subDetailRecords.Where(d_b => d_b.DetailId == masterP_1.DetailId).ToArray(),
							Another = subDetailRecords.Where(d_b => d_b.DetailId == masterP_1.DetailId).ToArray()
						}).ToArray()
					};

				var result   = masterQuery.ToArray();
				var expected = expectedQuery.ToArray();

				result   = result  .Select(_ => new { _.Id1, Details = _.Details.Select(_ => new { SubDetails = _.SubDetails.OrderBy(_ => _.SubDetailId).ToArray(), Another = _.Another.OrderBy(_ => _.SubDetailId).ToArray() }).OrderBy(_ => _.SubDetails.First().DetailId).ToArray() }).ToArray();
				expected = expected.Select(_ => new { _.Id1, Details = _.Details.Select(_ => new { SubDetails = _.SubDetails.OrderBy(_ => _.SubDetailId).ToArray(), Another = _.Another.OrderBy(_ => _.SubDetailId).ToArray() }).OrderBy(_ => _.SubDetails.First().DetailId).ToArray() }).ToArray();

				AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
			}
		}

		[Test]
		public void TestWhenMasterIsNotConnected([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords) = GenerateDataManyId();

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			{
				var masterQuery = from m in master.Take(11)
					group m by m.Id1
					into g
					select new
					{
						Count = g.Count(),
						Details = detail.ToArray()
					};

				var expectedQuery = from m in masterRecords.Take(11)
					group m by m.Id1
					into g
					select new
					{
						Count = g.Count(),
						Details = detailRecords.ToArray()
					};

				var result = masterQuery.ToArray();
				var expected = expectedQuery.ToArray();

				AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(result));
			}
		}

		[Test]
		public void TestSelectMany([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords, subDetailRecords) = GenerateDataWithSubDetail();

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			using (var subDetails = db.CreateLocalTable(subDetailRecords))
			{
				var query = from m in master.Take(20)
					from d in detail
					select new
					{
						Detail = d,
						SubDetails = subDetails.Where(sd => sd.DetailId == d.DetailId).ToArray(),
						SubDetailsAssocaited = d.SubDetails
					};

				var expectedQuery = from m in masterRecords.Take(20)
					from d in detailRecords
					select new
					{
						Detail = d,
						SubDetails = subDetailRecords.Where(sd => sd.DetailId == d.DetailId).ToArray(),
						SubDetailsAssocaited = subDetailRecords.Where(sd => sd.DetailId == d.DetailId).ToArray()
					};

				var result   = query.ToArray();
				var expected = expectedQuery.ToArray();

				result   = result  .Select(_ => new { _.Detail, SubDetails = _.SubDetails.OrderBy(_ => _.SubDetailId).ToArray(), SubDetailsAssocaited = _.SubDetailsAssocaited.OrderBy(_ => _.SubDetailId).ToArray() }).ToArray();
				expected = expected.Select(_ => new { _.Detail, SubDetails = _.SubDetails.OrderBy(_ => _.SubDetailId).ToArray(), SubDetailsAssocaited = _.SubDetailsAssocaited.OrderBy(_ => _.SubDetailId).ToArray() }).ToArray();

				AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(result));
			}
		}

		[ActiveIssue("https://github.com/linq2db/linq2db/issues/3619", Configuration = TestProvName.AllClickHouse)]
		[Test]
		public void TestJoin([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			{
				var query = from m in master.Take(20)
					join d in detail on m.Id1 equals d.MasterId 
					select new
					{
						Detail = d,
						Masters = master.Where(mm => m.Id1 == d.MasterId).ToArray()
					};

				var expectedQuery = from m in masterRecords.Take(20)
					join d in detailRecords on m.Id1 equals d.MasterId 
					select new
					{
						Detail = d,
						Masters = masterRecords.Where(mm => m.Id1 == d.MasterId).ToArray()
					};

				var result   = query.ToArray();
				var expected = expectedQuery.ToArray();

				AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(result));
			}
		}

		[Test]
		public void TestPureGroupJoin([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			{
				var query = master.Take(20)
					.GroupJoin(detail, m => m.Id1, d => d.MasterId,
						(m1, d) => new { Master = m1, Details = d.ToArray() });

				var expectedQuery = masterRecords.Take(20)
					.GroupJoin(detailRecords, m => m.Id1, d => d.MasterId,
						(m1, d) => new { Master = m1, Details = d.ToArray() });

				var result   = query.ToArray();
				var expected = expectedQuery.ToArray();

				expected = expected.Select(_ => new { _.Master, Details = _.Details.OrderBy(_ => _.DetailId).ToArray() }).ToArray();
				result   = result  .Select(_ => new { _.Master, Details = _.Details.OrderBy(_ => _.DetailId).ToArray() }).ToArray();

				AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(result));
			}
		}

		[ActiveIssue("https://github.com/linq2db/linq2db/issues/3619", Configuration = TestProvName.AllClickHouse)]
		[Test]
		public void TestGroupJoin([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords, subDetailRecords) = GenerateDataWithSubDetail();

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			using (var subDetail = db.CreateLocalTable(subDetailRecords))
			{
				var query = from m in master.OrderByDescending(m => m.Id2).Take(20)
					join d in detail on m.Id1 equals d.MasterId into j
					from dd in j
					select new
					{
						Master = m,
						Detail = dd,
						DetailAssociated = dd.SubDetails,
						DetailAssociatedFiltered = dd.SubDetails.OrderBy(sd => sd.SubDetailValue).Take(10).ToArray(),
						Masters = master.Where(mm => mm.Id1 == dd.MasterId).OrderBy(mm => mm.Value).Take(10).ToArray()
					};

				var expectedQuery = from m in masterRecords.OrderByDescending(m => m.Id2).Take(20)
					join d in detailRecords on m.Id1 equals d.MasterId into j
					from dd in j
					select new
					{
						Master = m,
						Detail = dd,
						DetailAssociated = subDetailRecords.Where(sd => sd.DetailId == dd.DetailId).ToArray(),
						DetailAssociatedFiltered = subDetailRecords.OrderBy(sd => sd.SubDetailValue).Where(sd => sd.DetailId == dd.DetailId).Take(10).ToArray(),
						Masters = masterRecords.Where(mm => mm.Id1 == dd.MasterId).OrderBy(mm => mm.Value).Take(10).ToArray()
					};

				var result   = query.ToArray();
				var expected = expectedQuery.ToArray();

				AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(result));
			}
		}

		[Test]
		public void TestDeepGroupJoin([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords, subDetailRecords) = GenerateDataWithSubDetail();

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			using (var subDetail = db.CreateLocalTable(subDetailRecords))
			{
				var query = master.OrderByDescending(m => m.Id2)
					.Take(20)
					.GroupJoin(detail, m => m.Id1, d => d.MasterId, (m, ds) => new { m, ds })
					.GroupJoin(master, dd => dd.m.Id1, mm => mm.Id1, (dd, mm) =>
						new
						{
							dd.m.Id1,
							Details = dd.ds.ToArray(),
							Masters = mm.ToArray()
						}
					);

				var expectedQuery = masterRecords.OrderByDescending(m => m.Id2)
					.Take(20)
					.GroupJoin(detailRecords, m => m.Id1, d => d.MasterId, (m, ds) => new { m, ds })
					.GroupJoin(masterRecords, dd => dd.m.Id1, mm => mm.Id1, (dd, mm) =>
						new
						{
							dd.m.Id1,
							Details = dd.ds.ToArray(),
							Masters = mm.ToArray()
						}
					);

				var result   = query.ToArray();
				var expected = expectedQuery.ToArray();

				expected = expected.Select(_ => new { _.Id1, Details = _.Details.OrderBy(_ => _.DetailId).ToArray(), Masters = _.Masters.OrderBy(_ => _.Id2).ToArray() }).ToArray();
				result   = result  .Select(_ => new { _.Id1, Details = _.Details.OrderBy(_ => _.DetailId).ToArray(), Masters = _.Masters.OrderBy(_ => _.Id2).ToArray() }).ToArray();

				AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(result));
			}
		}

		[Test]
		public void TestDeepJoin([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords, subDetailRecords) = GenerateDataWithSubDetail();

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			using (var subDetail = db.CreateLocalTable(subDetailRecords))
			{
				var query = master.OrderByDescending(m => m.Id2)
					.Take(20)
					.GroupJoin(detail, m => m.Id1, d => d.MasterId, (m, ds) => new { m, ds })
					.Join(master, dd => dd.m.Id1, mm => mm.Id1, (dd, mm) =>
						new
						{
							dd.m.Id1,
							Details = dd.ds.ToArray(),
							Master = mm
						}
					);

				var expectedQuery = masterRecords.OrderByDescending(m => m.Id2)
					.Take(20)
					.GroupJoin(detailRecords, m => m.Id1, d => d.MasterId, (m, ds) => new { m, ds })
					.Join(masterRecords, dd => dd.m.Id1, mm => mm.Id1, (dd, mm) =>
						new
						{
							dd.m.Id1,
							Details = dd.ds.ToArray(),
							Master = mm
						}
					);

				var result   = query.ToArray();
				var expected = expectedQuery.ToArray();

				expected = expected.Select(_ => new { _.Id1, Details = _.Details.OrderBy(_ => _.DetailId).ToArray(), _.Master }).ToArray();
				result   = result  .Select(_ => new { _.Id1, Details = _.Details.OrderBy(_ => _.DetailId).ToArray(), _.Master }).ToArray();

				AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(result));
			}
		}

		[Test]
		public void TestSubSelect([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords, subDetailRecords) = GenerateDataWithSubDetail();

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			{
				var query = master.OrderByDescending(m => m.Id2)
					.Take(20)
					.Select(m => new { Master = m })
					.Distinct();

				var result = query.Select(e => new
				{
					e.Master,
					Details = e.Master.Details.Select(d => new { d.DetailId, d.DetailValue }).ToArray()
				}).ToArray();

				var expectedQuery = masterRecords.OrderByDescending(m => m.Id2)
					.Take(20)
					.Select(m => new { Master = m })
					.Distinct();

				var expected = expectedQuery.Select(e => new
				{
					e.Master,
					Details = detailRecords.Where(dr => dr.MasterId == e.Master.Id1).Select(d => new { d.DetailId, d.DetailValue }).ToArray()
				}).ToArray();

				result   = result .Select(_ => new { _.Master, Details = _.Details.OrderBy(_ => _.DetailId).ToArray() }).ToArray();
				expected = expected.Select(_ => new { _.Master, Details = _.Details.OrderBy(_ => _.DetailId).ToArray() }).ToArray();

				AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(result));
			}
		}

		[Test]
		public void TestSelectGroupBy([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			{
				var query = from m in master.OrderByDescending(m => m.Id2).Take(20)
					join d in detail on m.Id1 equals d.MasterId into j
					from dd in j
					select new
					{
						Master = m,
						Detail = dd,
						FirstMaster = master.Where(mm => mm.Id1 == dd.MasterId)
							.AsEnumerable()
							.GroupBy(_ => _.Id1)
							.Select(_ => _.OrderBy(mm => mm.Id1).First())
					};

				var expectedQuery = from m in masterRecords.OrderByDescending(m => m.Id2).Take(20)
					join d in detailRecords on m.Id1 equals d.MasterId into j
					from dd in j
					select new
					{
						Master = m,
						Detail = dd,
						FirstMaster = masterRecords.Where(mm => mm.Id1 == dd.MasterId)
							.GroupBy(_ => _.Id1)
							.Select(_ => _.OrderBy(mm => mm.Id1).First())
					};

				var result   = query.ToArray();
				var expected = expectedQuery.ToArray();

				AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(result));
			}
		}

		[Test]
		public void TestTupleQueryingFabric([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			{
				var query1 = from m in master
						select Tuple.Create(m, m.Id1);

				var query2 = from q in query1
					where q.Item2 > 5 && q.Item1.Id2 > 5
					select q.Item1;

				var result = query2.ToArray();
			}
		}

		[Test]
		public void TestTupleQueryingNew([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			{
				var query1 = from m in master
					select new Tuple<MasterClass, int>(m, m.Id1);

				var query2 = from q in query1
					where q.Item2 > 5 && q.Item1.Id2 > 5
					select q.Item1;

				var result = query2.ToArray();
			}
		}

		[Test]
		public void TestCorrectFilteringMembers([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			{
				var query1 = master.Select(e => new { e.Id1, e.Value, e.ByteValues });
				var query2 = master.Select(e => new { e.Id1, Value = (string?)"Str", e.ByteValues });

				var concated = query1.Concat(query2);

				var query = concated.Select(e1 => new
				{
					e1.Id1,
					e1.Value,
					e1.ByteValues
				});

				var result = query.ToArray(); 

				var equery1 = masterRecords.Select(e => new { e.Id1, e.Value, e.ByteValues });
				var equery2 = masterRecords.Select(e => new { e.Id1, Value = (string?)"Str", e.ByteValues });

				var econcated = equery1.Concat(equery2);

				var equery = econcated.Select(e1 => new
				{
					e1.Id1,
					e1.Value,
					e1.ByteValues
				});

				var expected = equery.ToArray();

				AreEqual(expected, result);
			}
		}

		private static X InitData<X>(X entity) => entity; // for simplicity

		[Test]
		public void ProjectionWithExtension([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			{
				var result = master.LoadWith(x => x.Details).Select(x => InitData(x))
					.ToArray();
				var result2 = master.LoadWith(x => x.Details).Select(x => InitData(x)).Select(x => new { x = InitData(x)})
					.ToArray();

				Assert.That(result, Has.Length.EqualTo(result2.Length));
			}
		}

		[Test]
		public void ProjectionWithoutClass([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			{
				var query = master.Select(x => new
					{
						Details = x.Details.Select(d => d.DetailValue)
					});

				var result = query.Select(m => m.Details).ToList();

				var expectedQuery = masterRecords.Select(x => new
				{
					Details = detailRecords.Where(d => d.MasterId == x.Id1).Select(d => d.DetailValue)
				});

				var expected = expectedQuery.Select(m => m.Details).ToList();

				AreEqual(expected, result);
			}
		}

		[Test]
		public void FirstSingleWithFilter([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			{
				var query = master.Select(x => new
				{
					x.Id1,
					Details = x.Details.Select(d => d.DetailValue)
				});

				FluentActions.Invoking(() => query.FirstOrDefault(x => x.Id1 == 1)).Should().NotThrow();
				FluentActions.Invoking(() => query.First(x => x.Id1          == 1)).Should().NotThrow();
				FluentActions.Invoking(() => query.Single(x => x.Id1         == 1)).Should().NotThrow();
			}
		}

		[Test]
		public async Task FirstSingleWithFilterAsync([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			{
				var query = master.Select(x => new
				{
					x.Id1,
					Details = x.Details.Select(d => d.DetailValue)
				});

				await FluentActions.Awaiting(() => query.FirstOrDefaultAsync(x => x.Id1 == 1)).Should().NotThrowAsync();
				await FluentActions.Awaiting(() => query.FirstAsync(x => x.Id1          == 1)).Should().NotThrowAsync();
				await FluentActions.Awaiting(() => query.SingleAsync(x => x.Id1         == 1)).Should().NotThrowAsync();
			}
		}

		[Test]
		public void TestSkipTake([DataSources] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			{
				var query = from m in master.LoadWith(m => m.Details)
					select new
					{
						m,
						details = m.Details.OrderBy(d => d.DetailId).Skip(1).Take(2).ToList()
					};

				AssertQuery(query);
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, ErrorMessage = ErrorHelper.Error_Correlated_Subqueries)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, TestProvName.AllFirebirdLess4, TestProvName.AllMySql57, TestProvName.AllSybase, TestProvName.AllOracle11, TestProvName.AllMariaDB, TestProvName.AllDB2, TestProvName.AllInformix, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void TestAggregate([DataSources] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			{
				var query = from m in master.LoadWith(m => m.Details)
					select new
					{
						Sum = m.Details.Select(x => x.DetailId)
							.Distinct()
							.OrderBy(x => x)
							.Skip(1).Take(5)
							.Sum(),

						Count = m.Details.Select(x => x.DetailValue)
							.Distinct()
							.OrderBy(x => x)
							.Skip(1).Take(2)
							.Count()
					};

				AssertQuery(query);
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, ErrorMessage = ErrorHelper.Error_Correlated_Subqueries)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, TestProvName.AllFirebirdLess4, TestProvName.AllMySql57, TestProvName.AllSybase, TestProvName.AllOracle11, TestProvName.AllMariaDB, TestProvName.AllDB2, TestProvName.AllInformix, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void TestAggregateAverage([DataSources] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();

			using (var db = GetDataContext(context))
			using (var master = db.CreateLocalTable(masterRecords))
			using (var detail = db.CreateLocalTable(detailRecords))
			{
				var query = from m in master.LoadWith(m => m.Details)
					where m.Details.Count() > 1
					select new
					{
						Average = m.Details.Select(x => x.DetailId)
							.Distinct()
							.OrderBy(x => x)
							.Skip(1).Take(5)
							.Average(x => (double)x),
					};

				AssertQuery(query);
			}
		}

		#region issue 1862
		[Table]
		public partial class Blog
		{
			[Column] public int     Id     { get; set; }
			[Column] public string? Title  { get; set; }
			[Column] public string? Slogan { get; set; }
			[Column] public string? UserId { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Post.BlogId))]
			public virtual ICollection<Post> Posts { get; set; } = null!;

			public static readonly Blog[] Data = new[]
			{
				new Blog() { Id = 1, Title = "Another .NET Core Guy", Slogan = "Doing .NET Core Stuff", UserId = TestData.Guid1.ToString("N") }
			};
		}

		[Table]
		public partial class Post
		{
			[Column] public int     Id          { get; set; }
			[Column] public int     BlogId      { get; set; }
			[Column] public string? Title       { get; set; }
			[Column] public string? PostContent { get; set; }
			[Column] public bool    IsDeleted   { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(PostTag.PostId), CanBeNull = true)]
			public virtual ICollection<PostTag> PostTags { get; set; } = null!;

			public static readonly Post[] Data = new[]
			{
				new Post() { Id = 1, BlogId = 1, Title = "Post 1", PostContent = "Content 1 is about EF Core and Razor page", IsDeleted = false },
				new Post() { Id = 2, BlogId = 1, Title = "Post 2", PostContent = "Content 2 is about Dapper", IsDeleted = false },
				new Post() { Id = 3, BlogId = 1, Title = "Post 3", PostContent = "Content 3", IsDeleted = true },
				new Post() { Id = 4, BlogId = 1, Title = "Post 4", PostContent = "Content 4", IsDeleted = false },
			};
		}

		[Table]
		public partial class Tag
		{
			[Column] public int     Id        { get; set; }
			[Column] public string? Name      { get; set; }
			[Column] public bool    IsDeleted { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(PostTag.TagId), CanBeNull = true)]
			public virtual ICollection<PostTag> PostTags { get; set; } = null!;

			public static readonly Tag[] Data = new[]
			{
				new Tag() { Id = 1, Name = "Razor Page", IsDeleted = false },
				new Tag() { Id = 2, Name = "EF Core", IsDeleted = false },
				new Tag() { Id = 3, Name = "Dapper", IsDeleted = false },
				new Tag() { Id = 4, Name = "Slapper Dapper", IsDeleted = false },
				new Tag() { Id = 5, Name = "SqlKata", IsDeleted = true },
			};
		}

		[Table]
		public partial class PostTag
		{
			[Column] public int  Id        { get; set; }
			[Column] public int  PostId    { get; set; }
			[Column] public int  TagId     { get; set; }
			[Column] public bool IsDeleted { get; set; }

			[Association(ThisKey = nameof(PostId), OtherKey = nameof(EagerLoadingTests.Post.Id), CanBeNull = false)]
			public virtual Post Post { get; set; } = null!;
			[Association(ThisKey = nameof(TagId), OtherKey = nameof(EagerLoadingTests.Tag.Id), CanBeNull = false)]
			public virtual Tag  Tag  { get; set; } = null!;

			public static readonly PostTag[] Data = new[]
			{
				new PostTag() { Id = 1, PostId = 1, TagId = 1, IsDeleted = false },
				new PostTag() { Id = 2, PostId = 1, TagId = 2, IsDeleted = false },
				new PostTag() { Id = 3, PostId = 2, TagId = 3, IsDeleted = false },
				new PostTag() { Id = 4, PostId = 4, TagId = 5, IsDeleted = false },
			};
		}

		[Test]
		public void Issue1862TestProjections([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db      = GetDataContext(context))
			using (var blog    = db.CreateLocalTable(Blog.Data))
			using (var post    = db.CreateLocalTable(Post.Data))
			using (var tage    = db.CreateLocalTable(Tag.Data))
			using (var postTag = db.CreateLocalTable(PostTag.Data))
			{
				var blogId = 1;
				var query = blog.Where(b => b.Id == blogId).Select(b => new
				{
					b.Id,
					b.Title,
					Posts = b.Posts.Select(p => new
					{
						p.Id,
						p.Title,
						p.PostContent,
						Tags = p.PostTags.Where(pp => !pp.IsDeleted).Select(t => new
						{
							Id = t.TagId,
							t.Tag.Name
						}).OrderBy(t => t.Id).ToArray()
					}).OrderBy(op => op.Id).ToArray()
				});

				var result = new
				{
					Blog = query.ToArray()
				};

				Assert.That(result.Blog, Has.Length.EqualTo(1));
				Assert.Multiple(() =>
				{
					Assert.That(result.Blog[0].Id, Is.EqualTo(1));
					Assert.That(result.Blog[0].Title, Is.EqualTo("Another .NET Core Guy"));
					Assert.That(result.Blog[0].Posts, Has.Length.EqualTo(4));
				});

				Assert.Multiple(() =>
				{
					Assert.That(result.Blog[0].Posts[0].Id, Is.EqualTo(1));
					Assert.That(result.Blog[0].Posts[0].Title, Is.EqualTo("Post 1"));
					Assert.That(result.Blog[0].Posts[0].PostContent, Is.EqualTo("Content 1 is about EF Core and Razor page"));
					Assert.That(result.Blog[0].Posts[0].Tags, Has.Length.EqualTo(2));
				});
				Assert.Multiple(() =>
				{
					Assert.That(result.Blog[0].Posts[0].Tags[0].Id, Is.EqualTo(1));
					Assert.That(result.Blog[0].Posts[0].Tags[0].Name, Is.EqualTo("Razor Page"));
					Assert.That(result.Blog[0].Posts[0].Tags[1].Id, Is.EqualTo(2));
					Assert.That(result.Blog[0].Posts[0].Tags[1].Name, Is.EqualTo("EF Core"));

					Assert.That(result.Blog[0].Posts[1].Id, Is.EqualTo(2));
					Assert.That(result.Blog[0].Posts[1].Title, Is.EqualTo("Post 2"));
					Assert.That(result.Blog[0].Posts[1].PostContent, Is.EqualTo("Content 2 is about Dapper"));
					Assert.That(result.Blog[0].Posts[1].Tags, Has.Length.EqualTo(1));
				});
				Assert.Multiple(() =>
				{
					Assert.That(result.Blog[0].Posts[1].Tags[0].Id, Is.EqualTo(3));
					Assert.That(result.Blog[0].Posts[1].Tags[0].Name, Is.EqualTo("Dapper"));

					Assert.That(result.Blog[0].Posts[2].Id, Is.EqualTo(3));
					Assert.That(result.Blog[0].Posts[2].Title, Is.EqualTo("Post 3"));
					Assert.That(result.Blog[0].Posts[2].PostContent, Is.EqualTo("Content 3"));
					Assert.That(result.Blog[0].Posts[2].Tags, Is.Empty);

					Assert.That(result.Blog[0].Posts[3].Id, Is.EqualTo(4));
					Assert.That(result.Blog[0].Posts[3].Title, Is.EqualTo("Post 4"));
					Assert.That(result.Blog[0].Posts[3].PostContent, Is.EqualTo("Content 4"));
					Assert.That(result.Blog[0].Posts[3].Tags, Has.Length.EqualTo(1));
				});
				Assert.Multiple(() =>
				{
					Assert.That(result.Blog[0].Posts[3].Tags[0].Id, Is.EqualTo(5));
					Assert.That(result.Blog[0].Posts[3].Tags[0].Name, Is.EqualTo("SqlKata"));
				});
			}
		}
#endregion

#region issue 2196
		public class EventScheduleItemBase
		{
			public EventScheduleItemBase()
			{
			}

			[PrimaryKey]
			[Column] public int  Id                        { get; set; }
			[Column] public int  EventId                   { get; set; }
			[Column] public bool IsActive                  { get; set; } = true;
			[Column] public int? ParentEventScheduleItemId { get; set; }
		}

		[Table]
		public class EventScheduleItem : EventScheduleItemBase
		{
			public EventScheduleItem()
			{
				Persons = new List<EventScheduleItemPerson>();
				ChildSchedules = new List<EventScheduleItem>();
			}

			[Association(ThisKey = nameof(ParentEventScheduleItemId), OtherKey = nameof(Id))]
			public virtual EventScheduleItem? ParentSchedule { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = nameof(ParentEventScheduleItemId))]
			public virtual List<EventScheduleItem> ChildSchedules { get; set; } = null!;
			[Association(ThisKey = nameof(Id), OtherKey = nameof(EventScheduleItemPerson.EventScheduleItemId))]
			public virtual List<EventScheduleItemPerson> Persons { get; set; } = null!;

			public static EventScheduleItem[] Items { get; } =
				new[]
				{
					new EventScheduleItem() { Id = 1, EventId = 1, IsActive = true, ParentEventScheduleItemId = 1 },
					new EventScheduleItem() { Id = 2, EventId = 2, IsActive = true, ParentEventScheduleItemId = 2 }
				};
		}

		[Table]
		public class EventScheduleItemPerson
		{
			[Column] public int Id                    { get; set; }
			[Column] public int EventSchedulePersonId { get; set; }
			[Column] public int EventScheduleItemId   { get; set; }

			[Association(ThisKey = nameof(EventSchedulePersonId), OtherKey = nameof(EventSchedulePerson.Id))]
			public virtual EventSchedulePerson Person { get; set; } = null!;
			[Association(ThisKey = nameof(EventScheduleItemId), OtherKey = nameof(EventScheduleItem.Id))]
			public virtual EventScheduleItem ScheduleItem { get; set; } = null!;

			public static EventScheduleItemPerson[] Items { get; } =
				new[]
				{
					new EventScheduleItemPerson() { Id = 1, EventSchedulePersonId = 1, EventScheduleItemId = 1 },
					new EventScheduleItemPerson() { Id = 2, EventSchedulePersonId = 2, EventScheduleItemId = 2 }
				};
		}

		[Table]
		public class EventSchedulePerson
		{
			public EventSchedulePerson()
			{
				EventScheduleItemPersons = new List<EventScheduleItemPerson>();
			}

			[Column] public int  Id             { get; set; }
			[Column] public int? TicketNumberId { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(EventScheduleItemPerson.EventSchedulePersonId))]
			public virtual ICollection<EventScheduleItemPerson> EventScheduleItemPersons { get; set; }

			public static EventSchedulePerson[] Items { get; } =
				new[]
				{
					new EventSchedulePerson() { Id = 1, TicketNumberId = 1 },
					new EventSchedulePerson() { Id = 2, TicketNumberId = 2 }
				};
		}

		public class EventScheduleListModel : EventScheduleItemBase
		{
			public List<EventScheduleListPersonModel> Persons { get; set; } = new List<EventScheduleListPersonModel>();
		}

		public class EventScheduleListPersonModel
		{
			public int  Id                    { get; set; }
			public int  EventSchedulePersonId { get; set; }
			public int? TicketNumberId        { get; set; }
		}

		[Test]
		public void Issue2196([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(EventScheduleItem.Items))
			using (db.CreateLocalTable(EventScheduleItemPerson.Items))
			using (db.CreateLocalTable(EventSchedulePerson.Items))
			{
				var eventId = 1;

				var query = db.GetTable<EventScheduleItem>()
					.Where(p => p.EventId == eventId && p.IsActive)
					.Select(p => new EventScheduleListModel()
					{
						Id      = p.Id,
						Persons = p.Persons.Select(pp => new EventScheduleListPersonModel()
						{
							EventSchedulePersonId = pp.EventSchedulePersonId,
							Id                    = pp.Id,
							TicketNumberId        = pp.Person.TicketNumberId
						}).ToList()
					});

				var result = query.ToList();

				Assert.That(result, Has.Count.EqualTo(1));
				Assert.That(result[0].Persons, Has.Count.EqualTo(1));
			}
		}
#endregion

#region issue 2307
		[Table]
		sealed class AttendanceSheet
		{
			[PrimaryKey]
			[Column] public int Id;

			public static AttendanceSheet[] Items { get; } =
				new[]
				{
					new AttendanceSheet() { Id = 1 },
					new AttendanceSheet() { Id = 2 }
				};
		}

		[Table]
		sealed class AttendanceSheetRow
		{
			[Column] public int Id;
			[Column] public int AttendanceSheetId;

			public static AttendanceSheetRow[] Items { get; } =
				new[]
				{
					new AttendanceSheetRow() { Id = 1, AttendanceSheetId = 1 },
					new AttendanceSheetRow() { Id = 2, AttendanceSheetId = 2 },
					new AttendanceSheetRow() { Id = 3, AttendanceSheetId = 1 },
					new AttendanceSheetRow() { Id = 4, AttendanceSheetId = 2 },
				};
		}

		sealed class AttendanceSheetDTO
		{
			public List<AttendanceSheetRowListModel> Rows = null!;
		}

		sealed class AttendanceSheetRowListModel
		{
			public AttendanceSheetRowListModel(AttendanceSheetRow row)
			{
				AttendanceSheetId = row.AttendanceSheetId;
			}

			public int AttendanceSheetId;
		}

		[Test]
		public void Issue2307([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (var sheets = db.CreateLocalTable(AttendanceSheet.Items))
			using (var sheetRows   = db.CreateLocalTable(AttendanceSheetRow.Items))
			{
				var query = from sheet in sheets
							join row in sheetRows on sheet.Id equals row.AttendanceSheetId into rows
							select new AttendanceSheetDTO()
							{
								Rows = rows.Select(x => new AttendanceSheetRowListModel(x)).ToList(),
							};

				query.ToList();
			}
		}
		#endregion

		#region issue 3128

		[Table]
		sealed class UserIssue3128
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(UserDetailsIssue3128.UserId), CanBeNull = true)]
			public UserDetailsIssue3128? Details { get; set; }
		}

		[Table]
		sealed class UserDetailsIssue3128
		{
			[PrimaryKey] public int UserId { get; set; }
			[Column] public int Age { get; set; }
		}

		[Test]
		public void TableExpressionAfterLoadWithTable([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<UserIssue3128>())
			using (db.CreateLocalTable<UserDetailsIssue3128>())
			{
				db.Insert(new UserIssue3128 { Id = 10 });
				db.Insert(new UserDetailsIssue3128 { UserId = 10, Age = 18 });

				var result = db.GetTable<UserIssue3128>()
					.LoadWithAsTable( _ => _.Details)
					.WithTableExpression($"{{0}} {{1}}")
					.ToList();

				Assert.That(result, Has.Count.EqualTo(1));
			}
		}

		[Test]
		public void TableExpressionFirst([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<UserIssue3128>())
			using (db.CreateLocalTable<UserDetailsIssue3128>())
			{
				db.Insert(new UserIssue3128 { Id = 10 });
				db.Insert(new UserDetailsIssue3128 { UserId = 10, Age = 18 });

				var result = db.GetTable<UserIssue3128>()
					.WithTableExpression($"{{0}} {{1}}")
					.LoadWithAsTable( _ => _.Details)
					.ToList();

				Assert.That(result, Has.Count.EqualTo(1));
			}
		}

		[Test]
		public void WithTableAttributeMethods([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<UserIssue3128>())
			using (db.CreateLocalTable<UserDetailsIssue3128>())
			{
				db.Insert(new UserIssue3128 { Id = 10 });
				db.Insert(new UserDetailsIssue3128 { UserId = 10, Age = 18 });

				db.Person.Where(p => db.GetTable<UserIssue3128>()
					.LoadWithAsTable(_ => _.Details)
					.SchemaName(null).Count() > 0).ToList();
			}
		}
		#endregion

		#region Issue 3664

		[Table]
		public class Test3664
		{
			[PrimaryKey] public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Test3664Item.TestId))]
			public List<Test3664Item> Items { get; set; } = null!;
		}

		[Table]
		public class Test3664Item
		{
			[PrimaryKey] public int Id     { get; set; }
			[Column    ] public int TestId { get; set; }
		}

		[Test]
		public void Issue3664Test([DataSources] string context)
		{
			using var db = GetDataContext(context);

			using var records = db.CreateLocalTable<Test3664>();
			db.Insert(new Test3664() { Id = 1 });
			using var items = db.CreateLocalTable(new[]
			{
				new Test3664Item() { Id = 11, TestId = 1 },
				new Test3664Item() { Id = 12, TestId = 1 }
			});

			var id = 11;
			var result = records.LoadWith(a => a.Items, a => a.Where(a => a.Id == id)).ToList();
			Assert.That(result, Has.Count.EqualTo(1));
			Assert.Multiple(() =>
			{
				Assert.That(result[0].Id, Is.EqualTo(1));
				Assert.That(result[0].Items, Has.Count.EqualTo(1));
			});
			Assert.That(result[0].Items[0].Id, Is.EqualTo(11));

			id = 12;
			result = records.LoadWith(a => a.Items, a => a.Where(a => a.Id == id)).ToList();

			Assert.That(result, Has.Count.EqualTo(1));
			Assert.Multiple(() =>
			{
				Assert.That(result[0].Id, Is.EqualTo(1));
				Assert.That(result[0].Items, Has.Count.EqualTo(1));
			});
			Assert.That(result[0].Items[0].Id, Is.EqualTo(12));
		}
		#endregion

		#region Issue 3806

		[Table(IsColumnAttributeRequired = false)]
		public class Issue3806Table
		{
			[PrimaryKey] public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Issue3806ItemTable.AssociationKey))]
			public IEnumerable<Issue3806ItemTable> Items { get; set; } = null!;
		}

		[Table(IsColumnAttributeRequired = false)]
		public class Issue3806ItemTable
		{
			[PrimaryKey] public int Id             { get; set; }
			[Column    ] public int Value          { get; set; }
			[Column    ] public int AssociationKey { get; set; }
		}

		[ActiveIssue]
		[Test]
		public void Issue3806Test([DataSources(false)] string context)
		{
			var queries = new SaveQueriesInterceptor();
			using var db = GetDataContext(context);
			db.AddInterceptor(queries);

			using var table = db.CreateLocalTable<Issue3806Table>();
			using var items = db.CreateLocalTable<Issue3806ItemTable>();

			queries.Queries.Clear();
			table.LoadWith(a => a.Items).Where(a => a.Id != 0).ToList();

			Assert.That(queries.Queries, Has.Count.EqualTo(1));
		}
		#endregion

		#region Issue 3799

		[Table]
		public class Test3799Item
		{
			[PrimaryKey     ] public int    Id       { get; set; }
			[Column         ] public int?   ParentId { get; set; }
			[Column, NotNull] public string Name     { get; set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(ParentId), CanBeNull = true)]
			public IEnumerable<Test3799Item> Children { get; set; } = null!;

			public static Test3799Item[] TestData = new[]
			{
				new Test3799Item() { Id = 1, ParentId = null, Name = "root"      },
				new Test3799Item() { Id = 2, ParentId = 1   , Name = "child 1"   },
				new Test3799Item() { Id = 3, ParentId = 2   , Name = "child 1.1" },
				new Test3799Item() { Id = 4, ParentId = 2   , Name = "child 1.2" },
				new Test3799Item() { Id = 5, ParentId = 1   , Name = "child 2"   },
				new Test3799Item() { Id = 6, ParentId = 5   , Name = "child 2.1" },
				new Test3799Item() { Id = 7, ParentId = 5   , Name = "child 2.1" },
			};
		}

		public sealed class Test3799FirstChildModel
		{
			public string              Name          { get; set; } = null!;
			public IEnumerable<string> ChildrenNames { get; set; } = null!;

			internal static Expression<Func<Test3799Item, Test3799FirstChildModel>> Selector = item => new Test3799FirstChildModel
			{
				Name          = item.Name,
				ChildrenNames = item.Children.AsQueryable().Select(x => x.Name).ToList(),
			};
		}

		public sealed class Test3799ItemModel
		{
			public string                   Name       { get; set; } = null!;
			public Test3799FirstChildModel? FirstChild { get; set; }

			internal static Expression<Func<Test3799Item, Test3799ItemModel>> Selector = item => new Test3799ItemModel()
			{
				Name       = item.Name,
				FirstChild = item.Children.AsQueryable().Select(Test3799FirstChildModel.Selector).FirstOrDefault(),
			};
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void Issue3799Test([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<Test3799Item>(Test3799Item.TestData);

			var result = table.Select(Test3799ItemModel.Selector).ToList();
		}
		#endregion

		#region Issue 4057
		[Test]
		public async Task Issue4057_Async([DataSources] string context)
		{
			DataOptions options;

			await using (var db = GetDataContext(context))
			{
				options = db.Options;
				await db.GetTable<Parent>()
					.LoadWith(x => x.Children)
					.FirstOrDefaultAsync(x => x.ParentID == 3);
			}

			await using (var db = new DataContext(options))
			{
				db.KeepConnectionAlive = true;

				await db.GetTable<Parent>()
					.LoadWith(x => x.Children)
					.FirstOrDefaultAsync(x => x.ParentID == 3);
			}

			await using (var db = new DataContext(options))
			{
				db.KeepConnectionAlive = false;

				await db.GetTable<Parent>()
					.LoadWith(x => x.Children)
					.FirstOrDefaultAsync(x => x.ParentID == 3);
			}
		}

		[Test]
		public void Issue4057_Sync([DataSources] string context)
		{
			DataOptions options;
			using (var db = GetDataContext(context))
			{
				options = db.Options;

				db.GetTable<Parent>()
					.LoadWith(x => x.Children)
					.FirstOrDefault(x => x.ParentID == 3);
			}

			using (var db = new DataContext(options))
			{
				db.KeepConnectionAlive = true;

				db.GetTable<Parent>()
					.LoadWith(x => x.Children)
					.FirstOrDefault(x => x.ParentID == 3);
			}

			using (var db = new DataContext(options))
			{
				db.KeepConnectionAlive = false;

				db.GetTable<Parent>()
					.LoadWith(x => x.Children)
					.FirstOrDefault(x => x.ParentID == 3);
			}
		}

		[Test]
		public async Task Issue4057_Async_ExplicitTransaction([DataSources] string context)
		{
			DataOptions options;

			await using (var db = GetDataContext(context))
			{
				using var _ = db.BeginTransaction();
				options = db.Options;
				await db.GetTable<Parent>()
					.LoadWith(x => x.Children)
					.FirstOrDefaultAsync(x => x.ParentID == 3);
			}

			await using (var db = new DataContext(options))
			{
				db.KeepConnectionAlive = true;

				using var _ = db.BeginTransaction();
				await db.GetTable<Parent>()
					.LoadWith(x => x.Children)
					.FirstOrDefaultAsync(x => x.ParentID == 3);
			}

			await using (var db = new DataContext(options))
			{
				db.KeepConnectionAlive = false;

				using var _ = db.BeginTransaction();
				await db.GetTable<Parent>()
					.LoadWith(x => x.Children)
					.FirstOrDefaultAsync(x => x.ParentID == 3);
			}
		}

		[Test]
		public void Issue4057_Sync_ExplicitTransaction([DataSources] string context)
		{
			DataOptions options;
			using (var db = GetDataContext(context))
			{
				options = db.Options;

				using var _ = db.BeginTransaction();
				db.GetTable<Parent>()
					.LoadWith(x => x.Children)
					.FirstOrDefault(x => x.ParentID == 3);
			}

			using (var db = new DataContext(options))
			{
				db.KeepConnectionAlive = true;

				using var _ = db.BeginTransaction();
				db.GetTable<Parent>()
					.LoadWith(x => x.Children)
					.FirstOrDefault(x => x.ParentID == 3);
			}

			using (var db = new DataContext(options))
			{
				db.KeepConnectionAlive = false;

				using var _ = db.BeginTransaction();
				db.GetTable<Parent>()
					.LoadWith(x => x.Children)
					.FirstOrDefault(x => x.ParentID == 3);
			}
		}
		#endregion

		[Test]
		public void Issue4060([DataSources] string context)
		{
			using var db = GetDataContext(context);

			db.Person.Where(p => p.ID == 2)
				.Concat(db.Person.Where(p => p.ID == 3))
				.LoadWith(p => p.Patient)
				.ToList();
		}

		#region Issue 3226
		[Table]
		sealed class Item
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public string? Text { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(ItemValue.ItemId), CanBeNull = true)]
			public IEnumerable<ItemValue> Values { get; set; } = null!;
		}

		[Table]
		sealed class ItemValue
		{
			[Column] public int Id { get; set; }
			[Column] public int ItemId { get; set; }
			[Column] public decimal Value { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3226")]
		public void Issue3226Test1([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<Item>();
			using var t2 = db.CreateLocalTable<ItemValue>();

			t1
				.OrderBy(x => x.Values.Sum(y => y.Value))
				.Select(x => new {
					Id = x.Id,
					Text = x.Text
				})
				.ToList();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3226")]
		public void Issue3226Test2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<Item>();
			using var t2 = db.CreateLocalTable<ItemValue>();

			t1
				.Select(x => new {
					Id = x.Id,
					Text = x.Text,
					Summary = x.Values.Select(y => new { Total = y.Value }).AsEnumerable()
				})
				.ToList();
		}

		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllClickHouse], ErrorMessage = ErrorHelper.Error_Correlated_Subqueries)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3226")]
		public void Issue3226Test3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<Item>();
			using var t2 = db.CreateLocalTable<ItemValue>();

			t1
				.OrderBy(x => x.Values.Sum(y => y.Value))
				.Select(x => new {
					Id = x.Id,
					Text = x.Text,
					Summary = x.Values.Select(y => new { Total = y.Value }).AsEnumerable()
				})
				.ToList();
		}

		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllClickHouse], ErrorMessage = ErrorHelper.Error_Correlated_Subqueries)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3226")]
		public void Issue3226Test4([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<Item>();
			using var t2 = db.CreateLocalTable<ItemValue>();

			t1
				.OrderBy(x => x.Values.Sum(y => (decimal?)y.Value) ?? (decimal)0.0)
				.Select(x => new {
					Id = x.Id,
					Text = x.Text,
					Summary = x.Values.Select(y => new { Total = y.Value }).AsEnumerable()
				})
				.ToList();
		}
		#endregion

		abstract class EntityBase
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public int? FK { get; set; }
		}

		[Table]
		class EntityA : EntityBase
		{
			[Association(ThisKey = nameof(FK), OtherKey = nameof(Id), CanBeNull = false)]
			public EntityB ObjectB { get; private set; } = null!;

			[Association(ThisKey = nameof(FK), OtherKey = nameof(Id), CanBeNull = true)]
			public EntityB? ObjectBOptional { get; private set; } = null!;

			[Association(ThisKey = nameof(FK), OtherKey = nameof(Id), CanBeNull = false)]
			public EntityB ObjectBRO => throw new NotImplementedException();

			public static EntityA[] Data =
			[
				new () { Id = 10, FK = 20 },
				new () { Id = 11, FK = 21 },
				new () { Id = 12, FK = 22 },
				new () { Id = 13, FK = 20 },
				new () { Id = 14, FK = null },
				new () { Id = 15, FK = null },
				new () { Id = 16, FK = 25 },
				new () { Id = 17, FK = 26 },
				new () { Id = 18, FK = 29 },
			];
		}

		[Table]
		class EntityB : EntityBase
		{
			[Association(ThisKey = nameof(FK), OtherKey = nameof(Id), CanBeNull = true)]
			public EntityC? ObjectC { get; private set; }

			[Association(ThisKey = nameof(FK), OtherKey = nameof(Id), CanBeNull = false)]
			public EntityC ObjectCRequired { get; private set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(FK), CanBeNull = true)]
			public EntityD[]? ObjectsD { get; private set; }

			public static EntityB[] Data =
			[
				new () { Id = 20, FK = 30 },
				new () { Id = 21, FK = 31 },
				new () { Id = 22, FK = 30 },
				new () { Id = 23, FK = 31 },
				new () { Id = 24, FK = 31 },
				new () { Id = 25, FK = null },
				new () { Id = 26, FK = null },
				new () { Id = 27, FK = null },
				new () { Id = 28, FK = 39 },
			];
		}

		[Table]
		class EntityC : EntityBase
		{
			public static EntityC[] Data =
			[
				new () { Id = 30 },
				new () { Id = 31 },
				new () { Id = 32 },
				new () { Id = 33 },
				new () { Id = 34 },
			];
		}

		[Table]
		class EntityD : EntityBase
		{
			public static EntityD[] Data =
			[
				new () { Id = 40, FK = 20 },
				new () { Id = 41, FK = 21 },
				new () { Id = 42, FK = 21 },
				new () { Id = 43, FK = 21 },
				new () { Id = 44, FK = 25 },
				new () { Id = 45, FK = 26 },
				new () { Id = 46, FK = 26 },
				new () { Id = 47, FK = null },
				new () { Id = 48, FK = null },
				new () { Id = 401, FK = 29 },
			];
		}

		[Table]
		class EntityMA : EntityBase
		{
			[Association(ThisKey = nameof(Id), OtherKey = nameof(FK), CanBeNull = false)]
			public EntityMB[] ObjectsB { get; private set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(FK), CanBeNull = true)]
			public EntityMB[] ObjectsBOptional { get; private set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(FK), CanBeNull = false)]
			public EntityMB[] ObjectsBRO => throw new NotImplementedException();

			public static EntityMA[] Data =
			[
				new () { Id = 10 },
				new () { Id = 11 },
				new () { Id = 12 },
				new () { Id = 13 },
			];
		}

		[Table]
		class EntityMB : EntityBase
		{
			[Column] public int? FKD { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(FK), CanBeNull = true)]
			public EntityMC[] ObjectsC { get; private set; } = null!;

			[Association(ThisKey = nameof(FKD), OtherKey = nameof(Id), CanBeNull = true)]
			public EntityMD? ObjectD { get; private set; }

			public static EntityMB[] Data =
			[
				new () { Id = 20, FK = 10, FKD = 40 },
				new () { Id = 21, FK = 11, FKD = null },
				new () { Id = 22, FK = 11, FKD = 40 },
				new () { Id = 23, FK = 19, FKD = 49 },
				new () { Id = 24, FK = 19, FKD = null },
				new () { Id = 25, FK = null, FKD = 49 },
				new () { Id = 26, FK = null, FKD = 40 },
				new () { Id = 27, FK = 19, FKD = 41 },
				new () { Id = 28, FK = 10, FKD = null },
			];
		}

		[Table]
		class EntityMC : EntityBase
		{
			public static EntityMC[] Data =
			[
				new () { Id = 30, FK = 20 },
				new () { Id = 31, FK = 24 },
				new () { Id = 32, FK = 21 },
				new () { Id = 33, FK = 21 },
				new () { Id = 34, FK = 23 },
				new () { Id = 35, FK = null },
				new () { Id = 36, FK = null },
				new () { Id = 37, FK = 29 },
			];
		}

		[Table]
		class EntityMD : EntityBase
		{
			public static EntityMD[] Data =
			[
				new () { Id = 40 },
				new () { Id = 41 },
				new () { Id = 42 },
			];
		}

		[Test]
		public void TestReadOnlyAssociationSingle([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var ta = db.CreateLocalTable(EntityA.Data);
			using var tb = db.CreateLocalTable(EntityB.Data);
			using var tc = db.CreateLocalTable(EntityC.Data);
			using var td = db.CreateLocalTable(EntityD.Data);

			var query = db.GetTable<EntityA>().LoadWith(e => e.ObjectBRO.ObjectC).LoadWith(e => e.ObjectBRO.ObjectsD);

			Assert.That(() => query.ToList(), Throws.InvalidOperationException.With.Message.EqualTo("Cannot construct object 'Tests.Linq.EagerLoadingTests+EntityA'. Following members are not assignable: ObjectBRO."));
		}

		[Test]
		public void TestReadOnlyAssociationMany([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var ta = db.CreateLocalTable(EntityMA.Data);
			using var tb = db.CreateLocalTable(EntityMB.Data);
			using var tc = db.CreateLocalTable(EntityMC.Data);
			using var td = db.CreateLocalTable(EntityMD.Data);

			var query = db.GetTable<EntityMA>()
				.LoadWith(e => e.ObjectsBRO).ThenLoad(e => e.ObjectsC)
				.LoadWith(e => e.ObjectsBRO).ThenLoad(e => e.ObjectD);

			Assert.That(() => query.ToList(), Throws.InvalidOperationException.With.Message.EqualTo("Cannot construct object 'Tests.Linq.EagerLoadingTests+EntityMA'. Following members are not assignable: ObjectsBRO."));
		}

		[Test]
		public void TestReadOnlyAssociationSingleNoData([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var ta = db.CreateLocalTable<EntityA>();
			using var tb = db.CreateLocalTable<EntityB>();
			using var tc = db.CreateLocalTable<EntityC>();
			using var td = db.CreateLocalTable<EntityD>();

			var query = db.GetTable<EntityA>().LoadWith(e => e.ObjectBRO.ObjectC).LoadWith(e => e.ObjectBRO.ObjectsD);

			Assert.That(() => query.ToList(), Throws.InvalidOperationException.With.Message.EqualTo("Cannot construct object 'Tests.Linq.EagerLoadingTests+EntityA'. Following members are not assignable: ObjectBRO."));
		}

		[Test]
		public void TestReadOnlyAssociationManyNoData([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var ta = db.CreateLocalTable<EntityMA>();
			using var tb = db.CreateLocalTable<EntityMB>();
			using var tc = db.CreateLocalTable<EntityMC>();
			using var td = db.CreateLocalTable<EntityMD>();

			var query = db.GetTable<EntityMA>()
				.LoadWith(e => e.ObjectsBRO).ThenLoad(e => e.ObjectsC)
				.LoadWith(e => e.ObjectsBRO).ThenLoad(e => e.ObjectD);

			Assert.That(() => query.ToList(), Throws.InvalidOperationException.With.Message.EqualTo("Cannot construct object 'Tests.Linq.EagerLoadingTests+EntityMA'. Following members are not assignable: ObjectsBRO."));
		}

		[Test]
		public void TestCardinalityEagerLoad1([DataSources] string context, [Values(1, 2, 3)] int testCase)
		{
			using var db = GetDataContext(context);
			using var ta = db.CreateLocalTable(EntityA.Data);
			using var tb = db.CreateLocalTable(EntityB.Data);
			using var tc = db.CreateLocalTable(EntityC.Data);
			using var td = db.CreateLocalTable(EntityD.Data);

			var result = testCase == 1
				? db.GetTable<EntityA>().LoadWith(e => e.ObjectB.ObjectC).ToList()
				: testCase == 2
					? db.GetTable<EntityA>().LoadWith(e => e.ObjectB.ObjectsD).ToList()
					: db.GetTable<EntityA>().LoadWith(e => e.ObjectB.ObjectC).LoadWith(e => e.ObjectB.ObjectsD).ToList();

			var expected = new int?[][]
			{
				[10, 20, 30],
				[11, 21, 31],
				[12, 22, 30],
				[13, 20, 30],
				[16, 25, null],
				[17, 26, null],
			};

			var expectedD = new Dictionary<int, HashSet<int>>()
			{
				{ 20, [40] },
				{ 21, [41,42,43] },
				{ 22, [] },
				{ 25, [44] },
				{ 26, [45,46] },
			};

			Assert.That(result, Has.Count.EqualTo(expected.Length));

			foreach (var set in expected)
			{
				var obj = result.SingleOrDefault(o => o.Id == set[0]);
				Assert.That(obj, Is.Not.Null);
				Assert.That(obj.ObjectB.Id, Is.EqualTo(set[1]));

				if (testCase is 1 or 3)
				{
					if (set[2] is null)
					{
						Assert.That(obj.ObjectB.ObjectC, Is.Null);
					}
					else
					{
						Assert.That(obj.ObjectB.ObjectC, Is.Not.Null);
						Assert.That(obj.ObjectB.ObjectC.Id, Is.EqualTo(set[2]));
					}
				}
				else
				{
					Assert.That(obj.ObjectB.ObjectC, Is.Null);
				}

				if (testCase is 2 or 3)
				{
					Assert.That(obj.ObjectB.ObjectsD, Is.Not.Null);

					var ids = expectedD[obj.ObjectB.Id];
					Assert.That(obj.ObjectB.ObjectsD, Has.Length.EqualTo(ids.Count));
					foreach (var id in ids)
					{
						var recordD = obj.ObjectB.ObjectsD.SingleOrDefault(r => r.Id == id);
						Assert.That(recordD, Is.Not.Null);
					}

				}
				else
				{
					Assert.That(obj.ObjectB.ObjectsD, Is.Null);
				}
			}
		}

		[Test]
		public void TestCardinalityProjected1([DataSources] string context, [Values(1, 2, 3)] int testCase)
		{
			using var db = GetDataContext(context);
			using var ta = db.CreateLocalTable(EntityA.Data);
			using var tb = db.CreateLocalTable(EntityB.Data);
			using var tc = db.CreateLocalTable(EntityC.Data);
			using var td = db.CreateLocalTable(EntityD.Data);

			var result = testCase == 1
				? db.GetTable<EntityA>().Select(e => new { e.Id, ObjectB = new { e.ObjectB.Id, e.ObjectB.ObjectC, ObjectsD = (EntityD[]?)null } }).ToList()
				: testCase == 2
					? db.GetTable<EntityA>().Select(e => new { e.Id, ObjectB = new { e.ObjectB.Id, ObjectC = (EntityC?)null, e.ObjectB.ObjectsD } }).ToList()
					: db.GetTable<EntityA>().Select(e => new { e.Id, ObjectB = new { e.ObjectB.Id, e.ObjectB.ObjectC, e.ObjectB.ObjectsD } }).ToList();

			var expected = new int?[][]
			{
				[10, 20, 30],
				[11, 21, 31],
				[12, 22, 30],
				[13, 20, 30],
				[16, 25, null],
				[17, 26, null],
			};

			var expectedD = new Dictionary<int, HashSet<int>>()
			{
				{ 20, [40] },
				{ 21, [41,42,43] },
				{ 22, [] },
				{ 25, [44] },
				{ 26, [45,46] },
			};

			Assert.That(result, Has.Count.EqualTo(expected.Length));

			foreach (var set in expected)
			{
				var obj = result.SingleOrDefault(o => o.Id == set[0]);
				Assert.That(obj, Is.Not.Null);
				Assert.That(obj.ObjectB.Id, Is.EqualTo(set[1]));

				if (testCase is 1 or 3)
				{
					if (set[2] is null)
					{
						Assert.That(obj.ObjectB.ObjectC, Is.Null);
					}
					else
					{
						Assert.That(obj.ObjectB.ObjectC, Is.Not.Null);
						Assert.That(obj.ObjectB.ObjectC.Id, Is.EqualTo(set[2]));
					}
				}
				else
				{
					Assert.That(obj.ObjectB.ObjectC, Is.Null);
				}

				if (testCase is 2 or 3)
				{
					Assert.That(obj.ObjectB.ObjectsD, Is.Not.Null);

					var ids = expectedD[obj.ObjectB.Id];
					Assert.That(obj.ObjectB.ObjectsD, Has.Length.EqualTo(ids.Count));
					foreach (var id in ids)
					{
						var recordD = obj.ObjectB.ObjectsD.SingleOrDefault(r => r.Id == id);
						Assert.That(recordD, Is.Not.Null);
					}
				}
				else
				{
					Assert.That(obj.ObjectB.ObjectsD, Is.Null);
				}
			}
		}

		[Test]
		public void TestCardinalityEagerLoad2([DataSources] string context, [Values(1, 2, 3)] int testCase)
		{
			using var db = GetDataContext(context);
			using var ta = db.CreateLocalTable(EntityMA.Data);
			using var tb = db.CreateLocalTable(EntityMB.Data);
			using var tc = db.CreateLocalTable(EntityMC.Data);
			using var td = db.CreateLocalTable(EntityMD.Data);

			var result = testCase == 1
				? db.GetTable<EntityMA>().LoadWith(e => e.ObjectsB).ThenLoad(e => e.ObjectsC).ToList()
				: testCase == 2
					? db.GetTable<EntityMA>().LoadWith(e => e.ObjectsB).ThenLoad(e => e.ObjectD).ToList()
					: db.GetTable<EntityMA>().LoadWith(e => e.ObjectsB).ThenLoad(e => e.ObjectsC).LoadWith(e => e.ObjectsB).ThenLoad(e => e.ObjectD).ToList();

			var expected = new Dictionary<int, Dictionary<int, (int? IdD, HashSet<int> IdsC)>>()
			{
				{ 10, new Dictionary<int, (int?, HashSet<int>)>()
					{
						{ 20, (40, [ 30 ]) },
						{ 28, (null, []) }
					}
				},
				{ 11, new Dictionary<int, (int?, HashSet<int>)>()
					{
						{ 21, (null, [ 32, 33 ]) },
						{ 22, (40, []) }
					}
				},
				{ 12, [] },
				{ 13, [] }
			};

			Assert.That(result, Has.Count.EqualTo(expected.Count));

			foreach (var kvp in expected)
			{
				var obj = result.SingleOrDefault(o => o.Id == kvp.Key);
				Assert.That(obj, Is.Not.Null);
				Assert.That(obj.ObjectsB, Has.Length.EqualTo(kvp.Value.Count));

				foreach (var kvp2 in kvp.Value)
				{
					var objB = obj.ObjectsB.SingleOrDefault(o => o.Id == kvp2.Key);
					Assert.That(objB, Is.Not.Null);

					if (testCase is 1 or 3)
					{
						Assert.That(objB.ObjectsC, Has.Length.EqualTo(kvp2.Value.IdsC.Count));

						foreach (var id in kvp2.Value.IdsC)
						{
							var objC = objB.ObjectsC.SingleOrDefault(o => o.Id == id);
							Assert.That(objC, Is.Not.Null);
						}
					}
					else
					{
						Assert.That(objB.ObjectsC, Is.Null);
					}

					if (testCase is 2 or 3 && kvp2.Value.IdD != null)
					{
						Assert.That(objB.ObjectD, Is.Not.Null);
						Assert.That(objB.ObjectD.Id, Is.EqualTo(kvp2.Value.IdD));
					}
					else
					{
						Assert.That(objB.ObjectD, Is.Null);
					}
				}
			}
		}

		[Test]
		public void TestCardinalityProjected2([DataSources] string context, [Values(1, 2, 3)] int testCase)
		{
			using var db = GetDataContext(context);
			using var ta = db.CreateLocalTable(EntityMA.Data);
			using var tb = db.CreateLocalTable(EntityMB.Data);
			using var tc = db.CreateLocalTable(EntityMC.Data);
			using var td = db.CreateLocalTable(EntityMD.Data);

			var result = testCase switch
			{
				1 => db.GetTable<EntityMA>().Select(e => new { e.Id, ObjectsB = e.ObjectsB.Select(e => new { e.Id, e.ObjectsC, ObjectD = (EntityMD?)null }).ToArray() }).ToList(),
				2 => db.GetTable<EntityMA>().Select(e => new { e.Id, ObjectsB = e.ObjectsB.Select(e => new { e.Id, ObjectsC            = (EntityMC[])null!, e.ObjectD }).ToArray() }).ToList(),
				_ => db.GetTable<EntityMA>().Select(e => new { e.Id, ObjectsB = e.ObjectsB.Select(e => new { e.Id, e.ObjectsC, e.ObjectD }).ToArray() }).ToList()
			};

			var expected = new Dictionary<int, Dictionary<int, (int? IdD, HashSet<int> IdsC)>>()
			{
				{ 10, new Dictionary<int, (int?, HashSet<int>)>()
					{
						{ 20, (40, [ 30 ]) },
						{ 28, (null, []) }
					}
				},
				{ 11, new Dictionary<int, (int?, HashSet<int>)>()
					{
						{ 21, (null, [ 32, 33 ]) },
						{ 22, (40, []) }
					}
				},
				{ 12, [] },
				{ 13, [] }
			};

			Assert.That(result, Has.Count.EqualTo(expected.Count));

			foreach (var kvp in expected)
			{
				var obj = result.SingleOrDefault(o => o.Id == kvp.Key);
				Assert.That(obj, Is.Not.Null);
				Assert.That(obj.ObjectsB, Has.Length.EqualTo(kvp.Value.Count));

				foreach (var kvp2 in kvp.Value)
				{
					var objB = obj.ObjectsB.SingleOrDefault(o => o.Id == kvp2.Key);
					Assert.That(objB, Is.Not.Null);

					if (testCase is 1 or 3)
					{
						Assert.That(objB.ObjectsC, Has.Length.EqualTo(kvp2.Value.IdsC.Count));

						foreach (var id in kvp2.Value.IdsC)
						{
							var objC = objB.ObjectsC.SingleOrDefault(o => o.Id == id);
							Assert.That(objC, Is.Not.Null);
						}
					}
					else
					{
						Assert.That(objB.ObjectsC, Is.Null);
					}

					if (testCase is 2 or 3 && kvp2.Value.IdD != null)
					{
						Assert.That(objB.ObjectD, Is.Not.Null);
						Assert.That(objB.ObjectD.Id, Is.EqualTo(kvp2.Value.IdD));
					}
					else
					{
						Assert.That(objB.ObjectD, Is.Null);
					}
				}
			}
		}

		[Test]
		public void TestCardinalityEagerLoadOptional1([DataSources] string context, [Values(1, 2, 3)] int testCase)
		{
			using var db = GetDataContext(context);
			using var ta = db.CreateLocalTable(EntityA.Data);
			using var tb = db.CreateLocalTable(EntityB.Data);
			using var tc = db.CreateLocalTable(EntityC.Data);
			using var td = db.CreateLocalTable(EntityD.Data);

			var result = testCase == 1
				? db.GetTable<EntityA>().LoadWith(e => e.ObjectBOptional!.ObjectC).ToList()
				: testCase == 2
					? db.GetTable<EntityA>().LoadWith(e => e.ObjectBOptional!.ObjectsD).ToList()
					: db.GetTable<EntityA>().LoadWith(e => e.ObjectBOptional!.ObjectC).LoadWith(e => e.ObjectBOptional!.ObjectsD).ToList();

			var expected = new int?[][]
			{
				[10, 20, 30],
				[11, 21, 31],
				[12, 22, 30],
				[13, 20, 30],
				[14, null, null],
				[15, null, null],
				[16, 25, null],
				[17, 26, null],
				[18, null, null],
			};

			var expectedD = new Dictionary<int, HashSet<int>>()
			{
				{ 20, [40] },
				{ 21, [41,42,43] },
				{ 22, [] },
				{ 25, [44] },
				{ 26, [45,46] },
			};

			Assert.That(result, Has.Count.EqualTo(expected.Length));

			foreach (var set in expected)
			{
				var obj = result.SingleOrDefault(o => o.Id == set[0]);
				Assert.That(obj, Is.Not.Null);
				if (set[1] is null)
				{
					Assert.That(obj.ObjectBOptional, Is.Null);
				}
				else
				{
					Assert.That(obj.ObjectBOptional, Is.Not.Null);
					Assert.That(obj.ObjectBOptional.Id, Is.EqualTo(set[1]));

					if (testCase is 1 or 3)
					{
						if (set[2] is null)
						{
							Assert.That(obj.ObjectBOptional.ObjectC, Is.Null);
						}
						else
						{
							Assert.That(obj.ObjectBOptional.ObjectC, Is.Not.Null);
							Assert.That(obj.ObjectBOptional.ObjectC.Id, Is.EqualTo(set[2]));
						}
					}
					else
					{
						Assert.That(obj.ObjectBOptional.ObjectC, Is.Null);
					}

					if (testCase is 2 or 3)
					{
						Assert.That(obj.ObjectBOptional.ObjectsD, Is.Not.Null);

						var ids = expectedD[obj.ObjectBOptional.Id];
						Assert.That(obj.ObjectBOptional.ObjectsD, Has.Length.EqualTo(ids.Count));
						foreach (var id in ids)
						{
							var recordD = obj.ObjectBOptional.ObjectsD.SingleOrDefault(r => r.Id == id);
							Assert.That(recordD, Is.Not.Null);
						}
					}
					else
					{
						Assert.That(obj.ObjectBOptional.ObjectsD, Is.Null);
					}
				}
			}
		}

		[Test]
		public void TestCardinalityProjectedOptional1([DataSources] string context, [Values(1, 2, 3)] int testCase)
		{
			using var db = GetDataContext(context);
			using var ta = db.CreateLocalTable(EntityA.Data);
			using var tb = db.CreateLocalTable(EntityB.Data);
			using var tc = db.CreateLocalTable(EntityC.Data);
			using var td = db.CreateLocalTable(EntityD.Data);

			var query = testCase switch
			{
				1 => db.GetTable<EntityA>().Select(e => new { e.Id, ObjectBOptional = e.ObjectBOptional == null ? null : new { e.ObjectBOptional.Id, e.ObjectBOptional.ObjectC, ObjectsD = (EntityD[]?)null } }),
				2 => db.GetTable<EntityA>().Select(e => new { e.Id, ObjectBOptional = e.ObjectBOptional == null ? null : new { e.ObjectBOptional.Id, ObjectC = (EntityC?)null, e.ObjectBOptional.ObjectsD } }),
				_ => db.GetTable<EntityA>().Select(e => new { e.Id, ObjectBOptional = e.ObjectBOptional == null ? null : new { e.ObjectBOptional.Id, e.ObjectBOptional.ObjectC, e.ObjectBOptional.ObjectsD } })
			};

			var result = query.ToList();

			var expected = new int?[][]
			{
				[10, 20, 30],
				[11, 21, 31],
				[12, 22, 30],
				[13, 20, 30],
				[14, null, null],
				[15, null, null],
				[16, 25, null],
				[17, 26, null],
				[18, null, null],
			};

			var expectedD = new Dictionary<int, HashSet<int>>()
			{
				{ 20, [40] },
				{ 21, [41,42,43] },
				{ 22, [] },
				{ 25, [44] },
				{ 26, [45,46] },
			};

			Assert.That(result, Has.Count.EqualTo(expected.Length));

			foreach (var set in expected)
			{
				var obj = result.SingleOrDefault(o => o.Id == set[0]);
				Assert.That(obj, Is.Not.Null);
				if (set[1] is null)
				{
					Assert.That(obj.ObjectBOptional, Is.Null);
				}
				else
				{
					Assert.That(obj.ObjectBOptional, Is.Not.Null);
					Assert.That(obj.ObjectBOptional.Id, Is.EqualTo(set[1]));

					if (testCase is 1 or 3)
					{
						if (set[2] is null)
						{
							Assert.That(obj.ObjectBOptional.ObjectC, Is.Null);
						}
						else
						{
							Assert.That(obj.ObjectBOptional.ObjectC, Is.Not.Null);
							Assert.That(obj.ObjectBOptional.ObjectC.Id, Is.EqualTo(set[2]));
						}
					}
					else
					{
						Assert.That(obj.ObjectBOptional.ObjectC, Is.Null);
					}

					if (testCase is 2 or 3)
					{
						Assert.That(obj.ObjectBOptional.ObjectsD, Is.Not.Null);

						var ids = expectedD[obj.ObjectBOptional.Id];
						Assert.That(obj.ObjectBOptional.ObjectsD, Has.Length.EqualTo(ids.Count));
						foreach (var id in ids)
						{
							var recordD = obj.ObjectBOptional.ObjectsD.SingleOrDefault(r => r.Id == id);
							Assert.That(recordD, Is.Not.Null);
						}
					}
					else
					{
						Assert.That(obj.ObjectBOptional.ObjectsD, Is.Null);
					}
				}
			}
		}

		[Test]
		public void TestCardinalityEagerLoadOptional2([DataSources] string context, [Values(1, 2, 3)] int testCase)
		{
			using var db = GetDataContext(context);
			using var ta = db.CreateLocalTable(EntityMA.Data);
			using var tb = db.CreateLocalTable(EntityMB.Data);
			using var tc = db.CreateLocalTable(EntityMC.Data);
			using var td = db.CreateLocalTable(EntityMD.Data);

			var result = testCase == 1
				? db.GetTable<EntityMA>().LoadWith(e => e.ObjectsBOptional).ThenLoad(e => e.ObjectsC).ToList()
				: testCase == 2
					? db.GetTable<EntityMA>().LoadWith(e => e.ObjectsBOptional).ThenLoad(e => e.ObjectD).ToList()
					: db.GetTable<EntityMA>().LoadWith(e => e.ObjectsBOptional).ThenLoad(e => e.ObjectsC).LoadWith(e => e.ObjectsBOptional).ThenLoad(e => e.ObjectD).ToList();

			var expected = new Dictionary<int, Dictionary<int, (int? IdD, HashSet<int> IdsC)>>()
			{
				{ 10, new Dictionary<int, (int?, HashSet<int>)>()
					{
						{ 20, (40, [ 30 ]) },
						{ 28, (null, []) }
					}
				},
				{ 11, new Dictionary<int, (int?, HashSet<int>)>()
					{
						{ 21, (null, [ 32, 33 ]) },
						{ 22, (40, []) }
					}
				},
				{ 12, [] },
				{ 13, [] }
			};

			Assert.That(result, Has.Count.EqualTo(expected.Count));

			foreach (var kvp in expected)
			{
				var obj = result.SingleOrDefault(o => o.Id == kvp.Key);
				Assert.That(obj, Is.Not.Null);
				Assert.That(obj.ObjectsBOptional, Has.Length.EqualTo(kvp.Value.Count));

				foreach (var kvp2 in kvp.Value)
				{
					var objB = obj.ObjectsBOptional.SingleOrDefault(o => o.Id == kvp2.Key);
					Assert.That(objB, Is.Not.Null);

					if (testCase is 1 or 3)
					{
						Assert.That(objB.ObjectsC, Has.Length.EqualTo(kvp2.Value.IdsC.Count));

						foreach (var id in kvp2.Value.IdsC)
						{
							var objC = objB.ObjectsC.SingleOrDefault(o => o.Id == id);
							Assert.That(objC, Is.Not.Null);
						}
					}
					else
					{
						Assert.That(objB.ObjectsC, Is.Null);
					}

					if (testCase is 2 or 3 && kvp2.Value.IdD != null)
					{
						Assert.That(objB.ObjectD, Is.Not.Null);
						Assert.That(objB.ObjectD.Id, Is.EqualTo(kvp2.Value.IdD));
					}
					else
					{
						Assert.That(objB.ObjectD, Is.Null);
					}
				}
			}
		}

		[Test]
		public void TestCardinalityProjectedOptional2([DataSources] string context, [Values(1, 2, 3)] int testCase)
		{
			using var db = GetDataContext(context);
			using var ta = db.CreateLocalTable(EntityMA.Data);
			using var tb = db.CreateLocalTable(EntityMB.Data);
			using var tc = db.CreateLocalTable(EntityMC.Data);
			using var td = db.CreateLocalTable(EntityMD.Data);

			var result = testCase == 1
				? db.GetTable<EntityMA>().Select(e => new { e.Id, ObjectsBOptional = e.ObjectsBOptional.Select(e => new { e.Id, e.ObjectsC, ObjectD = (EntityMD?)null }).ToArray() }).ToList()
				: testCase == 2
					? db.GetTable<EntityMA>().Select(e => new { e.Id, ObjectsBOptional = e.ObjectsBOptional.Select(e => new { e.Id, ObjectsC = (EntityMC[])null!, e.ObjectD }).ToArray() }).ToList()
					: db.GetTable<EntityMA>().Select(e => new { e.Id, ObjectsBOptional = e.ObjectsBOptional.Select(e => new { e.Id, e.ObjectsC, e.ObjectD }).ToArray() }).ToList();

			var expected = new Dictionary<int, Dictionary<int, (int? IdD, HashSet<int> IdsC)>>()
			{
				{ 10, new Dictionary<int, (int?, HashSet<int>)>()
					{
						{ 20, (40, [ 30 ]) },
						{ 28, (null, []) }
					}
				},
				{ 11, new Dictionary<int, (int?, HashSet<int>)>()
					{
						{ 21, (null, [ 32, 33 ]) },
						{ 22, (40, []) }
					}
				},
				{ 12, [] },
				{ 13, [] }
			};

			Assert.That(result, Has.Count.EqualTo(expected.Count));

			foreach (var kvp in expected)
			{
				var obj = result.SingleOrDefault(o => o.Id == kvp.Key);
				Assert.That(obj, Is.Not.Null);
				Assert.That(obj.ObjectsBOptional, Has.Length.EqualTo(kvp.Value.Count));

				foreach (var kvp2 in kvp.Value)
				{
					var objB = obj.ObjectsBOptional.SingleOrDefault(o => o.Id == kvp2.Key);
					Assert.That(objB, Is.Not.Null);

					if (testCase is 1 or 3)
					{
						Assert.That(objB.ObjectsC, Has.Length.EqualTo(kvp2.Value.IdsC.Count));

						foreach (var id in kvp2.Value.IdsC)
						{
							var objC = objB.ObjectsC.SingleOrDefault(o => o.Id == id);
							Assert.That(objC, Is.Not.Null);
						}
					}
					else
					{
						Assert.That(objB.ObjectsC, Is.Null);
					}

					if (testCase is 2 or 3 && kvp2.Value.IdD != null)
					{
						Assert.That(objB.ObjectD, Is.Not.Null);
						Assert.That(objB.ObjectD.Id, Is.EqualTo(kvp2.Value.IdD));
					}
					else
					{
						Assert.That(objB.ObjectD, Is.Null);
					}
				}
			}
		}

		[Test]
		public void TestCardinalityEagerLoadOptional3([DataSources] string context, [Values(1, 2, 3)] int testCase)
		{
			using var db = GetDataContext(context);
			using var ta = db.CreateLocalTable(EntityA.Data);
			using var tb = db.CreateLocalTable(EntityB.Data);
			using var tc = db.CreateLocalTable(EntityC.Data);
			using var td = db.CreateLocalTable(EntityD.Data);

			var query = testCase switch
			{
				1 => db.GetTable<EntityA>().LoadWith(e => e.ObjectBOptional!.ObjectCRequired).AsQueryable(),
				2 => db.GetTable<EntityA>().LoadWith(e => e.ObjectBOptional!.ObjectsD),
				_ => db.GetTable<EntityA>().LoadWith(e => e.ObjectBOptional!.ObjectCRequired).LoadWith(e => e.ObjectBOptional!.ObjectsD)
			};

			var result = query.ToList();

			var expected = new int?[][]
			{
				[10, 20, 30],
				[11, 21, 31],
				[12, 22, 30],
				[13, 20, 30],
				[14, null, null],
				[15, null, null],
				[16, 25, null],
				[17, 26, null],
				[18, null, null],
			};

			var expectedD = new Dictionary<int, HashSet<int>>()
			{
				{ 20, [40] },
				{ 21, [41,42,43] },
				{ 22, [] },
				{ 25, [44] },
				{ 26, [45,46] },
			};

			Assert.That(result, Has.Count.EqualTo(expected.Length));

			for (var index = 0; index < expected.Length; index++)
			{
				var set = expected[index];
				var obj = result.SingleOrDefault(o => o.Id == set[0]);
				Assert.That(obj, Is.Not.Null);
				if (set[1] is null)
				{
					Assert.That(obj.ObjectBOptional, Is.Null);
				}
				else
				{
					Assert.That(obj.ObjectBOptional, Is.Not.Null);
					Assert.That(obj.ObjectBOptional.Id, Is.EqualTo(set[1]));

					if (testCase is 1 or 3)
					{
						if (set[2] is null)
						{
							Assert.That(obj.ObjectBOptional.ObjectCRequired, Is.Null);
						}
						else
						{
							Assert.That(obj.ObjectBOptional.ObjectCRequired, Is.Not.Null);
							Assert.That(obj.ObjectBOptional.ObjectCRequired.Id, Is.EqualTo(set[2]));
						}
					}
					else
					{
						Assert.That(obj.ObjectBOptional.ObjectCRequired, Is.Null);
					}

					if (testCase is 2 or 3)
					{
						Assert.That(obj.ObjectBOptional.ObjectsD, Is.Not.Null);

						var ids = expectedD[obj.ObjectBOptional.Id];
						Assert.That(obj.ObjectBOptional.ObjectsD, Has.Length.EqualTo(ids.Count));
						foreach (var id in ids)
						{
							var recordD = obj.ObjectBOptional.ObjectsD.SingleOrDefault(r => r.Id == id);
							Assert.That(recordD, Is.Not.Null);
						}
					}
					else
					{
						Assert.That(obj.ObjectBOptional.ObjectsD, Is.Null);
					}
				}
			}
		}

		[Test]
		public void TestCardinalityProjectedOptional3([DataSources] string context, [Values(1, 2, 3)] int testCase)
		{
			using var db = GetDataContext(context);
			using var ta = db.CreateLocalTable(EntityA.Data);
			using var tb = db.CreateLocalTable(EntityB.Data);
			using var tc = db.CreateLocalTable(EntityC.Data);
			using var td = db.CreateLocalTable(EntityD.Data);

			var query = testCase switch
			{
				1 => db.GetTable<EntityA>().Select(e => new { e.Id, ObjectBOptional = e.ObjectBOptional == null ? null : new { e.ObjectBOptional.Id, e.ObjectBOptional.ObjectCRequired, ObjectsD = (EntityD[]?)null } }),
				2 => db.GetTable<EntityA>().Select(e => new { e.Id, ObjectBOptional = e.ObjectBOptional == null ? null : new { e.ObjectBOptional.Id, ObjectCRequired                             = (EntityC)null!, e.ObjectBOptional.ObjectsD } }),
				_ => db.GetTable<EntityA>().Select(e => new { e.Id, ObjectBOptional = e.ObjectBOptional == null ? null : new { e.ObjectBOptional.Id, e.ObjectBOptional.ObjectCRequired, e.ObjectBOptional.ObjectsD } })
			};

			var result = query.ToList();

			var expected = new int?[][]
			{
				[10, 20, 30],
				[11, 21, 31],
				[12, 22, 30],
				[13, 20, 30],
				[14, null, null],
				[15, null, null],
				[16, 25, null],
				[17, 26, null],
				[18, null, null],
			};

			var expectedD = new Dictionary<int, HashSet<int>>()
			{
				{ 20, [40] },
				{ 21, [41,42,43] },
				{ 22, [] },
				{ 25, [44] },
				{ 26, [45,46] },
			};

			Assert.That(result, Has.Count.EqualTo(expected.Length));

			foreach (var set in expected)
			{
				var obj = result.SingleOrDefault(o => o.Id == set[0]);
				Assert.That(obj, Is.Not.Null);
				if (set[1] is null)
				{
					Assert.That(obj.ObjectBOptional, Is.Null);
				}
				else
				{
					Assert.That(obj.ObjectBOptional, Is.Not.Null);
					Assert.That(obj.ObjectBOptional.Id, Is.EqualTo(set[1]));

					if (testCase is 1 or 3)
					{
						if (set[2] is null)
						{
							Assert.That(obj.ObjectBOptional.ObjectCRequired, Is.Null);
						}
						else
						{
							Assert.That(obj.ObjectBOptional.ObjectCRequired, Is.Not.Null);
							Assert.That(obj.ObjectBOptional.ObjectCRequired.Id, Is.EqualTo(set[2]));
						}
					}
					else
					{
						Assert.That(obj.ObjectBOptional.ObjectCRequired, Is.Null);
					}

					if (testCase is 2 or 3)
					{
						Assert.That(obj.ObjectBOptional.ObjectsD, Is.Not.Null);

						var ids = expectedD[obj.ObjectBOptional.Id];
						Assert.That(obj.ObjectBOptional.ObjectsD, Has.Length.EqualTo(ids.Count));
						foreach (var id in ids)
						{
							var recordD = obj.ObjectBOptional.ObjectsD.SingleOrDefault(r => r.Id == id);
							Assert.That(recordD, Is.Not.Null);
						}
					}
					else
					{
						Assert.That(obj.ObjectBOptional.ObjectsD, Is.Null);
					}
				}
			}
		}

		#region Issue 4497
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4497")]
		public void Issue4497Test1([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);

			db.Person
				.LeftJoin(
					db.Patient,
					(join, p) => join.ID == p.PersonID,
					(join, p) => new { join, p })
				.Where(i => i.p.PersonID != 0)
				.ToList();

			Assert.That(db.LastQuery, Does.Contain("LEFT JOIN"));
			Assert.That(db.LastQuery, Does.Contain("IS NULL"));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4497")]
		public void Issue4497Test2([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);

			db.Person
				.LoadWith(p => p.Patient)
				.Where(i => i.Patient!.PersonID != 0)
				.ToList();

			Assert.That(db.LastQuery, Does.Contain("LEFT JOIN"));
			Assert.That(db.LastQuery, Does.Contain("IS NULL"));
		}
		#endregion

		#region Issue 4585
		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4585")]
		public void Issue4585Test([DataSources] string context)
		{
			var fluentMappingBuilder = new FluentMappingBuilder(new MappingSchema());

			fluentMappingBuilder
				.Entity<Issue4585TableNested>()
				.Property(x => x.Id)
				.Property(x => x.Code);

			fluentMappingBuilder
				.Entity<Issue4585TableBase>()
				.Inheritance(x => x.TypeId, HierarchyType.Type1, typeof(Issue4585Table))
				.Property(t => t.Data).HasColumnName("data")
				.Property(t => t.Id).IsPrimaryKey()
				.Property(t => t.TypeId).IsDiscriminator()
				.Build();

			fluentMappingBuilder.Entity<Issue4585Table>()
				.Property(x => x.SomeField)
				.Property(x => x.NestedTypeId)
				.Association(x => x.Nested, x => x.NestedTypeId, x => x!.Id);

			using var db = GetDataContext(context, fluentMappingBuilder.MappingSchema);

			using var table = db.CreateLocalTable<Issue4585TableBase>();
			using var table1 = db.CreateLocalTable<Issue4585TableNested>();

			var list      = db.GetTable<Issue4585Table>()
				.LoadWith(x => x.Nested)
				.ToList();
		}

		class Issue4585TableBase
		{
			public int Id { get; set; }
			public string Data { get; set; } = null!;
			public HierarchyType TypeId { get; set; }
		}

		enum HierarchyType
		{
			Type1 = 0,
			Type2 = 1
		}

		sealed class Issue4585Table : Issue4585TableBase
		{
			public string? SomeField { get; set; }
			public int? NestedTypeId { get; set; }
			public Issue4585TableNested? Nested { get; set; }
		}

		sealed class Issue4585TableNested
		{
			public int Id { get; set; }
			public string Code { get; set; } = null!;
		}
		#endregion

		#region Issue 3140
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3140")]
		public void Issue3140Test1([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);
			using var t1 = db.CreateLocalTable<Issue3140Parent>();
			using var t2 = db.CreateLocalTable<Issue3140Child>();

			var query = from p in t1
						select new Issue3140Parent()
						{
							Id = p.Id,
							Child = new Issue3140Child()
							{
								Id = p.Child!.Id,
								Name = p.Child!.Name,
							}
						};

			query.ToArray();

			var selects = db.LastQuery!.Split(["SELECT"], StringSplitOptions.None).Length - 1;
			var joins = db.LastQuery.Split(["LEFT JOIN"], StringSplitOptions.None).Length - 1;

			Assert.Multiple(() =>
			{
				Assert.That(selects, Is.EqualTo(1));
				Assert.That(joins, Is.EqualTo(1));
			});
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3140")]
		public void Issue3140Test2([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);
			using var t1 = db.CreateLocalTable<Issue3140Parent>();
			using var t2 = db.CreateLocalTable<Issue3140Child>();

			var query = t1
				.LoadWith(
					p => p.Child,
					c => c.Select(x => new Issue3140Child() { Id = x.Id, Name = x.Name }));

			query.ToArray();

			var selects = db.LastQuery!.Split(["SELECT"], StringSplitOptions.None).Length - 1;
			var joins = db.LastQuery.Split(["LEFT JOIN"], StringSplitOptions.None).Length - 1;

			Assert.Multiple(() =>
			{
				Assert.That(selects, Is.EqualTo(1));
				Assert.That(joins, Is.EqualTo(1));
			});
		}

		sealed class Issue3140Parent
		{
			[PrimaryKey] public int Id { get; set; }
			public int ChildId { get; set; }
			[Association(ThisKey = nameof(ChildId), OtherKey = nameof(Issue3140Child.Id))] public Issue3140Child? Child { get; set; }
		}

		sealed class Issue3140Child
		{
			[PrimaryKey] public int Id { get; set; }
			public string? Name { get; set; }
		}
		#endregion

		#region Issue 4588
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, ErrorMessage = ErrorHelper.Error_Skip_in_Subquery)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OrderBy_in_Derived)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4588")]
		public void Issue4588Test([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);
			using var t1 = db.CreateLocalTable<Order>();
			using var t2 = db.CreateLocalTable<SubOrder>();
			using var t3 = db.CreateLocalTable<SubOrderDetail>();

			db.GetTable<Order>()
				.Where(x => x.Name!.StartsWith("cat"))
				.LoadWith(x => x.SubOrders)
				.ThenLoad(x => x.SubOrderDetails)
				.OrderBy(x => x.Id)
				.Skip(100)
				.Take(10)
				.ToArray();
		}

		sealed class Order
		{
			public int Id { get; set; }
			public string? Name { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = nameof(SubOrder.OrderId))]
			public List<SubOrder> SubOrders { get; set; } = null!;
		}

		sealed class SubOrder
		{
			public int Id { get; set; }
			public int OrderId { get; set; }
			[Association(ThisKey = nameof(OrderId), OtherKey = nameof(Order.Id))]
			public Order? Order { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = nameof(SubOrderDetail.SubOrderId))]
			public List<SubOrderDetail> SubOrderDetails { get; set; } = null!;
		}

		sealed class SubOrderDetail
		{
			public int Id { get; set; }
			public int SubOrderId { get; set; }
			[Association(ThisKey = nameof(SubOrderId), OtherKey = nameof(SubOrder.Id))]
			public SubOrder? SubOrder { get; set; }
			public string? Code { get; set; }
			public DateTime Date { get; set; }
			public bool IsActive { get; set; }
		}
		#endregion

		#region Issue : CTE cloning

		class CteTable
		{
			public int Id { get; set; }
			public int Value1 { get; set; }
			public int Value2 { get; set; }
			public int Value3 { get; set; }
			public int Value4 { get; set; }
			public int Value5 { get; set; }
		}

		class CteChildTable
		{
			public int Id { get; set; }
			public int Value { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(CteTable.Value3))]
			public CteTable[]? MainRecords { get; set; }
		}

		sealed record CteRecord(int Id, int Value1, int Value2, int Value4, int Value5);

		[Test]
		public void CteCloning_Original([CteTests.CteContextSource(TestProvName.AllSapHana)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CteTable>();
			using var tc = db.CreateLocalTable<CteChildTable>();

			var cte = db.GetCte<CteTable>(cte =>
			{
				return (
				from r in tb
				select new CteTable() { Id = r.Id, Value1 = r.Value1, Value2 = r.Value2, Value3 = r.Value3, Value4 = r.Value4, Value5 = r.Value5 }
				).Concat(
					from c in cte
					join r in tb on c.Value2 equals r.Value3
					select new CteTable() { Id = r.Id, Value1 = r.Value1, Value2 = r.Value2, Value3 = r.Value3, Value4 = r.Value4, Value5 = r.Value5 }
					);
			});

			var query = (from r in cte
						 join c in tc on r.Value4 equals c.Id into ds
						 from d in ds.DefaultIfEmpty()
						 select new
						 {
							 r.Id,
							 r.Value1,
							 r.Value2,
							 r.Value3,
							 r.Value4,
							 r.Value5,
							 Children = d.MainRecords!.ToArray()
						 }
						);

			query.ToArray();
		}

		[Test]
		public void CteCloning_Simple([CteTests.CteContextSource] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CteTable>();
			using var tc = db.CreateLocalTable<CteChildTable>();

			var cte = (
				from r in tb
				select new CteTable() { Id = r.Id, Value1 = r.Value1, Value2 = r.Value2, Value3 = r.Value3, Value4 = r.Value4, Value5 = r.Value5 }
				).AsCte();

			var query = (from r in cte
						 join c in tc on r.Value4 equals c.Id into ds
						 from d in ds.DefaultIfEmpty()
						 select new
						 {
							 r.Id,
							 r.Value1,
							 r.Value2,
							 r.Value3,
							 r.Value4,
							 r.Value5,
							 Children = d.MainRecords!.ToArray()
						 }
						);

			query.ToArray();
		}

		[Test]
		public void CteCloning_SimpleChain([CteTests.CteContextSource] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CteTable>();
			using var tc = db.CreateLocalTable<CteChildTable>();

			var cte1 = (
				from r in tb
				select new CteTable() { Id = r.Id, Value1 = r.Value1, Value2 = r.Value2, Value3 = r.Value3, Value4 = r.Value4, Value5 = r.Value5 }
				).AsCte();

			var cte2 = (
				from r in cte1
				select new CteTable() { Id = r.Id, Value1 = r.Value2, Value2 = r.Value2, Value3 = r.Value5, Value4 = r.Value4, Value5 = r.Value3 }
				).AsCte();

			var cte3 = (
				from r in cte2
				select new CteTable() { Id = r.Value1, Value1 = r.Value3, Value2 = r.Value5, Value3 = r.Value2, Value4 = r.Id, Value5 = r.Value4 }
				).AsCte();

			var query = (from r in cte3
						 join c in tc on r.Value4 equals c.Id into ds
						 from d in ds.DefaultIfEmpty()
						 select new
						 {
							 r.Id,
							 r.Value1,
							 r.Value2,
							 r.Value3,
							 r.Value4,
							 r.Value5,
							 Children = d.MainRecords!.ToArray()
						 }
						);

			query.ToArray();
		}

		[Test]
		public void CteCloning_RecursiveChain([CteTests.CteContextSource(TestProvName.AllSapHana, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CteTable>();
			using var tc = db.CreateLocalTable<CteChildTable>();

			var cte1 = db.GetCte<CteTable>(cte =>
			{
				return (
				from r in tb
				select new CteTable() { Id = r.Id, Value1 = r.Value1, Value2 = r.Value2, Value3 = r.Value3, Value4 = r.Value4, Value5 = r.Value5 }
				).Concat(
					from c in cte
					join r in tb on c.Value2 equals r.Value3
					select new CteTable() { Id = r.Id, Value1 = r.Value1, Value2 = r.Value1, Value3 = r.Value3, Value4 = r.Value1, Value5 = r.Value5 }
					);
			});

			var cte2 = db.GetCte<CteTable>(cte =>
			{
				return (
				from r in tb
				select new CteTable() { Id = r.Id, Value1 = r.Value1, Value2 = r.Value2, Value3 = r.Value3, Value4 = r.Value4, Value5 = r.Id }
				)
				.Concat(
					from c in cte1
					join r in tb on c.Value2 equals r.Value3
					select new CteTable() { Id = r.Id, Value1 = r.Value1, Value2 = r.Value2, Value3 = r.Value2, Value4 = r.Id, Value5 = r.Value5 }
					)
				.Concat(
					from c in cte
					join r in tb on c.Value2 equals r.Value3
					select new CteTable() { Id = r.Id, Value1 = r.Value1, Value2 = r.Value2, Value3 = r.Id, Value4 = r.Value2, Value5 = r.Value5 }
					)
				;
			});

			var cte3 = db.GetCte<CteTable>(cte =>
			{
				return (
				from r in tb
				select new CteTable() { Id = r.Value4, Value1 = r.Value1, Value2 = r.Value2, Value3 = r.Value3, Value4 = r.Value4, Value5 = r.Value5 }
				)
				.Concat(
					from c in cte2
					join r in tb on c.Value2 equals r.Value3
					select new CteTable() { Id = r.Id, Value1 = r.Value1, Value2 = r.Value2, Value3 = r.Value3, Value4 = r.Id, Value5 = r.Value4 }
					)
				.Concat(
					from c in cte1
					join r in tb on c.Value2 equals r.Value3
					select new CteTable() { Id = r.Id, Value1 = r.Value4, Value2 = r.Id, Value3 = r.Value3, Value4 = r.Value4, Value5 = r.Value5 }
					)
				.Concat(
					from c in cte
					join r in tb on c.Value2 equals r.Value3
					select new CteTable() { Id = r.Id, Value1 = r.Value1, Value2 = r.Value2, Value3 = r.Id, Value4 = r.Value4, Value5 = r.Value5 }
					);
			});

			var query = (from r in cte3
						 join c in tc on r.Value4 equals c.Id into ds
						 from d in ds.DefaultIfEmpty()
						 select new
						 {
							 r.Id,
							 r.Value1,
							 r.Value2,
							 r.Value3,
							 r.Value4,
							 r.Value5,
							 Children = d.MainRecords!.ToArray()
						 }
						);

			query.ToArray();
		}

		#endregion

		#region Issue 4797

		[Table]
		public class Issue4797Parent
		{
			[Column]
			public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Issue4797Child.ParentId))]
			public Issue4797Child[]? Children { get; set; }

			public static readonly Issue4797Parent[] Data =
			[
				new Issue4797Parent() { Id = 1 }
			];
		}

		[Table]
		public class Issue4797Child
		{
			[Column]
			public int Id { get; set; }

			[Column]
			public int? ParentId { get; set; }

			[Association(ThisKey = nameof(ParentId), OtherKey = nameof(Issue4797Parent.Id))]
			public Issue4797Parent? Parent { get; set; }

			public static readonly Issue4797Child[] Data =
			[
				new Issue4797Child() { Id = 1, ParentId = 1 },
				new Issue4797Child() { Id = 2, ParentId = 1 },
			];
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4797")]
		public void Issue4797Test([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var pt = db.CreateLocalTable(Issue4797Parent.Data);
			using var ct = db.CreateLocalTable(Issue4797Child.Data);

			var result = pt
				.LoadWith(
					x => x.Children,
					x => x.LoadWith(y => y.Parent, y => y.LoadWith(z => z.Children)))
				.ToArray();

			Assert.That(result, Has.Length.EqualTo(1));
			Assert.Multiple(() =>
			{
				Assert.That(result[0].Id, Is.EqualTo(1));
				Assert.That(result[0].Children, Is.Not.Null);
			});
			Assert.That(result[0].Children, Has.Length.EqualTo(2));
			Assert.Multiple(() =>
			{
				Assert.That(result[0].Children.Count(r => r.Id == 1), Is.EqualTo(1));
				Assert.That(result[0].Children.Count(r => r.Id == 2), Is.EqualTo(1));
			});
			Assert.That(result[0].Children[0].Parent, Is.Not.Null);
			Assert.That(result[0].Children[0].Parent.Children, Is.Not.Null);
			Assert.Multiple(() =>
			{
				Assert.That(result[0].Children[0].Parent.Children, Has.Length.EqualTo(2));
				Assert.That(result[0].Children[1].Parent, Is.Not.Null);
			});
			Assert.That(result[0].Children[1].Parent.Children, Is.Not.Null);
			Assert.That(result[0].Children[1].Parent.Children, Has.Length.EqualTo(2));

			// TODO: right now we create separate objects for same record on different levels
			// if we want to change this behavior - it makes sense to add object equality asserts
		}

		#endregion
	}
}
