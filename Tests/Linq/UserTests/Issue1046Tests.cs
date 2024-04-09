using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	public class Issue1046Tests: TestBase
	{
		[Table, InheritanceMapping(Code = BdaTask.Code, Type = typeof(BdaTask))]
		class Task
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; set; }

			[Column(IsDiscriminator = true)]
			public string? TargetName { get; set; }
		}

		[Table("Task")]
		sealed class TaskTable
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; set; }

			[Column(IsDiscriminator = true)]
			public string? TargetName { get; set; }

			[Column]
			public string? BdaValue { get; set; }
		}

		[Table("Task")]
		sealed class BdaTask : Task
		{
			public const string Code = "bda.Requests";

			[Column]
			public string? BdaValue { get; set; }
		}

		sealed class SelectAllAndExpand<T>
		{
			public T Instance { get; set; } = default!;
		}

		[Test]
		public void TestInheritance([DataSources] string context)
		{
			using (new DisableBaseline("TODO: debug reason for inconsistent column order"))
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<TaskTable>())
				{
					db.Insert(new BdaTask
					{
						BdaValue = "Bda value",
						TargetName = BdaTask.Code,
						Id = 1
					});

					var items = db.GetTable<Task>()
						.OfType<BdaTask>()
						.Select(p => new SelectAllAndExpand<BdaTask>
						{
							Instance = p
						})
						.ToArray();

					Assert.That(items, Has.Length.EqualTo(1));
					Assert.That(items[0].Instance.BdaValue, Is.EqualTo("Bda value"));

					var items2 = db.GetTable<Task>()
						.OfType<BdaTask>()
						.Select(p => new SelectAllAndExpand<BdaTask>
						{
							Instance = new BdaTask
							{
								BdaValue = p.BdaValue
							}
						})
						.ToArray();

					Assert.That(items2, Has.Length.EqualTo(1));
					Assert.That(items2[0].Instance.BdaValue, Is.EqualTo("Bda value"));
				}
			}
		}
	}
}
