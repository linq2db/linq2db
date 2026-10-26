using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using LinqToDB;

using NUnit.Framework;

using Shouldly;

namespace Tests.Wasm
{
	/// <summary>
	/// Runs the remote client inside the browser-wasm runtime (under Node) against every provider configuration.
	/// Bundles come from <c>Tests/Tests.Wasm/publish-wasm-bundles.ps1</c>; see <see cref="WasmBundleRunner"/>.
	/// </summary>
	[TestFixture]
	[Description("https://github.com/linq2db/linq2db/issues/5159")]
	public sealed class WasmRuntimeTests
	{
		static readonly string[] _workingModes = [WasmBundleRunner.Debug, WasmBundleRunner.TrimPartial];

		static readonly string[] _expectedOperations = ["Init", "Select", "SelectParameterized", "Update", "Insert", "Merge"];

		WasmServer?                                _server;
		readonly Dictionary<string, string?>       _preflight = new(StringComparer.Ordinal);
		readonly Dictionary<string, Task<ModeRun>> _runs      = new(StringComparer.Ordinal);

		[OneTimeSetUp]
		public async Task StartServer()
		{
			_server = await WasmServer.StartAsync();

			// Server-side construction is decided here, in-process, so a provider that cannot load on the runner is never
			// reported as a client failure.
			foreach (var row in ProviderMatrix.Rows)
			{
				try
				{
					using (var db = WasmServer.CreateDataConnection(row))
						db.DataProvider.Name.ShouldBe(row.ProviderName, $"matrix row '{row.Configuration}' constructs the wrong provider");

					await _server.Service.GetInfoAsync(row.Configuration);

					_preflight[row.Configuration] = null;
				}
				catch (Exception ex)
				{
					_preflight[row.Configuration] = ex.ToString();
				}
			}
		}

		[OneTimeTearDown]
		public async Task StopServer()
		{
			if (_server != null)
				await _server.DisposeAsync();
		}

		// [ActiveIssue] rewrites a result only when a SetUp/TearDown command beneath it has turned the exception into one (#6174).
		[SetUp]
		public void SetUp()
		{
		}

		public static IEnumerable<TestCaseData> ClientWorksCases                => Cases(_workingModes, r => !r.LoadsProviderAssembly);
		public static IEnumerable<TestCaseData> ClientLoadsProviderAssemblyCases => Cases(_workingModes, r => r.LoadsProviderAssembly);
		public static IEnumerable<TestCaseData> FullTrimmingCases               => Cases([WasmBundleRunner.TrimFull],         _ => true);
		public static IEnumerable<TestCaseData> NoReflectionJsonCases           => Cases([WasmBundleRunner.NoReflectionJson], _ => true);

		public static IEnumerable<TestCaseData> UnavailableCases =>
			ProviderMatrix.Rows
				.Where(r => !r.IsAvailable)
				.Select(r => new TestCaseData(r.Configuration));

		static IEnumerable<TestCaseData> Cases(string[] modes, Func<MatrixRow, bool> filter) =>
			from mode in modes
			from row  in ProviderMatrix.Rows
			where row.IsAvailable && filter(row)
			select new TestCaseData(mode, row.Configuration);

		[TestCaseSource(nameof(ClientWorksCases))]
		public Task ClientWorks(string mode, string configuration) => AssertClientAsync(mode, configuration);

		[ActiveIssue(6167, ErrorMessage = "Cannot load assembly")]
		[TestCaseSource(nameof(ClientLoadsProviderAssemblyCases))]
		public Task ClientLoadsProviderAssembly(string mode, string configuration) => AssertClientAsync(mode, configuration);

		[ActiveIssue(6168, ErrorMessage = "DeserializeNoConstructor")]
		[TestCaseSource(nameof(FullTrimmingCases))]
		public Task ClientFailsUnderFullTrimming(string mode, string configuration) => AssertClientAsync(mode, configuration);

		[ActiveIssue(6168, ErrorMessage = "JsonSerializerIsReflectionDisabled")]
		[TestCaseSource(nameof(NoReflectionJsonCases))]
		public Task ClientFailsWithoutReflectionJson(string mode, string configuration) => AssertClientAsync(mode, configuration);

