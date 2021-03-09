using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

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

		public class PrimaryKeyEquality<T>
		{
		}

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

		[Test]
		public void TestIssue2832([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<DctSetpointtype>())
			using (db.CreateLocalTable<VWellTree>())
			using (db.CreateLocalTable<DctOu>())
			using (db.CreateLocalTable<UacUsersDatagroup>())
			using (db.CreateLocalTable<Deviation>())
			{
				var query =
					db.GetTable<DctSetpointtype>()
					.SelectMany(
						spt =>
					db.GetTable<VWellTree>()
							.Join(
								db.GetTable<DctOu>()
									.SelectMany(
										c => db.GetTable<UacUsersDatagroup>()
											.Where(cudg => ((c.Id == cudg.DatagroupId) && (cudg.UserId == 150)))
											.DefaultIfEmpty(),
										(c, cudg) => new
										{
											c = c,
											cudg = cudg
										})
									.SelectMany(
										tp0 => db.GetTable<UacUsersDatagroup>()
											.Where(oudg => (((tp0.c.ParentId == (long?)oudg.DatagroupId) && (oudg.UserId == 150)) && (oudg.Inheritablepermission > 0)))
											.DefaultIfEmpty(),
										(tp0, oudg) => new
										{
											tp0 = tp0,
											oudg = oudg
										})
									.Select(
										tp1 => new
										{
											tp1 = tp1,
											p = (decimal?)(tp1.tp0.cudg.Permission + tp1.oudg.Inheritablepermission)
										})
									.Where(tp2 => tp2.p.HasValue)
									.Select(
										tp2 => new DataGroupPermission()
										{
											DatagroupId = tp2.tp1.tp0.c.Id,
											Permission = Sql.Convert<int,decimal>(Sql.ToNotNull<decimal>(tp2.p))
										}),
								w => w.ShopId,
								flt => (decimal?)(decimal)flt.DatagroupId,
								(w, flt) => new
								{
									w = w,
									flt = flt
								})
							.Join(
								db.GetTable<Deviation>(),
								tp0 => tp0.w.WellId,
								d => (long?)d.WellId,
								(tp0, d) => new
								{
									WellId = tp0.w.WellId,
									SetpointtypeId = d.SetpointtypeId
								})
							.LeftJoin(x => x.SetpointtypeId == spt.Id)
							.DefaultIfEmpty(),
						(spt, t2) => new { Id = spt.Id });

				query.ToList();
			}
		}
	}
}
