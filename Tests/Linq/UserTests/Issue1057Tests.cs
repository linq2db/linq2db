using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[ActiveIssue(1057)]
	public class Issue1057Tests : TestBase
	{
		[Table, InheritanceMapping(Code = "bda.Requests", Type = typeof(BdaTask))]
		class Task
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; set; }

			[Column(IsDiscriminator = true)]
			public string TargetName { get; set; }

			[Association(ExpressionPredicate = nameof(ActualStageExp))]
			public TaskStage ActualStage { get; set; }

			private static Expression<Func<Task, TaskStage, bool>> ActualStageExp()
				=> (t, ts) => t.Id == ts.TaskId && ts.Actual == true;
		}

		class BdaTask : Task
		{
			public const string Code = "bda.Requests";
		}

		[Table]
		class TaskStage
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; set; }

			[Column]
			public int TaskId { get; set; }

			[Column]
			public bool Actual { get; set; }
		}


		[Test]
		public void Test()
		{
			DataConnection.AddConfiguration("default", "Data Source=:memory:", new LinqToDB.DataProvider.SQLite.SQLiteDataProvider());
			DataConnection.DefaultConfiguration = "default";

			var db = new DataConnection();
			db.CreateTable<Task>();
			db.CreateTable<TaskStage>();

			db.Insert(new Task { Id = 1, TargetName = "bda.Requests" });
			db.Insert(new TaskStage { Id = 1, TaskId = 1, Actual = true });

			var query = db.GetTable<Task>()
				.OfType<BdaTask>()
				.Select(p => new
				{
					Instance = (Task)p, //without cast throw other exception
					ActualStageId = (p as Task).ActualStage.Id
				});
			var res = query.ToArray(); //this call throw exception
		}
	}
}
