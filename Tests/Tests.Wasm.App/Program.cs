using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Remote.HttpClient.Client;

namespace Tests.Wasm
{
	// Runs inside the browser-wasm runtime under Node. Arguments: <server url> <round-trip configuration> <configuration>...
	// Writes one WasmReport.Prefix line per configuration to stdout; the Tests.Wasm host decides what is red.
	static class Program
	{
		static List<string>? _resolveAttempts;

		static async Task<int> Main(string[] args)
		{
			if (args.Length < 3)
			{
				Console.Error.WriteLine("usage: main.mjs <server url> <round-trip configuration> <configuration>...");
				return 2;
			}

			// Resolve attempts are reported, never failed: the SqlServer mapping schema probes optional assemblies on purpose.
			AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
			{
				_resolveAttempts?.Add(e.Name);
				return null;
			};

			var serverUrl              = new Uri(args[0]);
			var roundTripConfiguration = args[1];

			foreach (var configuration in args.Skip(2))
			{
				var operations = new List<Operation>();

				_resolveAttempts = new List<string>();

				// One configuration's failure must not cost the rest of the mode their reports.
				try
				{
					await RunConfigurationAsync(serverUrl, configuration, configuration == roundTripConfiguration, operations).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					operations.Add(new Operation(WasmReport.Unexpected, null, ex));
				}

				Console.WriteLine(WasmReport.Prefix + WriteReport(configuration, operations, _resolveAttempts));

				_resolveAttempts = null;
			}

			return 0;
		}

		static async Task RunConfigurationAsync(Uri serverUrl, string configuration, bool roundTrip, List<Operation> operations)
		{
			HttpClientDataContext? db = null;

			try
			{
				// Sync APIs block on a single-threaded runtime, so the configuration must be fetched asynchronously first.
				if (!await RunAsync(operations, WasmReport.Init, async () =>
				{
					db = new HttpClientDataContext(serverUrl, WasmReport.Route, o => o.UseConfiguration(configuration));

					await db.InitHttpClientAsync().ConfigureAwait(false);
					return null;
				}).ConfigureAwait(false))
					return;

				RunOperations(db!, operations);

				if (roundTrip)
				{
					await RunAsync(operations, WasmReport.RoundTrip, async () =>
					{
						var rows = await db!.GetTable<WasmPerson>()
							.OrderBy(p => p.Id)
							.ToListAsync()
							.ConfigureAwait(false);

						return string.Join(",", rows.Select(p => p.Id));
					}).ConfigureAwait(false);
				}
			}
			finally
			{
				if (db != null)
					await db.DisposeAsync().ConfigureAwait(false);
			}
		}

		static void RunOperations(HttpClientDataContext db, List<Operation> operations)
		{
			var persons = db.GetTable<WasmPerson>();
			var name    = "John";

			Run(operations, WasmReport.Select, () => persons
				.Where(p => p.Name.StartsWith("J") && p.BirthDate > new DateTime(2000, 1, 1))
				.Select(p => new { p.Id, Upper = p.Name.ToUpper(), p.Name.Length })
				.ToSqlQuery()
				.Sql);

			Run(operations, WasmReport.SelectParameterized, () => persons
				.Where(p => p.Name == name)
				.ToSqlQuery()
				.Sql);

			Run(operations, WasmReport.Update, () => persons
				.Where(p => p.Id == 1)
				.Set(p => p.Name, "Jack")
				.ToSqlQuery()
				.Sql);

			Run(operations, WasmReport.Insert, () => persons
				.Value(p => p.Id, 3)
				.Value(p => p.Name, "Jim")
				.Value(p => p.BirthDate, new DateTime(2001, 2, 3))
				.ToSqlQuery()
				.Sql);

			// Non-empty source and Update/Insert only: the delete and by-source operations and an empty source have refusal
			// paths of their own, and the expectation per configuration is about the builder's MERGE support alone.
			Run(operations, WasmReport.Merge, () => persons
				.Merge()
				.Using(new[]
				{
					new WasmPerson { Id = 1, Name = "John", BirthDate = new DateTime(2001, 1, 1) },
					new WasmPerson { Id = 4, Name = "Jill", BirthDate = new DateTime(2002, 2, 2) },
				})
				.OnTargetKey()
				.UpdateWhenMatched()
				.InsertWhenNotMatched()
				.ToSqlQuery()
				.Sql);
		}

		static void Run(List<Operation> operations, string name, Func<string?> action)
		{
			try
			{
				operations.Add(new Operation(name, action(), null));
			}
			catch (Exception ex)
			{
				operations.Add(new Operation(name, null, ex));
			}
		}

		static async Task<bool> RunAsync(List<Operation> operations, string name, Func<Task<string?>> action)
		{
			try
			{
				var result = await action().ConfigureAwait(false);

				operations.Add(new Operation(name, result, null));
				return true;
			}
			catch (Exception ex)
			{
				operations.Add(new Operation(name, null, ex));
				return false;
			}
		}

		// Utf8JsonWriter, not JsonSerializer: NoReflectionJson disables reflection-based serialization and TrimFull trims the metadata it needs.
		static string WriteReport(string configuration, List<Operation> operations, List<string> resolveAttempts)
		{
			using var stream = new MemoryStream();

			using (var writer = new Utf8JsonWriter(stream))
			{
				writer.WriteStartObject();
				writer.WriteString(WasmReport.Property.Configuration, configuration);

				writer.WriteStartArray(WasmReport.Property.Operations);

				foreach (var operation in operations)
				{
					writer.WriteStartObject();
					writer.WriteString(WasmReport.Property.Name, operation.Name);

					if (operation.Error == null)
					{
						writer.WriteString(WasmReport.Property.Result, operation.Result);
					}
					else
					{
						writer.WriteString(WasmReport.Property.ErrorType,    operation.Error.GetType().FullName);
						writer.WriteString(WasmReport.Property.ErrorMessage, operation.Error.Message);
						writer.WriteString(WasmReport.Property.Error,        operation.Error.ToString());
					}

					writer.WriteEndObject();
				}

				writer.WriteEndArray();

				writer.WriteStartArray(WasmReport.Property.ResolveAttempts);

				foreach (var attempt in resolveAttempts)
					writer.WriteStringValue(attempt);

				writer.WriteEndArray();
				writer.WriteEndObject();
			}

			return Encoding.UTF8.GetString(stream.ToArray());
		}

		sealed record Operation(string Name, string? Result, Exception? Error);
	}
}
