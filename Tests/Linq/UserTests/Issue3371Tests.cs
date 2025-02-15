using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3371Tests : TestBase
	{
		private sealed class PayRate
		{
			public PayRate()
			{
				Name = string.Empty;
			}

			public int    Id   { get; set; }
			public string Name { get; set; }
		}

		private sealed class Employee
		{
			public int      Id        { get; set; }
			public PayRate? PayRate   { get; set; }
			public int?     PayRateId { get; set; }
		}

		[Test]
		public void NullReferenceExceptionTest([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var ms      = new MappingSchema();
			var builder = new FluentMappingBuilder(ms);

			builder.Entity<Employee>()
				.Association(x => x.PayRate, x => x.PayRateId, x => x!.Id);

			builder.Entity<PayRate>();

			builder.Build();

			var payRateData = new PayRate[]
			{
				new() { Id = 1, Name = "Name1" }, new() { Id = 2, Name = "Name2" }, new() { Id = 3, Name = "test" }
			};

			var employeeData = new Employee[]
			{
				new() { Id = 1, PayRateId = 1 }, new() { Id = 2, PayRateId = null },
				new() { Id = 3, PayRateId = 3 },
			};

			using (var db = GetDataContext(context, o => o.UseMappingSchema(ms)))
			using (var payRates = db.CreateLocalTable("PayRate", payRateData))
			using (var employees = db.CreateLocalTable("Employees", employeeData))
			{
				var queryNavProp = employees
					.Select(x => new
					{
						x.Id,
						PayRate = x.PayRate == null // nav property
							? null
							: new { x.Id, x.PayRate.Name }
					})
					.Where(item => item.PayRate!.Name.Equals("test"));

				var good = queryNavProp.ToList();

				var queryFK = employees
					.Select(x => new
					{
						x.Id,
						PayRate = x.PayRateId == null // FK property
							? null
							: new
							{
								x.Id,
								x.PayRate!.Name,
							}
					})
					.Where(item => item.PayRate!.Name.Equals("test"));

				var bad = queryFK.ToList(); // System.NullReferenceException

				AreEqual(good, bad);
			}
		}
	}
}
