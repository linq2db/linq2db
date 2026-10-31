using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2832Tests : TestBase
	{
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

		[Sql.FunctionAttribute(Name = "UTILS.GREATESTNOTNULL3", ServerSideOnly = true, IsNullable = Sql.IsNullableType.Nullable)]
		private static decimal? UtilsGreatestnotnull3(decimal? value1, decimal? value2, decimal? value3)
		{
			throw new ServerSideOnlyException(nameof(UtilsGreatestnotnull3));
		}

		[ActiveIssue(6216, Configuration = TestProvName.AllAccess, ErrorMessage = "Assert.That(sourcesCount, Is.LessThanOrEqualTo(",
			Details = "Access wraps joined-side LEFT JOIN ON predicates into derived tables: 4 sources where 2 are allowed.")]
		[Test]
		public void TestIssue2832([DataSources] string context)
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

			// Without complex join conditions the subquery's WHERE must stay in the derived table, not land in the LEFT JOIN ON.
			if (context.IsAnyOf(TestProvName.AllYdb, TestProvName.AllAccess))
				query.GetSelectQuery().From.Tables[0].Joins[0].Condition.Predicates.Count.ShouldBe(1);

			var sourcesCount = QueryHelper.EnumerateAccessibleSources(query.GetSelectQuery()).Count(s => s.ElementType == QueryElementType.SqlQuery);

			// YDB has no correlated-subquery support (SqlProviderFlags.IsSupportedSimpleCorrelatedSubqueries is
			// false), so the optimizer cannot collapse these into the flat shape the other providers reach.
			Assert.That(sourcesCount, Is.LessThanOrEqualTo(context.IsAnyOf(TestProvName.AllYdb) ? 5 : 2));
		}
	}
}
