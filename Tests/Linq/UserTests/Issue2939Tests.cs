using System;
using System.Linq;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2939Tests : TestBase
	{
		interface IService
		{
			int  Id       { get; set; }
			int? IdClient { get; set; }
		}

		[Table]
		public partial class Adsl : IService
		{
			[PrimaryKey] public int  Id       { get; set; } // int
			[Column    ] public int? IdClient { get; set; } // int
		}

		[Table]
		public partial class Client
		{
			[PrimaryKey] public int Id { get; set; } // int
		}

		[Test]
		public void Test([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var services = db.CreateLocalTable<Adsl>())
			using (var clients = db.CreateLocalTable<Client>())
			{
				IQueryable<Client>   q_clients  = clients;
				IQueryable<IService> q_services = services;
				IQueryable<Adsl>     q_adsl     = services;

				q_services = from adsl in q_adsl select adsl;

				var q_test = (
					from serv in q_services
					join client in q_clients on serv.IdClient equals client.Id
					select serv.Id
				);

				var res = q_test.ToList();
			}
		}
	}
}
