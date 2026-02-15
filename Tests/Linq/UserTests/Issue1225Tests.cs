using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1225Tests : TestBase
	{
		[Table]
		sealed class Task
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; set; }

			[Association(ExpressionPredicate = nameof(ActualStageExp))]
			public TaskStage? ActualStage { get; set; }

			private static Expression<Func<Task, TaskStage, bool>> ActualStageExp()
				=> (t, ts) => t.Id == ts.TaskId && ts.Actual == true;
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

		private sealed class GroupByWrapper
		{
			public GroupByWrapper()
			{
			}

			public LastInChain GroupByContainer { get; set; } = null!;
		}

		private sealed class LastInChain
		{
			public LastInChain()
			{
			}

			public string  Name  { get; set; } = null!;
			public object? Value { get; set; }
		}

		private sealed class AggregationWrapper
		{
			public AggregationWrapper()
			{
			}

			public LastInChain GroupByContainer { get; set; } = null!;
			public LastInChain Container        { get; set; } = null!;
		}

		[Test]
		public void Test([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t = db.CreateLocalTable<Task>();
			using var ts = db.CreateLocalTable<TaskStage>();
			db.Insert(new Task { Id = 1 }, t.TableName);
			db.Insert(new Task { Id = 2 }, t.TableName);
			db.Insert(new TaskStage { Id = 2, TaskId = 1, Actual = true }, ts.TableName);

			var query = t
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
			var res = query.AsEnumerable().OrderBy(_ => _.GroupByContainer.Value).ToArray();

			Assert.That(res, Has.Length.EqualTo(2));
			Assert.That(res[0].Container, Is.Not.Null);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res[0].Container.Value, Is.EqualTo(2));
				Assert.That(res[1].Container, Is.Not.Null);
			}

			Assert.That(res[1].Container.Value, Is.Null);
		}
	}
}
