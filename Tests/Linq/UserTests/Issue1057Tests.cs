using System;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;

namespace Tests.UserTests
{
	using LinqToDB;
	using LinqToDB.Mapping;

	public class Issue1057Tests: TestBase
	{
		[Table, InheritanceMapping(Code = "bda.Requests", Type = typeof(BdaTask))]
		class Task
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; set; }

			[Column(IsDiscriminator = true)]
			public string TargetName { get; set; }

			[Association(ExpressionPredicate =nameof(ActualStageExp))]
			public TaskStage ActualStage { get; set; }

			private static Expression<Func<Task, TaskStage, bool>> ActualStageExp()
				=> (t, ts) => t.Id == ts.TaskId && ts.Actual == true;
		}

		class BdaTask:Task
		{
			public const string Code = "bda.Requests";
		}

		[Table]
		class TaskStage
		{
			[Column(IsPrimaryKey =true)]
			public int Id { get; set; }

			[Column]
			public int TaskId { get; set; }

			[Column]
			public bool Actual { get; set; }
		}


		[Test, DataContextSource]
		public void Test(string configuration)
		{
			using (var db = GetDataContext(configuration))
			{
				try
				{
					db.CreateTable<Task>();
					db.CreateTable<TaskStage>();
				}
				catch 
				{
					db.DropTable<Task>(throwExceptionIfNotExists: false);
					db.DropTable<TaskStage>(throwExceptionIfNotExists: false);

					db.CreateTable<Task>();
					db.CreateTable<TaskStage>();
				}

				try
				{
					db.Insert(new Task { Id = 1, TargetName = "bda.Requests" });
					db.Insert(new TaskStage { Id = 1, TaskId = 1, Actual = true});

					var query = db.GetTable<Task>()
						.OfType<BdaTask>()
						.Select(p => new
						{
							Instance = p, //without cast throw other exception
							ActualStageId = p.ActualStage.Id
						});
					var res = query.ToArray(); //this call throw exception					
				}
				finally
				{
					db.DropTable<Task>();
					db.DropTable<TaskStage>();
				}
			}
		}

	}
}
