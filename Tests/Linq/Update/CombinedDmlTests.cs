using System;
using System.Data.Common;
using System.Linq;

using LinqToDB;
using LinqToDB.Interceptors;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.xUpdate
{
	/// <summary>
	/// Combined-command coverage for the DML half of the opt-in engine.
	/// </summary>
	/// <remarks>
	/// The engine has two consumers. Eager loading is well covered — EagerLoadingTests runs its whole surface twice,
	/// with combining off and on. The DML half was exercised on SQLite only (InterceptorsTests), and planning is the
	/// part that is provider-agnostic: what is provider-specific is the EXECUTION of a merged group — harvesting the
	/// identity value and rows-affected out of a multi-result-set reader instead of a separate ExecuteScalar /
	/// ExecuteNonQuery, and sharing parameters across the concatenated steps. Ten IDmlService.BuildCommandScenario
	/// overrides feed that path, each with its own identity mechanism (SCOPE_IDENTITY, RETURNING INTO,
	/// SELECT ... FROM FINAL TABLE, RETURNING, ...), so a green run with the switch off says nothing about any of them.
	/// <para>
	/// Each test therefore runs BOTH configurations itself and counts round-trips, rather than taking the switch as a
	/// test parameter and asserting the same outcome in both arms — that shape passes just as happily when the engine
	/// stops engaging at all. The off arm is the calibration: it discovers whether this provider's scenario is
	/// multi-step in the first place, so the expectation is derived from the real gate instead of from a provider list.
	/// </para>
	/// <para>
	/// Remote contexts are excluded on purpose: combining is a DataConnection-only path, so a remote leg would re-test
	/// the sequential shape while producing a differently-grouped baseline than the direct one.
	/// </para>
	/// </remarks>
	[TestFixture]
	public class CombinedDmlTests : TestBase
	{
		sealed class CommandCounter : CommandInterceptor
		{
			public int Count { get; set; }

			public override DbCommand CommandInitialized(CommandEventData eventData, DbCommand command)
			{
				Count++;
				return command;
			}
		}

		/// <summary>
		/// The shared expectation. <paramref name="on"/> may never exceed <paramref name="off"/> — combining merges
		/// round-trips, it cannot add them. The exact-one expectation applies only where there was something to merge:
		/// a provider whose identity insert already renders as a single command (PostgreSQL's RETURNING) reads 1 in
		/// both arms, and a provider that does not report the combining capability at all keeps its sequential shape
		/// however the option is set.
		/// </summary>
		static void AssertCollapsed(int off, int on, bool combining)
		{
			on.ShouldBeLessThanOrEqualTo(off);

			if (combining && off > 1)
				on.ShouldBe(1);
		}

		// Identity insert: two steps (INSERT + identity retrieval) that the combined engine merges into one command.
		[Test]
		public void InsertWithIdentity_Combined([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			var off = InsertWithIdentityRoundTrips(context, combinedCommands: false, out _);
			var on  = InsertWithIdentityRoundTrips(context, combinedCommands: true,  out var combining);

			AssertCollapsed(off, on, combining);
		}

		// InsertOrReplace: the other multi-step shape — an UPDATE/INSERT emulation whose steps the engine merges on
		// providers without native upsert support.
		[Test]
		public void InsertOrReplace_Combined([InsertOrUpdateDataSources(false)] string context)
		{
			var off = InsertOrReplaceRoundTrips(context, combinedCommands: false, out _);
			var on  = InsertOrReplaceRoundTrips(context, combinedCommands: true,  out var combining);

			AssertCollapsed(off, on, combining);
		}

		// Registering the counter forces the concatenated backend: CanUseDbBatch requires no command interceptor, and a
		// DbBatch would not report through CommandInitialized anyway. So these counts measure the concat path — which is
		// the one every provider shares.
		int InsertWithIdentityRoundTrips(string context, bool combinedCommands, out bool combining)
		{
			using var db      = GetDataContext(context, o => o.UseCombinedCommands(combinedCommands));
			using var cleanup = new DeletePerson(db);

			combining = db.UsesCombinedCommands();

			var counter = new CommandCounter();
			db.AddInterceptor(counter);

			counter.Count = 0;

			var id = db.Person.InsertWithIdentity(() => new Person
			{
				FirstName = "John",
				LastName  = "Shepard",
				Gender    = Gender.Male
			});

			var commands = counter.Count;

			id.ShouldNotBeNull();

			var john = db.Person.Single(p => p.FirstName == "John" && p.LastName == "Shepard");

			john.ID.ShouldBe(Convert.ToInt32(id));

			return commands;
		}

		int InsertOrReplaceRoundTrips(string context, bool combinedCommands, out bool combining)
		{
			ResetPersonIdentity(context);

			using var db      = GetDataContext(context, o => o.UseCombinedCommands(combinedCommands));
			using var cleanup = new RestoreBaseTables(db);

			combining = db.UsesCombinedCommands();

			var counter = new CommandCounter();
			db.AddInterceptor(counter);

			var id = db.InsertWithInt32Identity(new Person
			{
				FirstName = "John",
				LastName  = "Shepard",
				Gender    = Gender.Male
			});

			// Three passes: the first inserts, the next two replace — so a merged group has to leave the row in the
			// state the LAST pass wrote, not the first. Only that last pass is counted: it is the representative
			// multi-step shape (a replace over an existing row), and counting all three would make the expected number
			// depend on how many passes the test happens to make.
			var commands = 0;

			for (var i = 0; i < 3; i++)
			{
				if (i == 2)
					counter.Count = 0;

				db.InsertOrReplace(new Patient
				{
					PersonID  = id,
					Diagnosis = "abc" + i,
				});

				if (i == 2)
					commands = counter.Count;
			}

			db.Patient.Single(p => p.PersonID == id).Diagnosis.ShouldBe("abc2");

			return commands;
		}
	}
}
