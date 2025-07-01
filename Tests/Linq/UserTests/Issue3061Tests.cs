using System.Collections.Generic;
using System.Linq;

using Shouldly;

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
		sealed class Properties
		{
			[PrimaryKey]
			public int     Id    { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(CaseLogProperty.PropertyId))]
			public List<CaseLogProperty> CaseLogProperties { get;   set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(IncidentProperty.PropertyId))]
			public List<IncidentProperty> IncidentProperties { get; set; } = null!;
		}

		sealed class CaseLog
		{
			[PrimaryKey]
			public int  Id     { get; set; }

			public int? Number { get; set; }
		}

		sealed class Incident
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column(CanBeNull = true)]
			public int? EventNumber { get; set; }
		}

		sealed class CaseLogProperty
		{
			[Column(CanBeNull = true)]
			public int? PropertyId { get; set; }
			[Column(CanBeNull = true)]
			public int? CaseLogId { get; set; }

			[Association(ThisKey = nameof(CaseLogId), OtherKey = nameof(Issue3061Tests.CaseLog.Id))]
			public CaseLog CaseLog { get; set; } = null!;
		}

		sealed class IncidentProperty
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
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<Properties>();
			using var t2 = db.CreateLocalTable<CaseLog>();
			using var t3 = db.CreateLocalTable<CaseLogProperty>();
			using var t4 = db.CreateLocalTable<Incident>();
			using var t5 = db.CreateLocalTable<IncidentProperty>();

			var query = db.GetTable<Properties>()
					.Where(x => x.Id.In(1, 2))
					.Select(x => new
					{
						CaseNumber     = x.CaseLogProperties.FirstOrDefault()!.CaseLog.Number,
						IncidentNumber = x.IncidentProperties.FirstOrDefault()!.Incident.EventNumber
					});

			query.GetSelectQuery().Select.Columns.Count.ShouldBe(2);

			query.ToArray();
		}

		[Table]
		sealed class Root
		{
			[Column] public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Draft1.RootId))]
			public ICollection<Draft1> SomeDrafts { get; set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Draft2.RootId))]
			public ICollection<Draft2> OtherDrafts { get; set; } = null!;
		}

		[Table]
		sealed class Draft1
		{
			[Column] public int     RootId { get; set; }
			[Column] public string? Html   { get; set; }
			[Column] public string? Plain  { get; set; }
		}

		[Table]
		sealed class Draft2
		{
			[Column] public int     RootId { get; set; }
			[Column] public string? Html   { get; set; }
			[Column] public string? Plain  { get; set; }
		}

		[Test]
		public void TestColumnsOptimization3487([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<Root>();
			using var t2 = db.CreateLocalTable<Draft1>();
			using var t3 = db.CreateLocalTable<Draft2>();

			var query = db.GetTable<Root>()
					.Select(x => new
					{
						NarrativeDraft = x.SomeDrafts
							.Select(y => new
							{
								Html   = y.Html,
								Plain2 = y.Plain
							})
							.FirstOrDefault(),
						SynopsisDraft = x.OtherDrafts
							.Select(y => new
							{
								Html  = y.Html,
								Plain = y.Plain
							})
							.FirstOrDefault()
					});

			query.GetSelectQuery().Select.Columns.Count.ShouldBe(6);
			query.ToArray();
		}
	}
}
