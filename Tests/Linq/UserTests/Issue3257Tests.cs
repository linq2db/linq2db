using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3257Tests : TestBase
	{
		[Table]
		public class Checklist
		{
			[Column] public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(ChecklistTrigger.ChecklistId))]
			public ICollection<ChecklistTrigger> ChecklistTriggers { get; set; } = null!;
		}

		[Table]
		public class ChecklistTrigger
		{
			[Column] public int         Id          { get; set; }
			[Column] public int         ChecklistId { get; set; }
			[Column] public TriggerType TriggerType { get; set; }
		}

		public enum TriggerType
		{
			Hired      = 1,
			PreHired   = 2,
			Terminated = 3
		}

		[Sql.Expression(ProviderName.SqlServer, "ISNULL({0}, {1})", ServerSideOnly = true)]
		private static T IsNull<T>(T value, T defaultValue)
		{
			throw new NotImplementedException();
		}

		[Test]
		public void Test1([IncludeDataSources(true, TestProvName.AllSqlServer2017Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Checklist>())
			using (db.CreateLocalTable<ChecklistTrigger>())
			{
				var query = db.GetTable<Checklist>()
					.Select(x => new
					{
						x.Id,
						Triggers = x.ChecklistTriggers.Any()
							? x.ChecklistTriggers.AsQueryable()
								.Select(checklist => checklist.TriggerType == TriggerType.Hired ? "Hired" :
									checklist.TriggerType == TriggerType.PreHired ? "PreHired" :
									checklist.TriggerType == TriggerType.Terminated ? "Terminated" : "")
								.StringAggregate(",").ToValue()
						: "None"
					});

				var good = query.ToList();
			}
		}

		[Test]
		public void Test2([IncludeDataSources(true, TestProvName.AllSqlServer2017Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Checklist>())
			using (db.CreateLocalTable<ChecklistTrigger>())
			{
				var query = db.GetTable<Checklist>()
					.Select(x => new
					{
						x.Id,
						Triggers = x.ChecklistTriggers.Any()
							? x.ChecklistTriggers.AsQueryable()
								.Select(checklist => checklist.TriggerType == TriggerType.Hired ? "Hired" :
									checklist.TriggerType == TriggerType.PreHired ? "PreHired" :
									checklist.TriggerType == TriggerType.Terminated ? "Terminated" : "")
								.StringAggregate(",").ToValue()
						: "None"
					});

				var bad = query.Where(x => x.Triggers.Contains("H")).ToList();
			}
		}

		[Test]
		public void Test3([IncludeDataSources(true, TestProvName.AllSqlServer2017Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Checklist>())
			using (db.CreateLocalTable<ChecklistTrigger>())
			{
				var query = db.GetTable<Checklist>()
					.Select(x => new
					{
						x.Id,
						Triggers = IsNull(
							x.ChecklistTriggers.AsQueryable()
								.Select(checklist => checklist.TriggerType == TriggerType.Hired ? "Hired" :
									checklist.TriggerType == TriggerType.PreHired ? "PreHired" :
									checklist.TriggerType == TriggerType.Terminated ? "Terminated" : "")
								.StringAggregate(",").ToValue()
						, "None")
					});

				query.ToArray();
			}
		}
	}
}
