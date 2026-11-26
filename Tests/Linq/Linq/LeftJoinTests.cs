using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class LeftJoinTests : TestBase
	{
		public class A
		{
			public int Id { get; set; }
		}

		public class B
		{
			public int Id { get; set; }
			public int AId { get; set; }
		}

		public class C
		{
			public int Id { get; set; }
			public int BId { get; set; }
		}

		[Test]
		public void left_join_on_sub_query_with_two_inner_joins_results_in_incorrect_SQL([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<A>())
			using (db.CreateLocalTable<B>())
			using (db.CreateLocalTable<C>())
			{
				db.Insert(new A { Id = 1 });

				var query =
					from a in db.GetTable<A>()

					join bc in
						from b in db.GetTable<B>()
						join c in db.GetTable<C>() on b.Id equals c.BId
						select new { aid = b.AId, bid = b.Id, cid = c.Id }
					on a.Id equals bc.aid into gbc

					from bc in gbc.DefaultIfEmpty()

					select new { aid = a.Id, bc };

				var result = query.ToArray();
				Assert.That(result, Has.Length.EqualTo(1));
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), ErrorMessage = "Cannot use the collection from a GroupJoin as an expression.")]
		public void LeftJoinGroupTest([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
				join c in db.Child on p.Value1 equals c.ParentID into g
				from c in g.DefaultIfEmpty()
				where g == null
				select p.ParentID;

			_ = q.ToSqlQuery().Sql;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1869")]
		public void LeftJoinDefaultIfEmptyOverGroupedAssociations([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<FtqMonth>())
			using (db.CreateLocalTable<FtqDefect>())
			using (db.CreateLocalTable<FtqRecord>())
			using (db.CreateLocalTable<FtqWorkstation>())
			using (db.CreateLocalTable<FtqWorkstationGroup>())
			{
				var dateMin = TestData.DateTime;
				var dateMax = TestData.DateTime;
				var sectorId = 1;

				var query1 = from q in db.GetTable<FtqRecord>()
							 where q.EntryDate >= dateMin
						  && q.EntryDate <= dateMax
						  && q.Defect.Workstation.WorkstationGroup.Id_Sector == sectorId
							 let MonthNumber = q.EntryDate.Month
							 group q by new { MonthNumber, q.Defect.Workstation.Id_WorkstationGroup }
					into g
							 select new
							 {
								 MonthNumber = g.Key.MonthNumber,
								 Id_WorkstationGroup = g.Key.Id_WorkstationGroup,
								 Ftq = g.Sum(_ => _.Qty) / g.Sum(_ => _.Defect.Ok ? 0 : _.Qty)
							 };

				var query2 = from q in query1
							 group q by q.MonthNumber
					into g
							 select new
							 {
								 MonthNumber = g.Key,
								 Ftq = g.Sum(_ => _.Ftq)
							 };

				var query3 = from m in db.GetTable<FtqMonth>()
							 from q in query2.Where(q => q.MonthNumber == m.MonthNumber).DefaultIfEmpty()
							 select new
							 {
								 m,
								 q
							 };

				var result = query3.ToArray();

				query3.GetSelectQuery().Select.Columns.Count.ShouldBe(3);
			}
		}

		public class FtqNotifierBase;

		[Table(Name = "tblFtq")]
		public class FtqRecord : FtqNotifierBase
		{
			[Column, PrimaryKey, Identity]
			public int Id { get; set; }

			[Column, NotNull]
			public DateTime EntryDate { get; set; }

			[Column, NotNull]
			public byte EntryShift { get; set; }

			[Column, NotNull]
			public int Id_Operator { get; set; }

			[Column, NotNull]
			public int Id_Reference { get; set; }

			[Column, NotNull]
			public int Id_Defect { get; set; }

			[Column, NotNull]
			public int Qty { get; set; }

			[Association(ThisKey = nameof(Id_Defect), OtherKey = nameof(FtqDefect.Id), CanBeNull = false)]
			public FtqDefect Defect { get; set; } = null!;
		}

		[Table(Name = "tblDefect")]
		public class FtqDefect : FtqNotifierBase
		{
			[Column, PrimaryKey, Identity]
			public int Id { get; set; }

			[Column, NotNull]
			public string Nam { get; set; } = null!;

			[Column, NotNull]
			public int Id_Workstation { get; set; }

			[Column, NotNull]
			public bool Ok { get; set; }

			[Column, NotNull]
			public bool Del { get; set; }

			[Association(ThisKey = nameof(Id_Workstation), OtherKey = nameof(FtqWorkstation.Id), CanBeNull = false)]
			public FtqWorkstation Workstation { get; set; } = null!;
		}

		[Table(Name = "tblWorkstation")]
		public class FtqWorkstation : FtqNotifierBase
		{
			[Column, PrimaryKey, Identity]
			public int Id { get; set; }

			[Column, NotNull]
			public int Id_WorkstationGroup { get; set; }

			[Column, NotNull]
			public string Nam { get; set; } = null!;

			[Column, NotNull]
			public bool Del { get; set; }

			[Association(ThisKey = nameof(Id_WorkstationGroup), OtherKey = nameof(FtqWorkstationGroup.Id), CanBeNull = false)]
			public FtqWorkstationGroup WorkstationGroup { get; set; } = null!;
		}

		[Table(Name = "tblWorkstationGroup")]
		public class FtqWorkstationGroup : FtqNotifierBase
		{
			[Column, PrimaryKey, Identity]
			public int Id { get; set; }

			[Column, NotNull]
			public string Nam { get; set; } = null!;

			[Column, NotNull]
			public int Id_SectorPart { get; set; }

			[Column, NotNull]
			public int Id_Sector { get; set; }

			[Column, NotNull]
			public bool Del { get; set; }
		}

		[Table(Name = "tblMonth")]
		public class FtqMonth : FtqNotifierBase
		{
			[Column]
			public int MonthNumber { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2008")]
		public void LeftJoinWithCorrelatedAnyInPredicate([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<LeftJoinAnyOuter>())
			using (db.CreateLocalTable<LeftJoinAnyInner>())
			using (db.CreateLocalTable<LeftJoinAnyFilter>())
			{
				var query = from rTable1 in db.GetTable<LeftJoinAnyOuter>()
					from rTable2 in db.GetTable<LeftJoinAnyInner>().Where(r2 => r2.Id_2 == rTable1.Id_1 &&
					                                                  db.GetTable<LeftJoinAnyFilter>().Any(r3 =>
						                                                  r3.Id_3 == rTable1.Id_1)
					).DefaultIfEmpty()
					select new { rTable1, rTable2 };
				var result = query.ToList();
			}
		}

		[Table]
		public class LeftJoinAnyOuter
		{
			[Column]
			public int Id_1 { get; set; }
			[Column]
			public int Description { get; set; }
		}

		[Table]
		public class LeftJoinAnyInner
		{
			[Column]
			public int Id_2 { get; set; }
			[Column]
			public int Description { get; set; }
		}

		[Table]
		public class LeftJoinAnyFilter
		{
			[Column]
			public int Id_3 { get; set; }
			[Column]
			public int Description { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2199")]
		public void LeftJoinSubqueriesCrossJoinedOnConstant([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<LeftJoinManufacturer>())
			using (db.CreateLocalTable<LeftJoinCountry>())
			{
				var man1 = from e in db.GetTable<LeftJoinManufacturer>()
					join c in db.GetTable<LeftJoinCountry>() on e.CountryCode equals c.Code into ce
					from co in ce.DefaultIfEmpty()
					where e.ManufacturerId == 1
					select new LeftJoinManufacturer { ManufacturerId=e.ManufacturerId, CountryCode=e.CountryCode,Name=e.Name,CountryEntity=co};

				var man2 = from e in db.GetTable<LeftJoinManufacturer>()
					join c in db.GetTable<LeftJoinCountry>() on e.CountryCode equals c.Code into ce
					from co in ce.DefaultIfEmpty()
					where e.ManufacturerId == 2
					select new LeftJoinManufacturer { ManufacturerId = e.ManufacturerId, CountryCode = e.CountryCode, Name = e.Name, CountryEntity = co };

				var query1 = from m1 in man1
					join m2 in man2 on 1 equals 1 into manjoin
					from m in manjoin.DefaultIfEmpty()
					select new Tuple<LeftJoinManufacturer, LeftJoinManufacturer>(m1, m);

				var result = query1.ToArray();
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2199")]
		public void LeftJoinMethodSubqueriesCrossJoinedOnConstant([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<LeftJoinManufacturer>())
			using (db.CreateLocalTable<LeftJoinCountry>())
			{
				var man1 = from e in db.GetTable<LeftJoinManufacturer>()
					from co in db.GetTable<LeftJoinCountry>().LeftJoin(co => co.Code == e.CountryCode)
					where e.ManufacturerId == 1
					select new LeftJoinManufacturer { ManufacturerId=e.ManufacturerId, CountryCode=e.CountryCode,Name=e.Name,CountryEntity=co};

				var man2 = from e in db.GetTable<LeftJoinManufacturer>()
					from co in db.GetTable<LeftJoinCountry>().LeftJoin(co => co.Code == e.CountryCode)
					where e.ManufacturerId == 2
					select new LeftJoinManufacturer { ManufacturerId = e.ManufacturerId, CountryCode = e.CountryCode, Name = e.Name, CountryEntity = co };

				var query1 = from m1 in man1
					join m2 in man2 on 1 equals 1 into manjoin
					from m in manjoin.DefaultIfEmpty()
					select new Tuple<LeftJoinManufacturer, LeftJoinManufacturer>(m1, m);

				var result = query1.ToArray();
			}
		}

		[Table("LeftJoinManufacturer")]
		public class LeftJoinManufacturer
		{
			[Column]
			[PrimaryKey]
			public int ManufacturerId { get; set; }

			[Column]
			public string? Name { get; set; }

			[Column]
			public string? CountryCode { get; set; }

			[Association(ThisKey = nameof(CountryCode), OtherKey = nameof(LeftJoinCountry.Code))]
			public LeftJoinCountry? CountryEntity { get; set; }
		}

		[Table("LeftJoinCountry")]
		public class LeftJoinCountry
		{
			[Column(CanBeNull = false)]
			[PrimaryKey]
			public string? Code { get; set; }

			[Column]
			public string? Name { get; set; }
		}

		[Sql.FunctionAttribute(Name = "UTILS.GREATESTNOTNULL3", ServerSideOnly = true, IsNullable = Sql.IsNullableType.Nullable)]
		private static decimal? UtilsGreatestnotnull3(decimal? value1, decimal? value2, decimal? value3)
		{
			throw new ServerSideOnlyException(nameof(UtilsGreatestnotnull3));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2832")]
		public void LeftJoinOfNestedSubqueriesFlattensSources([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
					from spt in db.GetTable<DctSetpointtype>()
					from t2 in (
						from w in db.GetTable<VWellTree>()
						join tp2 in (
								from c in db.GetTable<DctOu>()
								from cudg in db.GetTable<UacUsersDatagroup>().Where(cudg =>
										c.Id == cudg.DatagroupId && cudg.UserId == 150)
									.DefaultIfEmpty()
								from oudg in db.GetTable<UacUsersDatagroup>()
									.Where(oudg =>
										c.ParentId == oudg.DatagroupId && oudg.UserId == 150 &&
										oudg.Inheritablepermission > 0)
									.DefaultIfEmpty()
								let p = UtilsGreatestnotnull3(
									Sql.ToNullable(cudg.Permission),
									Sql.ToNullable(oudg.Inheritablepermission), null)
								where p.HasValue
								select new DataGroupPermission
								{
									DatagroupId = c.Id,
									Permission = Sql.Convert<int, decimal>(Sql.ToNotNull(p))
								}
							)
							on w.ShopId equals tp2.DatagroupId
						join d in db.GetTable<Deviation>() on w.WellId equals d.WellId
						select new {w.WellId, d.SetpointtypeId}
					).LeftJoin(x => x.SetpointtypeId == spt.Id).DefaultIfEmpty()
					select new {spt.Id};

			BaselinesManager.LogQuery(query.ToSqlQuery().Sql);

			var sourcesCount = QueryHelper.EnumerateAccessibleSources(query.GetSelectQuery()).Count(s => s.ElementType == QueryElementType.SqlQuery);

			// YDB has no correlated-subquery support (SqlProviderFlags.IsSupportedSimpleCorrelatedSubqueries is
			// false), so the optimizer cannot collapse these into the flat shape the other providers reach.
			Assert.That(sourcesCount, Is.LessThanOrEqualTo(context.IsAnyOf(TestProvName.AllYdb) ? 5 : 2));
		}

		public class DataGroupPermission
		{
			public int DatagroupId { get; set; }
			public int Permission { get; set; }
		}

		[Table]
		public class DctSetpointtype : PrimaryKeyEquality<DctSetpointtype>
		{
			[Column, PrimaryKey] public int Id { get; set; }
		}

		[Table]
		public class DctOu : PrimaryKeyEquality<DctOu>
		{
			[Column, PrimaryKey] public int Id { get; set; }

			[Column] public int? ParentId { get; set; }
		}

		public class PrimaryKeyEquality<T>;

		[Table]
		public class Deviation : PrimaryKeyEquality<Deviation>
		{
			[Column] public int SetpointtypeId { get; set; }
			[Column] public int WellId { get; set; }
		}

		[Table]
		public class UacUsersDatagroup : PrimaryKeyEquality<UacUsersDatagroup>
		{
			[Column, PrimaryKey(1)] public int UserId { get; set; }
			[Column, PrimaryKey(2)] public int DatagroupId { get; set; }

			[Column] public int Permission { get; set; }
			[Column] public int Inheritablepermission { get; set; }
		}

		[Table]
		public class VWellTree
		{
			[Column] public decimal? ShopId { get; set; }
			[Column] public int? WellId { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2961")]
		public void LeftJoinOfGroupedCountSubqueries([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Condo>())
			using (db.CreateLocalTable<CategoryCondo>())
			using (db.CreateLocalTable<CondoTag>())
			using (db.CreateLocalTable<Location>())
			{
				var condoCategories = from ccr in db.GetTable<CategoryCondo>()
					group ccr by ccr.CondoId
					into g1
					select new {CondoId = g1.Key, CountCondoCategories = g1.Count()};

				var condoTags = from ctr in db.GetTable<CondoTag>()
					group ctr by ctr.CondoId
					into g2
					select new {CondoId = g2.Key, CountCondoTags = g2.Count()};

				var sqlCondos = from c in db.GetTable<Condo>()
					join l in db.GetTable<Location>() on c.LocationId equals l.Id
					join ct in condoTags on c.Id equals ct.CondoId into ctleft
					from subct in ctleft.DefaultIfEmpty()
					join cc in condoCategories on c.Id equals cc.CondoId into ccleft
					from subcc in ccleft.DefaultIfEmpty()
					select new
					{
						Condo           = c,
						CategoriesCount = subcc.CountCondoCategories,
						TagsCount       = subct.CountCondoTags,
						LocationName    = l.LocationName,
						LocationId      = l.Id
					};

				sqlCondos.ToList();

				sqlCondos.ToSqlQuery().Sql.ShouldContain("COUNT(*)", AtLeast.Twice());
			}
		}

		[Table(Name = "Condos")]
		public class Condo
		{
			[Column(DataType = DataType.Int32)]
			[PrimaryKey]
			public int Id { get; set; } // int

			[Column(DataType = DataType.Int32)]
			[NotNull]
			public int LocationId { get; set; } // int
		}

		[Table(Name = "CategoryCondos")]
		public class CategoryCondo
		{
			[Column]
			[PrimaryKey(1)]
			public int CategoryId { get; set; } // uniqueidentifier

			[Column(DataType = DataType.Int32)]
			[PrimaryKey(2)]
			[NotNull]
			public int CondoId { get; set; } // int
		}

		[Table(Name = "CondoTags")]
		public class CondoTag
		{
			[Column(DataType = DataType.Int32)]
			[PrimaryKey(1)]
			[NotNull]
			public int CondoId { get; set; } // int

			[Column(DataType = DataType.Int32)]
			[PrimaryKey(2)]
			[NotNull]
			public int TagId { get; set; } // int
		}

		[Table(Name = "Locations")]
		public class Location
		{
			[Column(DataType = DataType.Int32)]
			[PrimaryKey]
			public int Id { get; set; } // int

			[Column(DataType = DataType.NVarChar, Length = 100)]
			[NotNull]
			public string LocationName { get; set; } = null!; // nvarchar(100)
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5328")]
		public void LeftJoinGroupedSubqueriesWithDefaultIfEmpty([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var table1Data = new[]
			{
				new LeftJoinContract { Id = 1, ContractNumber = "C001" },
				new LeftJoinContract { Id = 2, ContractNumber = "C002" }
			};

			var table2Data = new[]
			{
				new LeftJoinSupplement { Id = 1, LinkedId = 1, ContractNumber = "C001", Amount = 100m }
			};

			var table3Data = new[]
			{
				new LeftJoinTransaction { Id = 1, LinkedId = 1, ContractNumber = "C001", Type = "Type1", Amount = 500m },
				new LeftJoinTransaction { Id = 2, LinkedId = 1, ContractNumber = "C001", Type = "Type2", Amount = 200m },
				new LeftJoinTransaction { Id = 3, LinkedId = 2, ContractNumber = "C002", Type = "Type1", Amount = 300m },
				new LeftJoinTransaction { Id = 4, LinkedId = 2, ContractNumber = "C002", Type = "Type2", Amount = 150m }
			};

			using var db = GetDataContext(context);
			using var table1 = db.CreateLocalTable(table1Data);
			using var table2 = db.CreateLocalTable(table2Data);
			using var table3 = db.CreateLocalTable(table3Data);

			var result = (
				from t1 in table1
				from supplementTotal in (
					from t2 in table2.LeftJoin(t2 => t2.LinkedId == t1.Id)
					group t2 by t2.ContractNumber into t2Group
					select new
					{
						supplementAmount = t2Group.Sum(can => can.Amount)
					}
				).DefaultIfEmpty()
				from transactionsTotals in (
					from t3 in table3.LeftJoin(t3 => t3.LinkedId == t1.Id)
					group t3 by t3.ContractNumber into t3Group
					select new
					{
						amount1 = t3Group.Where(dt => dt.Type == "Type1").Sum(d => d.Amount),
						amount2 = t3Group.Where(dt => dt.Type == "Type2").Sum(d => d.Amount)
					}
				).DefaultIfEmpty()
				select new LeftJoinResult
				{
					Table1 = t1,
					AddedAmounts = (Sql.ToNullable(transactionsTotals.amount1) ?? 0m) - (Sql.ToNullable(supplementTotal.supplementAmount) ?? 0m),
					Amount1 = transactionsTotals.amount1,
					Amount2 = transactionsTotals.amount2,
				}
			).ToList();

			result.Count.ShouldBe(2);

			var result1 = result.First(r => r.Table1!.Id == 1);
			result1.Amount1.ShouldBe(500m);
			result1.Amount2.ShouldBe(200m);
			result1.AddedAmounts.ShouldBe(400m, "AddedAmounts should be 400 for ID 1");

			var result2 = result.First(r => r.Table1!.Id == 2);
			result2.Amount1.ShouldBe(300m);
			result2.Amount2.ShouldBe(150m);
			result2.AddedAmounts.ShouldNotBeNull("AddedAmounts should not be null when left join returns no results");
			result2.AddedAmounts.ShouldBe(300m, "AddedAmounts should be 300 (300-0) for ID 2");
		}

		[Table("LeftJoinContract")]
		public class LeftJoinContract
		{
			[Column, PrimaryKey]
			public int Id { get; set; }

			[Column]
			public string? ContractNumber { get; set; }
		}

		[Table("LeftJoinSupplement")]
		public class LeftJoinSupplement
		{
			[Column, PrimaryKey]
			public int Id { get; set; }

			[Column]
			public int LinkedId { get; set; }

			[Column]
			public string? ContractNumber { get; set; }

			[Column]
			public decimal Amount { get; set; }
		}

		[Table("LeftJoinTransaction")]
		public class LeftJoinTransaction
		{
			[Column, PrimaryKey]
			public int Id { get; set; }

			[Column]
			public int LinkedId { get; set; }

			[Column]
			public string? ContractNumber { get; set; }

			[Column]
			public string? Type { get; set; }

			[Column]
			public decimal Amount { get; set; }
		}

		public class LeftJoinResult
		{
			public LeftJoinContract? Table1 { get; set; }
			public decimal? AddedAmounts { get; set; }
			public decimal? Amount1 { get; set; }
			public decimal? Amount2 { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5576")]
		[ActiveIssue(5611, Configuration = TestProvName.AllSQLite, ErrorTypeName = "Shouldly.ShouldAssertException",
			ErrorMessage = "should be{0}5m{1}but was",
			Details = "SQLite renders decimal division as integer division: CAST(int AS Decimal) keeps NUMERIC/integer affinity")]
		public void LeftJoinLocalClassWithDecimalArithmetic([DataSources(TestProvName.AllNativeAccess, ProviderName.DB2, TestProvName.AllInformix)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<LeftJoinCampaign>();

			var g1 = TestData.Guid1;
			var g2 = TestData.Guid2;
			var g3 = TestData.Guid3;

			db.Insert(new LeftJoinCampaign { Guid = g1, Sold = 100 });
			db.Insert(new LeftJoinCampaign { Guid = g2, Sold = 40  });
			db.Insert(new LeftJoinCampaign { Guid = g3, Sold = 200 });

			var counts = new[]
			{
				new LeftJoinCounts { Key = g1, Count = 5  },
				new LeftJoinCounts { Key = g2, Count = 10 },
			};

			// Stage 1 — LEFT JOIN the in-memory multi-member source.
			var stage1 = table
				.LeftJoin(
					counts.AsQueryable(),
					(c, lc) => c.Guid == lc.Key,
					(c, lc) => new LeftJoinStat
					{
						CampaignGuid = c.Guid,
						LeadCount    = lc.Count,
						Sold         = c.Sold
					});

			// Stage 2 — decimal arithmetic on the (nullable) joined member -> Decimal descriptor.
			var stage2 = stage1.Select(s => new LeftJoinWithRate
			{
				CampaignGuid = s.CampaignGuid,
				LeadCount    = s.LeadCount,
				Rate         = s.LeadCount.HasValue ? s.LeadCount.Value / (decimal)s.Sold * 100 : (decimal?)null
			});

			// Stage 3 — re-mapping projection (data-model -> domain-model). This layering triggers the bug.
			var query = stage2.Select(r => new LeftJoinRateResult
			{
				CampaignGuid = r.CampaignGuid,
				Leads        = r.LeadCount,
				Rate         = r.Rate
			});

			var result = query.ToList();

			result.Count.ShouldBe(3);

			var r1 = result.Single(r => r.CampaignGuid == g1);
			r1.Leads.ShouldBe(5);
			r1.Rate.ShouldBe(5m);

			var r2 = result.Single(r => r.CampaignGuid == g2);
			r2.Leads.ShouldBe(10);
			r2.Rate.ShouldBe(25m);

			var r3 = result.Single(r => r.CampaignGuid == g3);
			r3.Leads.ShouldBeNull();
			r3.Rate.ShouldBeNull();
		}

		[Table]
		sealed class LeftJoinCampaign
		{
			// PrimaryKey: YDB rejects keyless tables; Guid is unique per row, so it doesn't affect the bug under test.
			[PrimaryKey] public Guid Guid { get; set; }
			[Column]     public int  Sold { get; set; }
		}

		// Multi-member class. Must be a class — an unmapped struct is treated as scalar (separate path).
		sealed class LeftJoinCounts
		{
			public Guid Key   { get; set; }
			public int  Count { get; set; }
		}

		// Stage 1: result of the LEFT JOIN, carrying the (nullable) joined member.
		sealed class LeftJoinStat
		{
			public Guid CampaignGuid { get; set; }
			public int? LeadCount    { get; set; }
			public int  Sold         { get; set; }
		}

		// Stage 2: adds the decimal rate computed from the joined member.
		sealed class LeftJoinWithRate
		{
			public Guid     CampaignGuid { get; set; }
			public int?     LeadCount    { get; set; }
			public decimal? Rate         { get; set; }
		}

		// Stage 3: a re-mapping projection (data-model -> domain-model).
		sealed class LeftJoinRateResult
		{
			public Guid     CampaignGuid { get; set; }
			public int?     Leads        { get; set; }
			public decimal? Rate         { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1760")]
		public void LeftJoinGroupedSubqueryChain([IncludeDataSources(TestProvName.AllSQLiteClassic)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<GroupedJoinMain>())
			using (db.CreateLocalTable<GroupedJoinSecond>())
			using (db.CreateLocalTable<GroupedJoinThird>())
			using (db.CreateLocalTable<GroupedJoinBTable>())
			using (db.CreateLocalTable<GroupedJoinCTable>())
			{
				int id = 0;
				var part1 = db.GetTable<GroupedJoinMain>()
					.GroupBy(w => w.CTb1LId)
					.Select(w => new
					{
						Col3 = w.Key,
						maxCol = w.Max(s => s.CTb1LId)
					})
					.LeftJoin(db.GetTable<GroupedJoinThird>(),
						(allE, tbl3) => allE.maxCol == tbl3.Id,
						(allG, tbl3) => new {allG.Col3, tbl3.Col})
					.LeftJoin(db.GetTable<GroupedJoinBTable>(),
						(allM, btbl) => allM.Col == btbl.Id,
						(allF, btbl) => new {all = allF, btbl})
					;

				var general = db.GetTable<GroupedJoinMain>()
					.Where(w => w.CommonTableId == id)
					.LeftJoin(db.GetTable<GroupedJoinSecond>(),
						(t1, bt1) => t1.CTb1LId == bt1.Id,
						(t1, bt1) => new {t1, bt1})
					.LeftJoin(part1,
						(allA, ctb) => allA.bt1.Col3 == ctb.btbl.Id,
						(allB, ctb) => new {allB.t1, allB.bt1, ctb})
					.LeftJoin(db.GetTable<GroupedJoinCTable>(),
						(allC, ctb2) => allC.bt1.TextCol == ctb2.Col1,
						(allD, ctb2) => new {allD.t1, allD.bt1, allD.ctb, ctb2})
					.GroupBy(s => new
					{
						s.ctb,
					})
					.Select(s => new {s.Key});

				_ = general.ToList();
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1760")]
		public void LeftJoinGroupedSubqueryChainQuerySyntax([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<GroupedJoinMain>())
			using (db.CreateLocalTable<GroupedJoinSecond>())
			using (db.CreateLocalTable<GroupedJoinThird>())
			using (db.CreateLocalTable<GroupedJoinBTable>())
			using (db.CreateLocalTable<GroupedJoinCTable>())
			{
				int id = 0;
				var grouping = from w in db.GetTable<GroupedJoinMain>()
						group w by w.CTb1LId into g
						select new
							{
								Id = g.Key,
								maxCol = g.Max(s => s.CTb1LId)
							};
				var part1 = from allE in grouping
					from tbl3 in db.GetTable<GroupedJoinThird>().LeftJoin(tbl3 => allE.maxCol == tbl3.Id)
					from btbl in db.GetTable<GroupedJoinThird>().LeftJoin(btbl => btbl.Col == tbl3.Col)
					select new
					{
						allE,
						tbl3,
						btbl
					};

				var general = from t1 in db.GetTable<GroupedJoinMain>()
					where t1.CommonTableId == id
					from bt1 in db.GetTable<GroupedJoinSecond>().LeftJoin(bt1 => t1.CTb1LId == bt1.Id)
					from ctb in part1.LeftJoin(ctb => bt1.Col3 == ctb.btbl.Id)
					from ctb2 in db.GetTable<GroupedJoinCTable>().LeftJoin(ctb2 => bt1.TextCol == ctb2.Col1)
					group ctb by ctb
					into g
					select new
					{
						g.Key
					};

				_ = general.ToList();
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1760")]
		public void LeftJoinGroupedInnerJoinSubqueryChain([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<GroupedJoinMain>())
			using (db.CreateLocalTable<GroupedJoinSecond>())
			using (db.CreateLocalTable<GroupedJoinThird>())
			using (db.CreateLocalTable<GroupedJoinBTable>())
			using (db.CreateLocalTable<GroupedJoinCTable>())
			{
				int id = 0;
				var part1 = db.GetTable<GroupedJoinMain>()
					.Where(w => w.CommonTableId == id)
					.InnerJoin(db.GetTable<GroupedJoinSecond>(),
						(tbl1, tbl2) => tbl1.IdTbl2 == tbl2.Id,
						(tbl1, tbl2) => new {tbl1, tbl2})
					.Where(w => w.tbl2.Col3.HasValue)
					.InnerJoin(db.GetTable<GroupedJoinThird>(),
						(all, tbl3) => all.tbl1.IdTbl3 == tbl3.Id,
						(all, tbl3) => new {all.tbl1, all.tbl2, tbl3})
					.GroupBy(w => w.tbl2.Col3)
					.Select(w => new
					{
						Col3 = w.Key,
						maxCol = w.Max(s => s.tbl3.Id)
					})
					.LeftJoin(db.GetTable<GroupedJoinThird>(),
						(allE, tbl3) => allE.maxCol == tbl3.Id,
						(allG, tbl3) => new {allG.Col3, tbl3.Col})
					.LeftJoin(db.GetTable<GroupedJoinBTable>(),
						(allM, btbl) => allM.Col == btbl.Id,
						(allF, btbl) => new {all = allF, btbl})
					.Select(w => new
					{
						c1 = w.all.Col3 ?? 0,
						b1 = w.btbl.Col1 ?? string.Empty,
						b2 = w.btbl.Col2 ?? string.Empty,
						b3 = w.btbl.Col3 ?? string.Empty,
					});

				var general = db.GetTable<GroupedJoinMain>()
					.Where(w => w.CommonTableId == id)
					.LeftJoin(db.GetTable<GroupedJoinSecond>(),
						(t1, bt1) => t1.CTb1LId == bt1.Id,
						(t1, bt1) => new {t1, bt1})
					.LeftJoin(part1,
						(allA, ctb) => allA.bt1.Col3 == ctb.c1,
						(allB, ctb) => new {allB.t1, allB.bt1, ctb})
					.LeftJoin(db.GetTable<GroupedJoinCTable>(),
						(allC, ctb2) => allC.bt1.TextCol == ctb2.Col1,
						(allD, ctb2) => new {allD.t1, allD.bt1, allD.ctb, ctb2})
					.GroupBy(s => new
					{
						s.ctb.b1,
						s.ctb.b2,
						s.ctb.b3,
						s.bt1.TextCol
					})
					.Select(s => new { s.Key.TextCol, s.Key.b1, s.Key.b2, s.Key.b3});

				_ = general.ToList();
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1760")]
		public void LeftJoinGroupedSubqueryWithProjectedKey([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<GroupedJoinMain>())
			using (db.CreateLocalTable<GroupedJoinSecond>())
			using (db.CreateLocalTable<GroupedJoinThird>())
			{
				var part1 = db.GetTable<GroupedJoinMain>()
					.InnerJoin(db.GetTable<GroupedJoinSecond>(),
						(tbl1, tbl2) => tbl1.IdTbl2 == tbl2.Id,
						(tbl1, tbl2) => new {tbl1, tbl2})
					.GroupBy(w => w.tbl2.Col3)
					.Select(w => new
					{
						Col3 = w.Key,
						maxCol = w.Max(s => s.tbl1.Id)
					})
					.LeftJoin(db.GetTable<GroupedJoinThird>(),
						(allE, tbl3) => allE.maxCol == tbl3.Id,
						(allG, tbl3) => new {c1=allG.Col3, b1=tbl3.Col});

				var general = db.GetTable<GroupedJoinMain>()
					.LeftJoin(part1,
						(allA, ctb) => allA.Id == ctb.c1,
						(allB, ctb) => allB.Id);

				_ = general.ToList();
			}
		}

		[Table(Schema="dbo", Name="b_table2")]
		public class GroupedJoinBTable
		{
			[Column("id"),   PrimaryKey, Identity] public int     Id   { get; set; } // int
			[Column("col1"), Nullable            ] public string? Col1 { get; set; } // varchar(50)
			[Column("col2"), Nullable            ] public string? Col2 { get; set; } // varchar(50)
			[Column("col3"), Nullable            ] public string? Col3 { get; set; } // varchar(50)
		}

		[Table(Schema="dbo", Name="commonTable")]
		public class GroupedJoinCommon
		{
			[Column("id"), PrimaryKey, Identity] public int Id { get; set; } // int
		}

		[Table(Schema="dbo", Name="c_table2")]
		public class GroupedJoinCTable
		{
			[Column("id"),          PrimaryKey, Identity] public int     Id        { get; set; } // int
			[Column("col1"),        Nullable            ] public string? Col1      { get; set; } // varchar(50)
			[Column("c_table3_id"), Nullable            ] public int?    CTable3Id { get; set; } // int
		}

		[Table(Schema="dbo", Name="GroupedJoinMain")]
		public class GroupedJoinMain
		{
			[Column("id"),            PrimaryKey,  Identity] public int  Id            { get; set; } // int
			[Column("id_tbl2"),          Nullable          ] public int? IdTbl2        { get; set; } // int
			[Column("id_tbl3"),          Nullable          ] public int? IdTbl3        { get; set; } // int
			[Column("commonTableId"), NotNull              ] public int  CommonTableId { get; set; } // int
			[Column("c_tb1l_Id"),        Nullable          ] public int? CTb1LId       { get; set; } // int
		}

		[Table(Schema="dbo", Name="GroupedJoinSecond")]
		public class GroupedJoinSecond
		{
			[Column("id"),      PrimaryKey, Identity] public int     Id      { get; set; } // int
			[Column("textCol"), Nullable            ] public string? TextCol { get; set; } // varchar(50)
			[Column("col3"),    Nullable            ] public int?    Col3    { get; set; } // int
		}

		[Table(Schema="dbo", Name="GroupedJoinThird")]
		public class GroupedJoinThird
		{
			[Column("id"),  PrimaryKey, Identity] public int  Id  { get; set; } // int
			[Column("col"), Nullable            ] public int? Col { get; set; } // int
		}

		private IQueryable<BranchInfoEntity> NotDeletedBranchInfo(IDataContext dc)
		{
			return dc.GetTable<BranchInfoEntity>().Where(m => m.Status == 0); 
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1964")]
		public void LeftJoinOnRowNumberFilteredSubquery([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<AttachmentEntity>())
			using (db.CreateLocalTable<BranchInfoEntity>())
			{
				var attachmentQry = from att in (from temp in db.GetTable<AttachmentEntity>()
						where temp.Status == 0 && temp.Type == 2
						select new
						{
							temp.ItemId,
							temp.Url,
							Index = Sql.Ext.RowNumber().Over().PartitionBy(temp.ItemId).OrderBy(temp.CreateTime)
								.ToValue()
						})
					where att.Index == 1
					select att;

				var qry = 
					from branchInfo in NotDeletedBranchInfo(db)
					from att in attachmentQry.LeftJoin(m => m.ItemId == branchInfo.BranchId)
					where branchInfo.BranchIsShow == 0
					select new BranchSelectOutput
					{
						BranchAddress = branchInfo.BranchAddress,
						BranchId = branchInfo.BranchId,
						BranchName = branchInfo.BranchName,
						BusinessHours = branchInfo.BusinessHours,
						PointX = branchInfo.PointX,
						PointY = branchInfo.PointY,
						BranchPic = att.Url
					};

				qry.ToList();
			}
		}

		public class BranchSelectOutput
		{
			public int    BranchId      { get; set; }
			public string BranchName    { get; set; } = null!;
			public string BranchAddress { get; set; } = null!;
			public string BusinessHours { get; set; } = null!;
			public double Distance      { get; set; }
			public string BranchPic     { get; set; } = null!;

			public decimal PointX { get; set; }
			public decimal PointY { get; set; }
		}

		[Table(Schema="dbo", Name="RowNumAttachment")]
		public partial class AttachmentEntity
		{
			[PrimaryKey, Identity] public int      Id          { get; set; } // int
			[Column,     NotNull ] public int      Type        { get; set; } // int
			[Column,     NotNull ] public int      ItemId      { get; set; } // int
			[Column,     NotNull ] public string   Name        { get; set; } = null!; // nvarchar(128)
			[Column,     NotNull ] public string   ContentType { get; set; } = null!; // nvarchar(64)
			[Column,     NotNull ] public string   Url         { get; set; } = null!; // nvarchar(128)
			[Column,     NotNull ] public int      Status      { get; set; } // int
			[Column,     NotNull ] public DateTime CreateTime  { get; set; } // datetime
		}

		[Table(Schema="dbo", Name="BranchInfo")]
		public partial class BranchInfoEntity
		{
			[PrimaryKey, Identity] public int      BranchId         { get; set; } // int
			[Column,     NotNull ] public string   BranchCode       { get; set; } = null!; // varchar(50)
			[Column,     NotNull ] public string   BranchName       { get; set; } = null!; // varchar(50)
			[Column,     NotNull ] public string   BranchParentCode { get; set; } = null!; // varchar(50)
			[Column,     NotNull ] public string   CompanyCode      { get; set; } = null!; // varchar(32)
			[Column,     NotNull ] public string   AreaCode         { get; set; } = null!; // varchar(50)
			[Column,     NotNull ] public string   BranchAddress    { get; set; } = null!; // varchar(100)
			[Column,     NotNull ] public string   BrandContacts    { get; set; } = null!; // varchar(20)
			[Column,     NotNull ] public string   BrandPhone       { get; set; } = null!; // varchar(20)
			[Column,     NotNull ] public int      Status           { get; set; } // int
			[Column,     NotNull ] public DateTime CreateTime       { get; set; } // datetime
			[Column,     NotNull ] public string   CreateUser       { get; set; } = null!; // varchar(50)
			[Column,     NotNull ] public DateTime LastUpdateTime   { get; set; } // datetime
			[Column,     NotNull ] public string   LastUpdateUser   { get; set; } = null!; // varchar(50)
			[Column,     NotNull ] public string   BusinessHours    { get; set; } = null!; // varchar(100)
			[Column,     NotNull ] public decimal  PointX           { get; set; } // decimal(18, 4)
			[Column,     NotNull ] public decimal  PointY           { get; set; } // decimal(18, 4)
			[Column,     NotNull ] public int      BranchIsShow     { get; set; } // int
			[Column,     NotNull ] public int      BranchType       { get; set; } // int
		}
	}
}
