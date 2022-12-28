using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Common.Internal;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.Infrastructure
{
	using Model;

	[TestFixture]
	public class DataOptionsTests : TestBase
	{
		[Test]
		public void LinqOptionsTest()
		{
			var lo1 = LinqToDB.Common.Configuration.Linq.Options with { GuardGrouping = false };
			var lo2 = lo1 with { GuardGrouping = true };

			Assert.That(((IConfigurationID)lo1).ConfigurationID, Is.Not.EqualTo(((IConfigurationID)lo2).ConfigurationID));
		}

		[Test]
		public void OnTraceTest()
		{
			string? s1 = null;

			{
				using var db = new TestDataConnection(options => options.WithOptions<QueryTraceOptions>(o => o with
				{
					OnTrace = ti => s1 = ti.SqlText
				}));

				_child = db.Child.ToList();

				Assert.NotNull(s1);
			}

			{
				s1 = null;

				using var db = new TestDataConnection();

				_child = db.Child.ToList();

				Assert.IsNull(s1);
			}
		}

		[Test]
		public void OnTrace2Test()
		{
			string? s1 = null;

			using var db = new TestDataConnection();

			_child = db.Child.ToList();

			Assert.IsNull(s1);

			using var db1 = new TestDataConnection(db.Options
				.UseConnection   (db.DataProvider, db.Connection, false)
				.UseMappingSchema(db.MappingSchema)
				.WithOptions<QueryTraceOptions>(o => o with
				{
					OnTrace = ti => s1 = ti.SqlText
				}));


			_child = db1.Child.ToList();

			Assert.NotNull(s1);
		}
	}
}
