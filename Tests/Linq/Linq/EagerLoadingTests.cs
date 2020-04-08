﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class EagerLoadingTests : TestBase
	{
		[Table]
		class MasterClass
		{
			[Column] [PrimaryKey] public int Id1    { get; set; }
			[Column] [PrimaryKey] public int Id2    { get; set; }
			[Column] public string? Value { get; set; }

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
		class MasterManyId
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
		class DetailClass
		{
			[Column] [PrimaryKey] public int DetailId    { get; set; }
			[Column] public int? MasterId    { get; set; }
			[Column] public string? DetailValue { get; set; }

			[Association(ThisKey = nameof(DetailId), OtherKey = nameof(SubDetailClass.DetailId))]
			public SubDetailClass[] SubDetails { get; set; } = null!;
}

		[Table]
		class SubDetailClass
		{
			[Column] [PrimaryKey] public int SubDetailId    { get; set; }
			[Column] public int? DetailId    { get; set; }
			[Column] public string? SubDetailValue { get; set; }
		}

		class SubDetailDTO
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
		public void TestLoadWith([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();
			var intParam = 0;

			using (new AllowMultipleQuery())
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
						Id1 = m.Id1,
						Id2 = m.Id2,
						Value = m.Value,
						Details = detailRecords.Where(d => d.MasterId == m.Id1).ToList(),
						DetailsQuery = detailRecords.Where(d => d.MasterId == m.Id1 && d.MasterId == m.Id2 && d.DetailId % 2 == 0).ToArray(),
					};

				var result = query.ToList();
				var expected = expectedQuery.ToList();

				AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
			}
		}

		[Test]
		public void TestLoadWithDeep([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var (masterRecords, detailRecords, subDetailRecords) = GenerateDataWithSubDetail();
			var intParam = 1;

			using (new AllowMultipleQuery())
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

				AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
			}
		}

		[Test]
		public void TestMethodMappedProjection([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var (masterRecords, detailRecords, subDetailRecords) = GenerateDataWithSubDetail();
			var intParam = 1;

			using (new AllowMultipleQuery())
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
		public void TestSelectProjectionList([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();
			var intParam = 0;

			using (new AllowMultipleQuery())
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
		public async Task TestSelectProjectionListAsync([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();
			var intParam = 0;

			using (new AllowMultipleQuery())
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
		public async Task TestSelectAssociationProjectionListAsync([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();
			var intParam = 0;

			using (new AllowMultipleQuery())
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

			using (new AllowMultipleQuery())
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
		public void TestQueryableAssociation([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();

			using (new AllowMultipleQuery())
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
		public void TestRecursive([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var (masterRecords, detailRecords, subDetailRecords) = GenerateDataWithSubDetail();

			var masterFilter = 5;
			using (new AllowMultipleQuery())
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
							SubDetails = subDetail.Where(d_b => d_b.DetailId == masterP_1.DetailId).ToArray()
						}).ToArray()
					};

				var expectedQuery = from master_1 in masterRecords
					where master_1.Id1 > masterFilter
					select new
					{
						master_1.Id1,
						Details = detailRecords.Where(d_1 => d_1.MasterId == master_1.Id1).Select(masterP_1 => new
						{
							SubDetails = subDetailRecords.Where(d_b => d_b.DetailId == masterP_1.DetailId).ToArray()
						}).ToArray()
					};

				var result   = masterQuery.ToArray();
				var expected = expectedQuery.ToArray();

				AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
			}
		}

		[Test]
		public void TestWhenMasterIsNotConnected([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var (masterRecords, detailRecords) = GenerateDataManyId();

			using (new AllowMultipleQuery())
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
		public void TestSelectMany([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var (masterRecords, detailRecords, subDetailRecords) = GenerateDataWithSubDetail();

			using (new AllowMultipleQuery())
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
						SubDetails = subDetails.Where(sd => sd.DetailId == d.DetailId).ToArray()
					};

				var expectedQuery = from m in masterRecords.Take(20)
					from d in detailRecords
					select new
					{
						Detail = d,
						SubDetails = subDetailRecords.Where(sd => sd.DetailId == d.DetailId).ToArray()
					};

				var result   = query.ToArray();
				var expected = expectedQuery.ToArray();

				AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(result));
			}
		}

		[Test]
		public void TestJoin([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();

			using (new AllowMultipleQuery())
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
		public void TestPureGroupJoin([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();

			using (new AllowMultipleQuery())
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

				AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(result));
			}
		}

		[Test]
		public void TestGroupJoin([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var (masterRecords, detailRecords) = GenerateData();

			using (new AllowMultipleQuery())
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
						Masters = master.Where(mm => m.Id1 == dd.MasterId).ToArray()
					};

				var expectedQuery = from m in masterRecords.OrderByDescending(m => m.Id2).Take(20)
					join d in detailRecords on m.Id1 equals d.MasterId into j
					from dd in j
					select new
					{
						Master = m,
						Detail = dd,
						Masters = masterRecords.Where(mm => m.Id1 == dd.MasterId).ToArray()
					};

				var result   = query.ToArray();
				var expected = expectedQuery.ToArray();

				AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(result));
			}
		}

		[Test]
		public void TestTupleQueryingFabric([IncludeDataSources(TestProvName.AllSQLite)] string context)
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
		public void TestTupleQueryingNew([IncludeDataSources(TestProvName.AllSQLite)] string context)
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
				new Blog() { Id = 1, Title = "Another .NET Core Guy", Slogan = "Doing .NET Core Stuff", UserId = Guid.NewGuid().ToString("N") }
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
		public void Issue1862TestProjections([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (new AllowMultipleQuery())
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
						Tags = p.PostTags.Where(p => !p.IsDeleted).Select(t => new
						{
							Id = t.TagId,
							t.Tag.Name
						}).OrderBy(t => t.Id).ToArray()
					}).OrderBy(p => p.Id).ToArray()
				});

				var result = new
				{
					Blog = query.ToArray()
				};

				Assert.AreEqual(1, result.Blog.Length);
				Assert.AreEqual(1, result.Blog[0].Id);
				Assert.AreEqual("Another .NET Core Guy", result.Blog[0].Title);
				Assert.AreEqual(4, result.Blog[0].Posts.Length);

				Assert.AreEqual(1, result.Blog[0].Posts[0].Id);
				Assert.AreEqual("Post 1", result.Blog[0].Posts[0].Title);
				Assert.AreEqual("Content 1 is about EF Core and Razor page", result.Blog[0].Posts[0].PostContent);
				Assert.AreEqual(2, result.Blog[0].Posts[0].Tags.Length);
				Assert.AreEqual(1, result.Blog[0].Posts[0].Tags[0].Id);
				Assert.AreEqual("Razor Page", result.Blog[0].Posts[0].Tags[0].Name);
				Assert.AreEqual(2, result.Blog[0].Posts[0].Tags[1].Id);
				Assert.AreEqual("EF Core", result.Blog[0].Posts[0].Tags[1].Name);

				Assert.AreEqual(2, result.Blog[0].Posts[1].Id);
				Assert.AreEqual("Post 2", result.Blog[0].Posts[1].Title);
				Assert.AreEqual("Content 2 is about Dapper", result.Blog[0].Posts[1].PostContent);
				Assert.AreEqual(1, result.Blog[0].Posts[1].Tags.Length);
				Assert.AreEqual(3, result.Blog[0].Posts[1].Tags[0].Id);
				Assert.AreEqual("Dapper", result.Blog[0].Posts[1].Tags[0].Name);

				Assert.AreEqual(3, result.Blog[0].Posts[2].Id);
				Assert.AreEqual("Post 3", result.Blog[0].Posts[2].Title);
				Assert.AreEqual("Content 3", result.Blog[0].Posts[2].PostContent);
				Assert.AreEqual(0, result.Blog[0].Posts[2].Tags.Length);

				Assert.AreEqual(4, result.Blog[0].Posts[3].Id);
				Assert.AreEqual("Post 4", result.Blog[0].Posts[3].Title);
				Assert.AreEqual("Content 4", result.Blog[0].Posts[3].PostContent);
				Assert.AreEqual(1, result.Blog[0].Posts[3].Tags.Length);
				Assert.AreEqual(5, result.Blog[0].Posts[3].Tags[0].Id);
				Assert.AreEqual("SqlKata", result.Blog[0].Posts[3].Tags[0].Name);
			}
		}
		#endregion
	}
}
