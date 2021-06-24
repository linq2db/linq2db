using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Tools;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3061Tests : TestBase
	{
		[Table]
		class Properties
		{
			[PrimaryKey]
			public int     Id    { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(CaseLogProperty.PropertyId))]
			public List<CaseLogProperty> CaseLogProperties { get;   set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(IncidentProperty.PropertyId))]
			public List<IncidentProperty> IncidentProperties { get; set; } = null!;
		}

		class CaseLog
		{
			[PrimaryKey]
			public int  Id     { get; set; }

			public int? Number { get; set; }
		}

		class Incident
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column(CanBeNull = true)]
			public int? EventNumber { get; set; }
		}

		class CaseLogProperty
		{
			[Column(CanBeNull = true)]
			public int? PropertyId { get; set; }
			[Column(CanBeNull = true)]
			public int? CaseLogId { get; set; }

			[Association(ThisKey = nameof(CaseLogId), OtherKey = nameof(Issue3061Tests.CaseLog.Id))]
			public CaseLog CaseLog { get; set; } = null!;
		}

		class IncidentProperty
		{
			[Column(CanBeNull = true)]
			public int? PropertyId { get; set; }
			[Column(CanBeNull = true)]
			public int? IncidentId  { get; set; }

			[Association(ThisKey = nameof(IncidentId), OtherKey = nameof(Issue3061Tests.Incident.Id))]
			public Incident Incident { get; set; } = null!;
		}

		[Test]
		public void TestColumnsOptimization([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.GetTable<Properties>()
					.Where(x => x.Id.In(1, 2))
					.Select(x => new
					{
						CaseNumber     = x.CaseLogProperties.FirstOrDefault().CaseLog.Number,
						IncidentNumber = x.IncidentProperties.FirstOrDefault().Incident.EventNumber
					});

				TestContext.WriteLine(query.ToString());

				query.GetSelectQuery().Select.Columns.Should().HaveCount(4);
			}
		}
	}
}
