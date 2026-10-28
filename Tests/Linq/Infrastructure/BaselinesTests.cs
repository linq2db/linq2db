using System;
using System.IO;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Shouldly;

namespace Tests.Infrastructure
{
	/// <summary>
	/// <see cref="NoBaselinesAttribute"/>, the direct-vs-remote baseline comparison (<see cref="BaselineComparer"/>,
	/// <see cref="BaselinesWriter"/>) and the <see cref="ActiveIssueAttribute.BaselinesMismatch"/> gate built on it.
	/// </summary>
	[TestFixture]
	public class BaselinesTests : TestBase
	{
		[Test, NoBaselines("test: capture is suppressed")]
		public void NoBaselinesSuppressesCapture([DataSources] string context)
		{
			using var db = GetDataContext(context);

			db.Person
				.Where(p => p.ID == 1)
				.ToList();

			GetCurrentBaselines()
				.ShouldBeEmpty();
		}

		[Test, NoBaselines("test: capture is suppressed for one configuration", Configuration = ProviderName.SQLiteMS)]
		public void NoBaselinesConfigurationScoped([DataSources] string context)
		{
			using var db = GetDataContext(context);

			db.Person
				.Where(p => p.ID == 1)
				.ToList();

			if (context.IsAnyOf(ProviderName.SQLiteMS))
				GetCurrentBaselines()
					.ShouldBeEmpty();
			else
				GetCurrentBaselines()
					.ShouldContain("SELECT");
		}

		[Test, NoBaselines("test: a scope whose condition is false keeps capture suppressed")]
		public void NoBaselinesSurvivesFalseScope([IncludeDataSources(true, ProviderName.SQLiteMS)] string context)
		{
			using var db = GetDataContext(context);

			using (new DisableBaseline("test", disable: false))
				db.Person
					.Where(p => p.ID == 1)
					.ToList();

			GetCurrentBaselines()
				.ShouldBeEmpty();
		}

		[Test]
		public void Comparer_SameSql_Matched()
		{
			var comparer = new BaselineComparer();

			comparer.Compare("ctx/Test.sql", isRemote: false, "SELECT 1")
				.ShouldBeNull();
			comparer.Compare("ctx/Test.sql", isRemote: true,  "SELECT 1")
				.ShouldBe(BaselineComparison.Matched);
		}

		[Test]
		public void Comparer_DifferentSql_Mismatched()
		{
			var comparer = new BaselineComparer();

			comparer.Compare("ctx/Test.sql", isRemote: false, "SELECT 1")
				.ShouldBeNull();
			comparer.Compare("ctx/Test.sql", isRemote: true,  "SELECT 2")
				.ShouldBe(BaselineComparison.Mismatched);
		}

		[Test]
		public void Comparer_RemoteFirst_MismatchDetectedOnDirect()
		{
			var comparer = new BaselineComparer();

			comparer.Compare("ctx/Test.sql", isRemote: true,  "SELECT 2")
				.ShouldBeNull();
			comparer.Compare("ctx/Test.sql", isRemote: false, "SELECT 1")
				.ShouldBe(BaselineComparison.Mismatched);
		}

		[Test]
		public void Comparer_OneSideOnly_NotCompared()
		{
			var comparer = new BaselineComparer();

			comparer.Compare("ctx/Test.sql",  isRemote: false, "SELECT 1")
				.ShouldBeNull();
			comparer.Compare("ctx/Other.sql", isRemote: true,  "SELECT 1")
				.ShouldBeNull();
		}

		[Test]
		public void Comparer_SameSideTwice_Throws()
		{
			var comparer = new BaselineComparer();

			comparer.Compare("ctx/Test.sql", isRemote: false, "SELECT 1");

			Action act = () => comparer.Compare("ctx/Test.sql", isRemote: false, "SELECT 1");

			act.ShouldThrow<InvalidOperationException>()
				.Message.ShouldContain("Baseline already in use");
		}

		[Test]
		public void Comparer_KeysDifferingInCase_Collide()
		{
			// one baseline file on a case-insensitive file system
			var comparer = new BaselineComparer();

			comparer.Compare("ctx/Test.sql", isRemote: false, "SELECT 1");

			Action act = () => comparer.Compare("CTX/test.sql", isRemote: false, "SELECT 1");

			act.ShouldThrow<InvalidOperationException>();
		}

		[Test]
		public void Comparer_ProviderSuffix_DoesNotCollide()
		{
			var comparer = new BaselineComparer();

			comparer.Compare("SQLite.MS/Test.sql",      isRemote: false, "SELECT 1")
				.ShouldBeNull();
			comparer.Compare("SQLite.MS.EF10/Test.sql", isRemote: false, "SELECT 2")
				.ShouldBeNull();
		}

