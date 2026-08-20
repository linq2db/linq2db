using System;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Infrastructure;
using LinqToDB.Internal.SqlProvider;

using NUnit.Framework;

using Shouldly;

namespace Tests.Infrastructure
{
	/// <summary>
	/// The identifier limits linq2db declares come from vendor documentation, which is not always right
	/// about which kind of name it applies to - MySQL caps an object name at 64 but an alias at 256, and
	/// declaring the smaller one truncated aliases the server would have accepted.
	/// <para>
	/// Asking whether the server *rejects* a long name is not enough: PostgreSQL accepts any length and
	/// silently truncates to 63 bytes, which is the very bug this all started from. So the probe sends an
	/// alias and asks the server what name it actually stored - a round trip catches silent truncation
	/// and outright rejection alike.
	/// </para>
	/// <para>
	/// It measures a <b>column</b> alias, because that is the one the server reports back. A table alias
	/// can be capped lower: SAP HANA stores a 255 character column alias but answers a table alias past
	/// 127 with "identifier is too long". Treat the number this reports as an upper bound, and confirm a
	/// change against the alias tests, which use a table alias.
	/// </para>
	/// </summary>
	[TestFixture]
	public class IdentifierLimitProbeTests : TestBase
	{
		/// <returns>
		/// Length of the name the server reported back, or <c>null</c> when it rejected the statement.
		/// </returns>
		static int? RoundTrip(DataConnection db, int length)
		{
			var builder = db.DataProvider.CreateSqlBuilder(db.MappingSchema, db.Options);
			var alias   = builder.ConvertInline(new string('a', length), ConvertType.NameToQueryFieldAlias);
			var table   = builder.ConvertInline("Person", ConvertType.NameToQueryTable);

			try
			{
				using var reader = db.ExecuteReader($"SELECT 1 AS {alias} FROM {table}");
				reader.Reader!.Read();
				return reader.Reader.GetName(0).Length;
			}
			catch (Exception)
			{
				return null;
			}
		}

		static IIdentifierService IdentifierService(DataConnection db)
			=> (IIdentifierService)((IInfrastructure<IServiceProvider>)db.DataProvider)
				.Instance.GetService(typeof(IIdentifierService))!;

		static int DeclaredLimit(DataConnection db)
			=> IdentifiersHelper.TruncateIdentifier(IdentifierService(db), IdentifierKind.Alias, new string('a', 1024)).Length;

		[Test]
		public void TruncatedAliasSurvivesRoundTrip([DataSources(false)] string context)
		{
			// The safety-critical invariant: whatever linq2db produces after truncating an over-long
			// alias has to come back from the server unchanged. If the server shortens it further, the
			// truncated names can still collide - which is exactly how two aliases became one on
			// PostgreSQL. Asking the service for the alias kind also covers the providers whose alias
			// cap differs from their object-name cap.
			using var db = (DataConnection)GetDataContext(context);

			var service   = IdentifierService(db);
			var truncated = IdentifiersHelper.TruncateIdentifier(service, IdentifierKind.Alias, new string('a', 1024));
			var reported  = RoundTrip(db, truncated.Length);

			reported.ShouldNotBeNull($"{context} rejected the {truncated.Length} character alias linq2db truncates to.");
			reported.ShouldBe(truncated.Length, $"{context} stored linq2db's {truncated.Length} character alias as {reported} characters, so the declared limit is too high.");
		}

		[Test]
		[Explicit("Diagnostic - reports the server's real alias limit so declared limits can be checked against it.")]
		public void DiscoverAliasLimit([DataSources(false)] string context)
		{
			using var db = (DataConnection)GetDataContext(context);

			const int ceiling = 1024;

			if (RoundTrip(db, 8) is null)
			{
				var builder = db.DataProvider.CreateSqlBuilder(db.MappingSchema, db.Options);
				var alias   = builder.ConvertInline(new string('a', 8), ConvertType.NameToQueryFieldAlias);
				var table   = builder.ConvertInline("Person", ConvertType.NameToQueryTable);

				try
				{
					using var reader = db.ExecuteReader($"SELECT 1 AS {alias} FROM {table}");
					reader.Reader!.Read();
				}
				catch (Exception ex)
				{
					TestContext.Out.WriteLine($"{context}: probe unusable - {ex.GetType().Name}: {ex.Message.Split('\n')[0]}");
					return;
				}
			}

			if (RoundTrip(db, ceiling) == ceiling)
			{
				TestContext.Out.WriteLine($"{context}: no limit up to {ceiling} (declared {DeclaredLimit(db)})");
				return;
			}

			// largest length the server stores intact
			int lo = 0, hi = ceiling;

			while (lo < hi)
			{
				var mid = lo + (hi - lo + 1) / 2;

				if (RoundTrip(db, mid) == mid)
					lo = mid;
				else
					hi = mid - 1;
			}

			TestContext.Out.WriteLine($"{context}: real alias limit {lo} (declared {DeclaredLimit(db)})");
		}
	}
}
