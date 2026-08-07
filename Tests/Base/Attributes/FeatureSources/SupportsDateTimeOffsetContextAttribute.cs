using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public sealed class SupportsDateTimeOffsetContextAttribute : DataSourcesAttribute
	{
		// Providers with no column type that carries an offset. A DateTimeOffset property does not map there at
		// all, so CreateLocalTable fails with a native database error - "DATETIMEOFFSET is an undefined name",
		// "Can't find type 'DateTimeOffset'", an ODBC syntax error - long before anything is asked about the value.
		// Excluded rather than declared as expected failures: the refusal says something about the type, not about
		// the feature under test, so asserting it here would pin the wrong contract.
		//
		// Firebird is here for two reasons at once: 2.5 has no such type, and while 4+ does, the client rejects the
		// offset on write with "Incorrect time zone value".
		//
		// Providers that DO carry an offset stay in scope even when they answer wrongly - that is a defect to
		// record, not a type gap to skip.
		internal static readonly List<string> Unsupported = new List<string>
		{
			TestProvName.AllSqlCe,
			TestProvName.AllAccess,
			TestProvName.AllSapHana,
			TestProvName.AllDB2,
			TestProvName.AllSybase,
			TestProvName.AllFirebird,
		}.SelectMany(_ => _.Split(',')).ToList();

		public SupportsDateTimeOffsetContextAttribute(params string[] excludedProviders)
			: base(true, Unsupported.Concat(excludedProviders.SelectMany(_ => _.Split(','))).ToArray())
		{
		}

		public SupportsDateTimeOffsetContextAttribute(bool includeLinqService, params string[] excludedProviders)
			: base(includeLinqService, Unsupported.Concat(excludedProviders.SelectMany(_ => _.Split(','))).ToArray())
		{
		}
	}
}
