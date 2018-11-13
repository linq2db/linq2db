using System;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;

namespace Tests.UserTests
{
	using LinqToDB;
	using LinqToDB.Mapping;

	[TestFixture]
	public class Issue1225Tests : TestBase
	{
		[Table]
		class Task
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; set; }

			[Association(ExpressionPredicate = nameof(ActualStageExp))]
			public TaskStage ActualStage { get; set; }

			private static Expression<Func<Task, TaskStage, bool>> ActualStageExp()
				=> (t, ts) => t.Id == ts.TaskId && ts.Actual == true;
		}

		[Table]
		class TaskStage
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; set; }

			[Column]
			public int TaskId { get; set; }

			[Column]
			[Column(Configuration = ProviderName.DB2, DbType = "char")]
			[Column(Configuration = ProviderName.Firebird, DbType = "char(1)")]
			public bool Actual { get; set; }
		}

		private class GroupByWrapper
		{
			public GroupByWrapper()
			{
			}

			public LastInChain GroupByContainer { get; set; }
		}

		private class LastInChain
		{
			public LastInChain()
			{
			}

			public string Name { get; set; }
			public object Value { get; set; }
		}

		private class AggregationWrapper
		{
			public AggregationWrapper()
			{
			}

			public LastInChain GroupByContainer { get; set; }
			public LastInChain Container { get; set; }
		}

		[ActiveIssue("Unsupported by Informix?", Configuration = ProviderName.Informix)]
		[Test]
		public void Test([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<Task>())
				using (db.CreateLocalTable<TaskStage>())
				{
					db.Insert(new Task { Id = 1 });
					db.Insert(new Task { Id = 2 });
					db.Insert(new TaskStage { Id = 2, TaskId = 1, Actual = true });

					var query = db.GetTable<Task>()
							.GroupBy(it => new GroupByWrapper()
							{
								GroupByContainer = new LastInChain()
								{
									Name = "Id",
									Value = it.Id
								}
							})
							.Select(it => new AggregationWrapper()
							{
								GroupByContainer = it.Key.GroupByContainer,
								Container = new LastInChain()
								{
									Name = "TotalId",
									Value = it
										.AsQueryable()
										.Sum(_ => (_.ActualStage == null)? null: (int?)_.ActualStage.Id)
								}
							});
					var res = query.ToArray();

					Assert.AreEqual(2, res.Length);
					Assert.IsNotNull(res[0].Container);
					Assert.AreEqual(2, res[0].Container.Value);
				}
			}
		}
	}
}