		[TestCaseSource(nameof(UnavailableCases))]
		public void UnavailableOnRunner(string configuration)
		{
			var row = ProviderMatrix.Find(configuration)!;

			_preflight[configuration].ShouldNotBeNull(
				$"'{configuration}' is listed as unavailable ({row.UnavailableReason}) but its provider constructs on this runner: remove the exclusion.");
		}

		// The SqlServer mapping schema probes optional assemblies once per process, so the capture is checked per mode.
		[TestCase(WasmBundleRunner.Debug)]
		[TestCase(WasmBundleRunner.TrimPartial)]
		public async Task ResolveAttemptsAreCaptured(string mode)
		{
			var run = await GetRunAsync(mode);

			var attempts = run.Reports.Values
				.SelectMany(r => r.ResolveAttempts)
				.ToList();

			attempts.ShouldContain(a => a.StartsWith("Microsoft.SqlServer.Types", StringComparison.Ordinal), run.Output);
		}

		[Test]
		public void ProviderNamesAreCovered()
		{
			var constants = typeof(ProviderName)
				.GetFields(BindingFlags.Public | BindingFlags.Static)
				.Where(f => f.IsLiteral && f.FieldType == typeof(string))
				.Select(f => (string)f.GetRawConstantValue()!)
				.ToList();

			var rows = ProviderMatrix.Rows
				.Select(r => r.ProviderName)
				.ToHashSet(StringComparer.Ordinal);

			constants
				.Where(c => !rows.Contains(c) && !ProviderMatrix.Aliases.ContainsKey(c))
				.ShouldBeEmpty("ProviderName constants with neither a matrix row nor an alias entry");

			ProviderMatrix.Aliases.Keys
				.Where(rows.Contains)
				.ShouldBeEmpty("names listed both as a matrix row and as an alias");

			ProviderMatrix.Rows
				.GroupBy(r => r.Configuration, StringComparer.Ordinal)
				.Where(g => g.Count() > 1)
				.Select(g => g.Key)
				.ShouldBeEmpty("duplicate matrix configurations");
		}

		async Task AssertClientAsync(string mode, string configuration)
		{
			var row = ProviderMatrix.Find(configuration)!;

			if (_preflight[configuration] is { } serverError)
				throw new ShouldAssertException($"'{configuration}' cannot be constructed server-side on this runner; list it as unavailable or fix the environment:\n{serverError}");

			var run = await GetRunAsync(mode);

			if (!run.Reports.TryGetValue(configuration, out var report))
				throw new ShouldAssertException($"The '{mode}' bundle reported nothing for '{configuration}'.\n{run.Output}");

			if (report.ResolveAttempts.Count > 0)
				await TestContext.Out.WriteLineAsync($"Resolve attempts: {string.Join(", ", report.ResolveAttempts)}");

			foreach (var operation in report.Operations)
			{
				if (operation.Name == "Merge" && row.Merge == MergeOutcome.Throws)
				{
					(operation.ErrorMessage ?? "")
						.ShouldContain(
							"doesn't support SQL MERGE statement",
							customMessage: $"MERGE was expected to be refused for '{configuration}', got: {operation.Result ?? operation.Error}");

					continue;
				}

				operation.Error.ShouldBeNull($"{operation.Name} failed for '{configuration}' in '{mode}': {operation.ErrorType}: {operation.ErrorMessage}");
			}

			string[] expected = configuration == ProviderMatrix.RoundTripConfiguration
				? [.. _expectedOperations, "RoundTrip"]
				: _expectedOperations;

			report.Operations
				.Select(o => o.Name)
				.ShouldBe(expected);

			if (configuration == ProviderMatrix.RoundTripConfiguration)
			{
				report.Operations
					.Single(o => o.Name == "RoundTrip")
					.Result
					.ShouldBe("1,2");
			}
		}

		Task<ModeRun> GetRunAsync(string mode)
		{
			lock (_runs)
			{
				if (!_runs.TryGetValue(mode, out var run))
				{
					var configurations = ProviderMatrix.Rows
						.Where(r => r.IsAvailable && _preflight[r.Configuration] == null)
						.Select(r => r.Configuration)
						.ToList();

					_runs[mode] = run = WasmBundleRunner.RunAsync(mode, _server!.Address, configurations);
				}

				return run;
			}
		}
	}
}
