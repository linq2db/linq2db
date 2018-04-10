using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
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
			public string TargetName { get; set; }
		}

		[Table("Task")]
		class TaskTable
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; set; }

			[Column(IsDiscriminator = true)]
			public string TargetName { get; set; }

			[Column]
			public string BdaValue { get; set; }
		}

		[Table("Task")]
		class BdaTask: Task
		{
			public const string Code = "bda.Requests";

			[Column]
			public string BdaValue { get; set; }
		}

		class SelectAllAndExpand<T>
		{
			public T Instance { get; set; } 
		}

		[Test, DataContextSource]
		public void TestInheritance(string context)
		{
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

					Assert.AreEqual(1, items.Length);
					Assert.AreEqual("Bda value", items[0].Instance.BdaValue);

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

					Assert.AreEqual(1, items2.Length);
					Assert.AreEqual("Bda value", items2[0].Instance.BdaValue);
				}
			}
		}
	}
}
