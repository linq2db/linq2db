using System.Linq;
using System.Text;

using LinqToDB;
using LinqToDB.NHibernate.Tests.Models.Northwind;

using NUnit.Framework;

using Shouldly;

using Tests;

namespace LinqToDB.NHibernate.Tests
{
	/// <summary>
	/// Verifies the SQL tracing/baseline pipeline: a linq2db query over the session flows through the
	/// <c>LinqToDB.NHibernate</c> logging bridge into the test logger, which records the generated SQL for the
	/// baseline (the same capture <c>BaselinesManager.Dump</c> writes out per test).
	/// </summary>
	[TestFixture]
	public class SqlTracingTests : NHTestBase
	{
		[Test]
		public void TracingBridge_CapturesGeneratedSql(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);

			using var session = sf.OpenSession();

			_ = session.GetTable<Customer>().Where(c => c.CustomerId == "ALFKI").ToList();

			var captured = CustomTestContext.Get().Get<StringBuilder>(CustomTestContext.BASELINE);
			captured.ShouldNotBeNull();
			captured!.ToString().ShouldContain("SELECT");
		}
	}
}
