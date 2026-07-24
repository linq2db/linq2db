using System;

using NHibernate;

using NUnit.Framework;

using Tests;

namespace LinqToDB.NHibernate.Tests.Logging
{
	/// <summary>
	/// NHibernate logger factory used by the test harness. Mirrors the EF Core test logger: the linq2db SQL surfaced
	/// through the <c>LinqToDB.NHibernate</c> tracing bridge is captured into <see cref="BaselinesManager"/> and echoed
	/// to the test output. Only the bridge category is enabled, so NHibernate's own (chatty) logging stays quiet.
	/// </summary>
	internal sealed class TestNHibernateLoggerFactory : INHibernateLoggerFactory
	{
		public INHibernateLogger LoggerFor(string keyName) => new TestNHibernateLogger(keyName);
		public INHibernateLogger LoggerFor(Type type)      => new TestNHibernateLogger(type.FullName ?? type.Name);
	}

	internal sealed class TestNHibernateLogger : INHibernateLogger
	{
		const string BridgeCategory = "LinqToDB.NHibernate";

		readonly bool _enabled;

		public TestNHibernateLogger(string name) => _enabled = name == BridgeCategory;

		public bool IsEnabled(NHibernateLogLevel logLevel) => _enabled && logLevel != NHibernateLogLevel.None;

		public void Log(NHibernateLogLevel logLevel, NHibernateLogValues state, Exception? exception)
		{
			if (!_enabled)
				return;

			var message = state.ToString();

			// The bridge emits the raw SQL on BeforeExecute and descriptive timing/error lines on the other steps;
			// only the SQL belongs in the baseline (mirrors the EF Core test logger, which baselines the command text).
			if (message.Length > 0
				&& !message.StartsWith("Execution time:",       StringComparison.Ordinal)
				&& !message.StartsWith("Total execution time:", StringComparison.Ordinal)
				&& !message.StartsWith("Failed executing",      StringComparison.Ordinal))
			{
				BaselinesManager.LogQuery(message + Environment.NewLine);
			}

			TestContext.Out.WriteLine(message);
			if (exception != null)
				TestContext.Out.WriteLine(exception.ToString());
		}
	}
}
