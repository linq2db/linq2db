using System.Linq;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3371Tests : TestBase
	{

		class PayRate
		{
			public int Id { get; set; }
			public string? Name { get; set; }
		}

		class Employee
		{
			public int Id { get; set; }
			public PayRate? PayRate { get; private set; }
			public int? PayRateId { get; private set; }
		}



		private PayRate[] CtreatePayRateData() => new PayRate[]
			{
				new PayRate { Id = 1, Name = "Name1" },
				new PayRate { Id = 2, Name = "Name2" },
				new PayRate { Id = 3, Name = "test" }
			};



		[Test]
		public void NullReferenceExceptionTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{

			var builder = MappingSchema.Default.GetFluentMappingBuilder();

			builder.Entity<Employee>()
				 .Association(x => x.PayRate, x => x.PayRateId, x => x.Id);


			builder.Entity<PayRate>();

			var payRateData = CtreatePayRateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<PayRate>("PayRate", payRateData))
			using (var table2 = db.CreateLocalTable<Employee>())
			//using (var table2 = db.CreateLocalTable<Employee>("Employees", new Employee[] { new Employee { Id = 1 , PayRate= null } }))
			{
				var data = table.Select(x => new { x.Id, x.Name }).First();
				Assert.AreEqual("Name1", data.Name);

				table.ToArray().Should().HaveCount(3);
				table2.ToArray().Should().BeEmpty();
				//table2.ToArray().Should().HaveCount(1);

				var queryNavProp = table2
					.Select(x => new
					{
						x.Id,
						PayRate = x.PayRate == null // nav property
						? null
						: new
						{
							x.Id,
							x.PayRate.Name,
						}
					})
#pragma warning disable CS8602 // Dereference of a possibly null reference.
				.Where(item => item.PayRate.Name.Equals("test"));
#pragma warning restore CS8602 // Dereference of a possibly null reference.

				var good = queryNavProp.ToList();

				var queryFK = table2
					.Select(x => new
					{
						x.Id,
						PayRate = x.PayRateId == null // FK property
						? null
						: new
						{
							x.Id,
#pragma warning disable CS8602 // Dereference of a possibly null reference.
							x.PayRate.Name,
#pragma warning restore CS8602 // Dereference of a possibly null reference.
						}
					})
#pragma warning disable CS8602 // Dereference of a possibly null reference.
					.Where(item => item.PayRate.Name.Equals("test"));
#pragma warning restore CS8602 // Dereference of a possibly null reference.

				var bad = queryFK.ToList(); // System.NullReferenceException
			}


		}
	}
}
