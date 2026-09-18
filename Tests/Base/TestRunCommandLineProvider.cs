using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace Tests
{
	/// <summary>
	/// Declares linq2db's test-run command-line options to Microsoft.Testing.Platform so the NUnit MTP runner
	/// accepts them and lists them under <c>--help</c>. The option <em>values</em> are consumed via
	/// <see cref="TestCommandLine"/> (which reads them early, during discovery). Registered through the
	/// <c>TestingPlatformBuilderHook</c> MSBuild item declared in <c>linq2db.BasicTestProjects.props</c>.
	/// </summary>
	public sealed class TestRunCommandLineProvider : ICommandLineOptionsProvider
	{
		/// <inheritdoc/>
		public string Uid => "linq2db.TestRunOptions";

		/// <inheritdoc/>
		public string Version => "1.0.0";

		/// <inheritdoc/>
		public string DisplayName => "linq2db test-run options";

		/// <inheritdoc/>
		public string Description => "linq2db-specific options for scoping a test run to providers and the progress heartbeat.";

		/// <inheritdoc/>
		public Task<bool> IsEnabledAsync() => Task.FromResult(true);

		/// <inheritdoc/>
		public IReadOnlyCollection<CommandLineOption> GetCommandLineOptions() =>
		[
			new CommandLineOption(
				TestCommandLine.ProviderOption,
				"Run only the named test provider(s), e.g. --provider SQLite.MS. Repeatable, and space- or comma-separated. REPLACES the providers configured in UserDataProviders.json — any provider with a defined connection string runs, no file edit needed. (EF Core test projects intersect their curated supported set instead.)",
				ArgumentArity.OneOrMore,
				isHidden: false),
			new CommandLineOption(
				TestCommandLine.TestProgressOption,
				"Write the live test-progress heartbeat. Optional value: a target directory or a .json file path; omit for the default path under .build/.agents.",
				ArgumentArity.ZeroOrOne,
				isHidden: false),
		];

		/// <inheritdoc/>
		public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments) =>
			ValidationResult.ValidTask;

		/// <inheritdoc/>
		public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions commandLineOptions) =>
			ValidationResult.ValidTask;
	}

	/// <summary>
	/// Microsoft.Testing.Platform build hook. The generated entry point invokes
	/// <see cref="AddExtensions"/> for every <c>TestingPlatformBuilderHook</c> MSBuild item; this one adds
	/// <see cref="TestRunCommandLineProvider"/> so the <c>--provider</c> / <c>--test-progress</c> options are
	/// recognised by the runner.
	/// </summary>
	public static class TestRunBuilderHook
	{
		/// <summary>Adds linq2db's command-line option provider to the test application builder.</summary>
		public static void AddExtensions(ITestApplicationBuilder builder, string[] arguments) =>
			builder.CommandLine.AddProvider(static () => new TestRunCommandLineProvider());
	}
}
