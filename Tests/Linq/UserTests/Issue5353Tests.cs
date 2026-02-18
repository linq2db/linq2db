using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.UserTests;

using static Tests.UserTests.Issue5353Tests;

namespace Tests.UserTests
{
	public class Issue5353Tests : TestBase
	{
		public interface IProfile
		{
			LicenseProfile Profile { get; set; }
			int ProfileId { get; set; }
		}

		[Table("Issue5353LicenseProfiles")]
		public class LicenseProfile
		{
			[PrimaryKey, Identity] public int Id { get; set; }
			[Column] public required string License { get; set; }
		}

		public abstract class CustomerBase : IProfile
		{
			[PrimaryKey, Identity] public int Id { get; set; }
			[Column] public int ProfileId { get; set; }
			public LicenseProfile Profile { get; set; } = null!;
		}

		[Table("Issue5353Customers")]
		public sealed class Customer : CustomerBase
		{
			[Column] public int Age { get; set; }
		}

		[Test]
		public void GenericWithInterfaceConstraint([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db            = GetDataContext(context);
			using var profileTable  = db.CreateLocalTable<LicenseProfile>([new() { License = "12345" }]);
			using var customerTable = db.CreateLocalTable<Customer>([new() { ProfileId = profileTable.First().Id }]);

			string[] licenseFilter = ["12345"];
			var query = customerTable.FilterLicense(licenseFilter);

			AssertQuery(query);
		}
	}
}

file static class LicenseFilterExtensions
{
	public static IQueryable<T> FilterLicense<T>(this IQueryable<T> source, IEnumerable<string>? licenseFilter) where T : class, IProfile
	{
		if (licenseFilter != null)
		{
			return source.Where(x => licenseFilter.Contains(x.Profile.License));
		}

		return source;
	}
}
