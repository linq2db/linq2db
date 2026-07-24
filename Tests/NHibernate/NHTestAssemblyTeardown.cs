using NHibernate;

using NUnit.Framework;

namespace LinqToDB.NHibernate.Tests
{
	/// <summary>
	/// Assembly-level setup/teardown. Registers the NHibernate test logger once before any fixture runs — so the
	/// <c>LinqToDB.NHibernate</c> tracing bridge feeds the generated SQL into <c>BaselinesManager</c> and the test
	/// output — and disposes the shared NHibernate session factories once after every fixture has run. Doing the
	/// latter here rather than in each fixture's one-time teardown keeps the shared static factory cache safe if
	/// fixture-level parallelism is ever enabled (no fixture disposes a factory another is still using).
	/// </summary>
	[SetUpFixture]
	public class NHTestAssemblyTeardown
	{
		[OneTimeSetUp]
		public void RegisterLogger() => NHibernateLogger.SetLoggersFactory(new Logging.TestNHibernateLoggerFactory());

		[OneTimeTearDown]
		public void DisposeSessionFactories() => NHTestBase.DisposeFactories();
	}
}
