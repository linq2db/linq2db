using NUnit.Framework;

namespace LinqToDB.NHibernate.Tests
{
	/// <summary>
	/// Disposes the shared NHibernate session factories once, after every fixture in the assembly has run.
	/// Doing this here rather than in each fixture's one-time teardown keeps the shared static factory cache
	/// safe if fixture-level parallelism is ever enabled (no fixture disposes a factory another is still using).
	/// </summary>
	[SetUpFixture]
	public class NHTestAssemblyTeardown
	{
		[OneTimeTearDown]
		public void DisposeSessionFactories() => NHTestBase.DisposeFactories();
	}
}
