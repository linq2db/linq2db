using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3402Tests : TestBase
	{
		[Table(Name = "VEMPLOYEE_SCH_SEC")]
		public class EmployeeScheduleSection
		{
			[Column(Name = "ACTIVE", CanBeNull = false)]
			public bool Active { get; set; }

			[Column(Name = "ID", CanBeNull = false, IsPrimaryKey = true)]
			public int ID { get; set; }

			[Column(Name = "NAME", CanBeNull = false)]
			public string Name { get; set; } = null!;

			[Association(ThisKey = "ID", OtherKey = "EmployeeScheduleSectionID")]
			public List<EmployeeScheduleSectionAdditionalPermission> AdditionalPermissions { get; set; } = null!;
		}

		[Table(Name = "VEMPLOYEE_SCHDL_PERM")]
		public class EmployeeScheduleSectionAdditionalPermission
		{
			[Column(Name = "ID", CanBeNull = false, IsPrimaryKey = true)]
			public int EmployeeScheduleSectionID { get; set; }

			[Column(Name = "IS_ACTIVE", CanBeNull = false)]
			public bool IsActive { get; set; }
		}

		[Test]
		public void ColumnOptimization([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<EmployeeScheduleSection>())
			using (db.CreateLocalTable<EmployeeScheduleSectionAdditionalPermission>())
			{
				var fullAccess = false;

				var permissions = from ess in db.GetTable<EmployeeScheduleSection>()
					let allowEdit = fullAccess || ess.AdditionalPermissions.Any(y => y.IsActive)
					where allowEdit
					select new
					{
						SectionID = ess.ID,
					};

				var data1 = permissions.ToList();

				fullAccess = true;

				permissions = from ess in db.GetTable<EmployeeScheduleSection>()
					let allowEdit = fullAccess || ess.AdditionalPermissions.Any(y => y.IsActive)
					where allowEdit
					select new
					{
						SectionID = ess.ID,
					};

				var data2 = permissions.ToList(); 
			}
		}

		[Test]
		public void SubQueryAny([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<EmployeeScheduleSection>())
			using (db.CreateLocalTable<EmployeeScheduleSectionAdditionalPermission>())
			{
				var query = from ess in db.GetTable<EmployeeScheduleSection>()
					let hasAdditionalPermissions = ess.AdditionalPermissions.Any(y => y.IsActive)
					where hasAdditionalPermissions
					select new
					{
						SectionID = ess.ID,
					};

				var result = query.ToList();
			}
		}
	}
}
