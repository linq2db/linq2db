using System;

using NHibernate;

using NUnit.Framework;

using Tests;

namespace LinqToDB.NHibernate.Tests.Logging
{
	/// <summary>
	/// NHibernate logger factory used by the test harness. Mirrors the EF Core test logger: SQL is captured into
	/// <see cref="BaselinesManager"/> and echoed to the test output. Two categories are enabled — the
	/// <c>LinqToDB.NHibernate</c> tracing bridge (linq2db's generated SQL) and NHibernate's own <c>NHibernate.SQL</c>
	/// (its native command text) — so a test that runs a query both ways surfaces both statements for comparison.
	/// Every other (chatty) NHibernate category stays quiet.
	/// </summary>
	internal sealed class TestNHibernateLoggerFactory : INHibernateLoggerFactory
	{
		public INHibernateLogger LoggerFor(string keyName) => new TestNHibernateLogger(keyName);
		public INHibernateLogger LoggerFor(Type type)      => new TestNHibernateLogger(type.FullName ?? type.Name);
	}

	internal sealed class TestNHibernateLogger : INHibernateLogger
	{
		const string BridgeCategory   = "LinqToDB.NHibernate";
		const string NHibernateSqlLog = "NHibernate.SQL";

		readonly bool _enabled;

		public TestNHibernateLogger(string name) => _enabled = name == BridgeCategory || name == NHibernateSqlLog;

		public bool IsEnabled(NHibernateLogLevel logLevel) => _enabled && logLevel != NHibernateLogLevel.None;

		public void Log(NHibernateLogLevel logLevel, NHibernateLogValues state, Exception? exception)
		{
			if (!_enabled)
				return;

			var message = state.ToString();

			// The bridge emits the raw SQL on BeforeExecute plus descriptive timing/error lines on the other
			// steps; NHibernate's NHibernate.SQL category emits its native command text. The SQL from both
			// belongs in the baseline — only the bridge's timing/error lines are filtered out.
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
