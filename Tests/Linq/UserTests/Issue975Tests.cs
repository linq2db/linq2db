using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue975Tests : TestBase
	{
		public static class SqlServer
		{
			[Sql.Function(ServerSideOnly = true, CanBeNull = false)]
			public static DateTime GetDate() { throw new InvalidOperationException("Use only LINQ expression"); }
		}

		public interface ITargetAware
		{
			string TargetName { get; }
			int    TargetId   { get; }
		}

		[Table(Name="TaskStages")]
		public partial class TaskStage
		{
			[PrimaryKey, Identity   ] public int      Id           { get; set; } // Int
			[Column,     NotNull    ] public int      TaskId       { get; set; } // Int
			[Column,     NotNull    ] public int      StageId      { get; set; } // Int
			[Column,     NotNull    ] public DateTime SwitchDate   { get; set; } // DateTime
			[Column,     NotNull    ] public bool     Actual       { get; set; } // Bit
			[Column,        Nullable] public int?     WorkReportId { get; set; } // Int
			[Column,        Nullable] public int?     UserId       { get; set; } // Int
		}

		[Table(Name="Tasks")]
		public partial class Task : ITargetAware
		{
			[PrimaryKey, Identity   ] public int       Id          { get; set; } // Int
			[Column,     NotNull    ] public DateTime  DateBegin   { get; set; } // DateTime
			[Column,        Nullable] public DateTime? DateEnd     { get; set; } // DateTime
			[Column,     NotNull    ] public Guid      DirectionId { get; set; } // UniqueIdentifier
			[Column,     NotNull    ] public string    Text        { get; set; } = null!; // varchar(1000)
			[Column,     NotNull    ] public string    TargetName  { get; set; } = null!; // nvarchar(128)
			[Column,     NotNull    ] public int       TargetId    { get; set; } // Int
			[Column,        Nullable] public int?      ParentId    { get; set; } // Int

			[Association(ThisKey =nameof(Id), OtherKey =nameof(TaskStage.TaskId), ExpressionPredicate = nameof(ActualTaskExp), CanBeNull =true)]
			public IEnumerable<TaskStage> ActualStage { get; set; } = null!;

			private static Expression<Func<Task, TaskStage, bool>> ActualTaskExp() => (t, ts) => ts.Actual == true;
		  }

		[Table(Name="Assignments")]
		public partial class Assignment : ITargetAware
		{
			[PrimaryKey, Identity   ] public int       Id          { get; set; } // Int
			[Column,     NotNull    ] public int       EmployeeId  { get; set; } // Int
			[Column,     NotNull    ] public DateTime  DateAssign  { get; set; } // DateTime
			[Column,        Nullable] public DateTime? DateRevoke  { get; set; } // DateTime
			[Column,     NotNull    ] public Guid      DirectionId { get; set; } // UniqueIdentifier
			[Column,     NotNull    ] public string    TargetName  { get; set; } = null!; // nvarchar(128)
			[Column,     NotNull    ] public int       TargetId    { get; set; } // Int
		}

		[Test]
		public void Test([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<Task>())
				using (db.CreateLocalTable<TaskStage>())
				using (db.CreateLocalTable<Assignment>())
				{
					var directionId = TestData.Guid1;
					var taskId = db.GetTable<Task>().InsertWithInt32Identity(() => new Task
					{
						DirectionId = directionId,
						TargetId = 1,
						TargetName = "TN",
						Text = "SomeText",
						DateBegin = Sql.CurrentTimestamp
					});

					db.GetTable<Assignment>().Insert(() => new Assignment
					{
						DirectionId = directionId,
						TargetId = 1,
						TargetName = "TN",
						EmployeeId = 10,
						DateAssign = Sql.CurrentTimestamp
					});

					db.GetTable<TaskStage>().Insert(() => new TaskStage
					{
						Actual = true,
						TaskId = taskId,
						StageId = 800,
						SwitchDate = Sql.CurrentTimestamp,
					});

					var employeeId = 10;
					var query = (from t in db.GetTable<Task>()
							join a in db.GetTable<Assignment>()
								on new {t.DirectionId, t.TargetId, t.TargetName}
								equals new {a.DirectionId, a.TargetId, a.TargetName}
							where a.EmployeeId == employeeId
								  && (a.DateRevoke == null || a.DateRevoke > SqlServer.GetDate())
							select t)
						.Distinct()
#pragma warning disable CS0472 // comparison of non-null int? with null
						.Where(it => it.ActualStage.Any(d => d.StageId < 9000 || ((int?)d.StageId) == null));
#pragma warning restore CS0472

					var zz = query.ToArray();
				}
			}
		}
	}
}
