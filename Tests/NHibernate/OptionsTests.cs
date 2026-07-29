using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

using LinqToDB;
using LinqToDB.Interceptors;
using LinqToDB.NHibernate.Tests.Models.Northwind;

using NUnit.Framework;

using Shouldly;

namespace LinqToDB.NHibernate.Tests
{
	/// <summary>
	/// Exercises <see cref="LinqToDBForNHibernateTools.AddOptions"/>: configuration registered against a session
	/// factory has to reach the <see cref="DataOptions"/> of the linq2db contexts created for its sessions.
	/// </summary>
	[TestFixture]
	public class OptionsTests : NHTestBase
	{
		sealed class CountingCommandInterceptor : CommandInterceptor
		{
			public int Count { get; private set; }

			public override DbCommand CommandInitialized(CommandEventData eventData, DbCommand command)
			{
				Count++;
				return base.CommandInitialized(eventData, command);
			}
		}

		[Test]
		public void AddOptions_InterceptorReachesLinqToDbContext(
			[NHIncludeDataSources] string provider)
		{
			var sf          = GetSessionFactory(provider);
			var interceptor = new CountingCommandInterceptor();

			LinqToDBForNHibernateTools.AddOptions(sf, o => o.UseInterceptor(interceptor));

			using var session = sf.OpenSession();

			_ = session.GetTable<Customer>().Select(c => c.CustomerId).ToList();

			interceptor.Count.ShouldBeGreaterThan(0);
		}

		[Test]
		public void AddOptions_RegistrationsComposeInOrder(
			[NHIncludeDataSources] string provider)
		{
			var sf      = GetSessionFactory(provider);
			var applied = new List<string>();

			// Each registration has to add to the previous one rather than replace it.
			LinqToDBForNHibernateTools.AddOptions(sf, o => { applied.Add("first");  return o; });
			LinqToDBForNHibernateTools.AddOptions(sf, o => { applied.Add("second"); return o; });

			using var session = sf.OpenSession();

			_ = session.GetTable<Customer>().Select(c => c.CustomerId).ToList();

			// A query may build more than one context; the first pass through shows both ran, in registration order.
			applied.Count.ShouldBeGreaterThanOrEqualTo(2);
			applied[0].ShouldBe("first");
			applied[1].ShouldBe("second");
		}
	}
}
