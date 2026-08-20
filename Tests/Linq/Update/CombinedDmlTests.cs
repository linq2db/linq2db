using System;
using System.Linq;

using LinqToDB;

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
	/// Remote contexts are excluded on purpose: combining is a DataConnection-only path, so a remote leg would re-test
	/// the sequential shape while producing a differently-grouped baseline than the direct one.
	/// </para>
	/// </remarks>
	[TestFixture]
	public class CombinedDmlTests : TestBase
	{
		// Identity insert: two steps (INSERT + identity retrieval) that the combined engine merges into one command.
		[Test]
		public void InsertWithIdentity_Combined(
			[DataSources(false, TestProvName.AllClickHouse)] string context,
			[Values] bool combinedCommands)
		{
			using var db      = GetDataContext(context, o => o.UseCombinedCommands(combinedCommands));
			using var cleanup = new DeletePerson(db);

			var id = db.Person.InsertWithIdentity(() => new Person
			{
				FirstName = "John",
				LastName  = "Shepard",
				Gender    = Gender.Male
			});

			id.ShouldNotBeNull();

			var john = db.Person.Single(p => p.FirstName == "John" && p.LastName == "Shepard");

			john.ID.ShouldBe(Convert.ToInt32(id));
		}

		// InsertOrReplace: the other multi-step shape — an UPDATE/INSERT emulation whose steps the engine merges on
		// providers without native upsert support.
		[Test]
		public void InsertOrReplace_Combined(
			[InsertOrUpdateDataSources(false)] string context,
			[Values] bool combinedCommands)
		{
			ResetPersonIdentity(context);

			using var db      = GetDataContext(context, o => o.UseCombinedCommands(combinedCommands));
			using var cleanup = new RestoreBaseTables(db);

			var id = db.InsertWithInt32Identity(new Person
			{
				FirstName = "John",
				LastName  = "Shepard",
				Gender    = Gender.Male
			});

			// Three passes: the first inserts, the next two replace — so a merged group has to leave the row in the
			// state the LAST pass wrote, not the first.
			for (var i = 0; i < 3; i++)
			{
				db.InsertOrReplace(new Patient
				{
					PersonID  = id,
					Diagnosis = "abc" + i,
				});
			}

			db.Patient.Single(p => p.PersonID == id).Diagnosis.ShouldBe("abc2");
		}
	}
}