		[Test]
		public void Record_UngatedMismatch_WritesOther()
		{
			InScratchDirectory(path =>
			{
				var comparer = new BaselineComparer();

				BaselinesWriter.Record(comparer, path, Path.Combine("ctx", "Test.sql"), isRemote: false, "SELECT 1", gated: false)
					.ShouldBeNull();
				BaselinesWriter.Record(comparer, path, Path.Combine("ctx", "Test.sql"), isRemote: true,  "SELECT 2", gated: false)
					.ShouldBe(BaselineComparison.Mismatched);

				File.ReadAllText(Path.Combine(path, "ctx", "Test.sql"      ))
					.ShouldBe("SELECT 1");
				File.ReadAllText(Path.Combine(path, "ctx", "Test.sql.other"))
					.ShouldBe("SELECT 2");
			});
		}

		[Test]
		public void Record_GatedMismatch_WritesNoOther()
		{
			InScratchDirectory(path =>
			{
				var comparer = new BaselineComparer();

				BaselinesWriter.Record(comparer, path, Path.Combine("ctx", "Test.sql"), isRemote: false, "SELECT 1", gated: true);
				BaselinesWriter.Record(comparer, path, Path.Combine("ctx", "Test.sql"), isRemote: true,  "SELECT 2", gated: true)
					.ShouldBe(BaselineComparison.Mismatched);

				File.Exists(Path.Combine(path, "ctx", "Test.sql"      ))
					.ShouldBeTrue();
				File.Exists(Path.Combine(path, "ctx", "Test.sql.other"))
					.ShouldBeFalse();
			});
		}

		[Test]
		public void Record_GatedRemoteFirst_WritesDirectSide()
		{
			InScratchDirectory(path =>
			{
				var comparer = new BaselineComparer();

				BaselinesWriter.Record(comparer, path, Path.Combine("ctx", "Test.sql"), isRemote: true,  "SELECT 2", gated: true)
					.ShouldBeNull();
				BaselinesWriter.Record(comparer, path, Path.Combine("ctx", "Test.sql"), isRemote: false, "SELECT 1", gated: true)
					.ShouldBe(BaselineComparison.Mismatched);

				File.ReadAllText(Path.Combine(path, "ctx", "Test.sql"))
					.ShouldBe("SELECT 1");
			});
		}

		[Test]
		public void Record_NoPath_WritesNothing()
		{
			var comparer = new BaselineComparer();

			BaselinesWriter.Record(comparer, null, Path.Combine("ctx", "Test.sql"), isRemote: false, "SELECT 1", gated: false)
				.ShouldBeNull();
			BaselinesWriter.Record(comparer, null, Path.Combine("ctx", "Test.sql"), isRemote: true,  "SELECT 2", gated: false)
				.ShouldBe(BaselineComparison.Mismatched);
		}

		// Write itself, on the process-wide comparer: the provider suffix keeps the key apart from real baselines, and no
		// path means no file is written even on CI.
		[Test]
		public void Write_UngatedMismatch_Throws([IncludeDataSources(ProviderName.SQLiteMS)] string context)
		{
			BaselinesWriter.Write(null, "SELECT 1", isRemote: false, ".UngatedProbe");

			Action act = () => BaselinesWriter.Write(null, "SELECT 2", isRemote: true, ".UngatedProbe");

			act.ShouldThrow<InvalidOperationException>()
				.Message.ShouldStartWith("Baselines for remote context doesn't match direct access baselines.");
		}

		// The direct and the remote case deliberately run different queries, so their baselines differ on every run;
		// the gate turns the side that compares Inconclusive and leaves the other one passing.
		[Test]
		[ActiveIssue(Details = "deliberate direct-vs-remote SQL difference", BaselinesMismatch = true)]
		public void MismatchIsGated([IncludeDataSources(true, ProviderName.SQLiteMS)] string context)
		{
			using var db = GetDataContext(context);

			if (context.IsRemote())
				db.Person
					.Count()
					.ShouldBeGreaterThan(0);
			else
				db.Person
					.Select(p => p.ID)
					.ToList()
					.ShouldNotBeEmpty();
		}

		static void InScratchDirectory(Action<string> action)
		{
			var path = Path.Combine(Path.GetTempPath(), "linq2db-baselines-" + Guid.NewGuid().ToString("N"));

			Directory.CreateDirectory(path);

			try
			{
				action(path);
			}
			finally
			{
				Directory.Delete(path, true);
			}
		}
	}
}
