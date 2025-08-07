using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	// disabled providers cannot handle concurrent queries well
	[TestFixture]
	public class Issue1398Tests : TestBase
	{
		[Table("Animals")]
		internal sealed class Animal
		{
			[PrimaryKey, Column(CanBeNull = false, DataType = DataType.NVarChar, Length = 100)]
			public string Name { get; set; } = null!;

			[Column(CanBeNull = false, DataType = DataType.NVarChar, Length = 100)]
			public string Color { get; set; } = null!;

			[Column(CanBeNull = false)]
			public int Length { get; set; }
		}

		[Table("AnimalsUpdate")]
		internal sealed class AnimalUpdate
		{
			[Column(CanBeNull = false, DataType = DataType.NVarChar, Length = 100)]
			public string Name { get; set; } = null!;

			[Column(CanBeNull = false)]
			public int Length { get; set; }

			[Column(CanBeNull = false)]
			public int Iteration { get; set; }
		}

		internal sealed class Data
		{
			public string Color { get; set; } = null!;

			public List<AnimalUpdate> Updates { get; set; } = null!;
		}

		[Table("InsertTable1398")]
		internal sealed class InsertTable
		{
			[Column(CanBeNull = false), PrimaryKey]
			public int Value { get; set; }
		}

		// TODO: disabled providers lacks connections
		[Test]
		public void TestInsert([DataSources(false, TestProvName.AllFirebird, TestProvName.AllSybase, TestProvName.AllInformix, TestProvName.AllOracle12, TestProvName.AllSQLiteClassic)] string context)
		{
			const int recordsCount = 20;

			// sqlite connection pooling is not compatible with tested template
			SQLiteTools.ClearAllPools(provider: null);

			using (new DisableBaseline("Multi-threading"))
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable<InsertTable>())
			{
				var tasks = new List<Task>();

				for (var iteration = 0; iteration < recordsCount; iteration++)
				{
					var local = iteration;
					tasks.Add(Task.Run(() => Insert(context, local)));
				}

				Task.WaitAll(tasks.ToArray());

				Assert.That(db.GetTable<InsertTable>().GroupBy(_ => _.Value).Count(), Is.EqualTo(db.GetTable<InsertTable>().Count()));
			}
		}

		private void Insert(string context, int value)
		{
			using (var db = GetDataConnection(context))
			{
				db.GetTable<InsertTable>().Insert(() => new InsertTable { Value = value });
			}
		}

		[Retry(3)] // could fail due to deadlock
		[Test]
		public void TestMerge([MergeDataContextSource(
			TestProvName.AllFirebird, ProviderName.SybaseManaged, TestProvName.AllInformix)]
			string context)
		{
			const int repeatsCount = 20;
			var rnd = new Random();

			var mammals  = new[] { "Elephant", "Cat" };
			var reptiles = new[] { "Snake", "Lizard" };

			using (new DisableBaseline("Multi-threading"))
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Animal>())
			using (db.CreateLocalTable<AnimalUpdate>())
			{
				for (var iteration = 0; iteration < repeatsCount; iteration++)
				{
					var mammalsUpdate = new Data()
					{
						Color   = "Grey",
						Updates = mammals
							.Select(name => new AnimalUpdate()
							{
								Name      = name,
								Length    = rnd.Next(),
								Iteration = iteration
							})
							.ToList()
					};

					var reptilesUpdate = new Data()
					{
						Color   = "Green",
						Updates = reptiles
							.Select(name => new AnimalUpdate()
							{
								Name      = name,
								Length    = rnd.Next(),
								Iteration = iteration + repeatsCount
							})
							.ToList()
					};

					foreach (var record in mammalsUpdate.Updates)
						db.Insert(record);
					foreach (var record in reptilesUpdate.Updates)
						db.Insert(record);

					var updateMammalsTask  = Task.Run(() => Update(context, mammalsUpdate, iteration));

					var updateReptilesTask = Task.Run(() => Update(context, reptilesUpdate, iteration + repeatsCount));

					Task.WaitAll(updateMammalsTask, updateReptilesTask);

					var greenAnimalsCount = db.GetTable<Animal>().Count(animal => animal.Color == "Green");

					if (greenAnimalsCount != reptiles.Length)
					{
						var error = new StringBuilder($"Error on iteration {iteration + 1}:");

						foreach (var animal in db.GetTable<Animal>())
						{
							error.AppendLine($"{animal.Name} is {animal.Color}");
						}

						Assert.Fail(error.ToString());
					}
				}
			}
		}

		private void Update(string context, Data data, int iteration)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<Animal>()
					.Merge()
					.Using(db.GetTable<AnimalUpdate>().Where(_ => _.Iteration == iteration))
					.On((target, source) => target.Name == source.Name)
					.UpdateWhenMatched(
						(target, source) => new Animal()
						{
							Color  = data.Color,
							Length = source.Length
						})
					.InsertWhenNotMatched(
						source => new Animal()
						{
							Name   = source.Name,
							Color  = data.Color,
							Length = source.Length
						})
					.Merge();
			}
		}
	}
}
