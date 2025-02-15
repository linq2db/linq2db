﻿using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	public class Issue1057Tests : TestBase
	{
		[Table, InheritanceMapping(Code = "bda.Requests", Type = typeof(BdaTask))]
		[Table, InheritanceMapping(Code = "None", Type = typeof(NonBdaTask))]
		class Task
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; set; }

			[Column(IsDiscriminator = true)]
			public string? TargetName { get; set; }

			[Association(ExpressionPredicate = nameof(ActualStageExp))]
			public TaskStage? ActualStage { get; set; }

			private static Expression<Func<Task, TaskStage, bool>> ActualStageExp()
				=> (t, ts) => t.Id == ts.TaskId && ts.Actual == true;
		}

		sealed class BdaTask : Task
		{
			public const string Code = "bda.Requests";
		}

		sealed class NonBdaTask : Task
		{
			public const string Code = "None";
		}

		[Table]
		sealed class TaskStage
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; set; }

			[Column]
			public int TaskId { get; set; }

			[Column]
			[Column(Configuration = ProviderName.DB2, DbType = "char")]
			public bool Actual { get; set; }
		}

		[Test]
		public void Test([DataSources] string configuration)
		{
			using (var db = GetDataContext(configuration))
			{
				using (db.CreateLocalTable<Task>())
				using (db.CreateLocalTable<TaskStage>())
				{
					db.Insert(new Task {Id = 1, TargetName = "bda.Requests"});
					db.Insert(new Task {Id = 2, TargetName = "None"});
					db.Insert(new TaskStage {Id = 2, TaskId = 1, Actual = true});

					var query = db.GetTable<Task>()
						.OfType<BdaTask>()
						.Select(p => new
						{
							Instance = p,
							ActualStageId = p.ActualStage!.Id
						});
					var res = query.ToArray();

					Assert.That(res, Has.Length.EqualTo(1));
					Assert.Multiple(() =>
					{
						Assert.That(res[0].Instance, Is.Not.Null);
						Assert.That(res[0].ActualStageId, Is.EqualTo(2));
					});
				}
			}
		}

		[Test]
		public void Test2([DataSources] string configuration)
		{
			using (var db = GetDataContext(configuration))
			{
				using (db.CreateLocalTable<Task>())
				using (db.CreateLocalTable<TaskStage>())
				{
					db.Insert(new Task {Id = 1, TargetName = "bda.Requests"});
					db.Insert(new Task {Id = 2, TargetName = "None"});
					db.Insert(new TaskStage {Id = 2, TaskId = 1, Actual = true});

					var query = db.GetTable<Task>()
						.OfType<BdaTask>()
						.Select(p => new
						{
							Instance = p,
							ActualStageId = (p as Task).ActualStage!.Id
						});
					var res = query.ToArray();

					Assert.That(res, Has.Length.EqualTo(1));
					Assert.Multiple(() =>
					{
						Assert.That(res[0].Instance, Is.Not.Null);
						Assert.That(res[0].ActualStageId, Is.EqualTo(2));
					});

					var query2 = db.GetTable<Task>()
						.Select(p => new
						{
							Instance = p,
							ActualStageId = (p as Task).ActualStage!.Id
						});

					var res2 = query2.OrderBy(_ => _.Instance.Id).ToArray();

					Assert.That(res2, Has.Length.EqualTo(2));
					Assert.Multiple(() =>
					{
						Assert.That(res2[0].Instance, Is.Not.Null);
						Assert.That(res2[0].ActualStageId, Is.EqualTo(2));
					});
				}
			}
		}
	}
}
