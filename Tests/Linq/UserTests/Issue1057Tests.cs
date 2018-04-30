using System;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;

namespace Tests.UserTests
{
	using LinqToDB;
	using LinqToDB.Mapping;

	public class Issue1057Tests : TestBase
	{
		[Table, InheritanceMapping(Code = "bda.Requests", Type = typeof(BdaTask))]
		[Table, InheritanceMapping(Code = "None",         Type = typeof(NonBdaTask))]
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

		class BdaTask : Task
		{
			public const string Code = "bda.Requests";
		}

		class NonBdaTask : Task
		{
			public const string Code = "None";
		}

		[Table]
		class TaskStage
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; set; }

			[Column]
			public int TaskId { get; set; }

			[Column]
			[Column(Configuration = ProviderName.DB2     , DbType = "char")]
			[Column(Configuration = ProviderName.Firebird, DbType = "char(1)")]
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
					db.Insert(new Task { Id = 2, TargetName = "None" });
					db.Insert(new TaskStage { Id = 2, TaskId = 1, Actual = true});

					var query = db.GetTable<Task>()
						.OfType<BdaTask>()
						.Select(p => new
						{
							Instance = p,
							ActualStageId = p.ActualStage.Id
						});
					var res = query.ToArray();

					Assert.AreEqual(1, res.Length);
					Assert.IsNotNull(  res[0].Instance);
					Assert.AreEqual(2, res[0].ActualStageId);
				}
				finally
				{
					db.DropTable<Task>();
					db.DropTable<TaskStage>();
				}
			}
		}

		[Test, DataContextSource]
		public void Test2(string configuration)
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
					db.Insert(new Task { Id = 2, TargetName = "None" });
					db.Insert(new TaskStage { Id = 2, TaskId = 1, Actual = true });

					var query = db.GetTable<Task>()
						.OfType<BdaTask>()
						.Select(p => new
						{
							Instance = p,
							ActualStageId = (p as Task).ActualStage.Id
						});
					var res = query.ToArray();

					Assert.AreEqual(1, res.Length);
					Assert.IsNotNull(  res[0].Instance);
					Assert.AreEqual(2, res[0].ActualStageId);


					var query2 = db.GetTable<Task>()
						.Select(p => new
						{
							Instance = p,
							ActualStageId = (p as Task).ActualStage.Id
						});

					var res2 = query2.ToArray();

					Assert.AreEqual(2, res2.Length);
					Assert.IsNotNull(  res2[0].Instance);
					Assert.AreEqual(2, res2[0].ActualStageId);


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
