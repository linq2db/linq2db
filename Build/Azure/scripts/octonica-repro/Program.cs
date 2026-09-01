// Repro for Octonica.ClickHouseClient: abandoning a ClickHouseDataReader before the result set is
// drained silently breaks the ClickHouseConnection. No exception reaches the caller at that point;
// the *next* command on the same connection fails with
//
//     Octonica.ClickHouseClient.Exceptions.ClickHouseException : The connection is closed.
//     ErrorCode: 3
//
// which is ClickHouseErrorCodes.ConnectionClosed, thrown from the ConnectionSession constructor
// because ClickHouseConnection's TcpClient is already null.
//
// The swallow happens in ClickHouseDataReader.Close(disposing: true, ...): it sends Cancel and then
// drains server messages to EndOfStream; anything the drain switch does not handle throws, and the
// catch discards the exception when disposing:
//
//     catch (Exception ex) { State = Broken; await _session.SetFailed(ex, false, async); if (disposing) return; }
//
// The FirstChanceException hook below prints that discarded exception, which is otherwise invisible.
//
// Usage: OctonicaRepro "Host=localhost;Port=9000;Database=default;User=default;Password="

using System.Runtime.ExceptionServices;
using Octonica.ClickHouseClient;

internal static class Program
{
	private const string StringTable  = "octonica_repro_s";
	private const string NumericTable = "octonica_repro_n";
	private const int    RowCount     = 200_000;

	private static string _connectionString = "Host=localhost;Port=9000;Database=default;User=default;Password=";
	private static volatile bool _traceFirstChance;

	private static int Main(string[] args)
	{
		if (args.Length > 0)
			_connectionString = args[0];

		AppDomain.CurrentDomain.FirstChanceException += OnFirstChance;

		Console.WriteLine($"Octonica.ClickHouseClient : {typeof(ClickHouseConnection).Assembly.GetName().Version}");

		using (var connection = Open())
		using (var command = connection.CreateCommand("SELECT version()"))
			Console.WriteLine($"ClickHouse server         : {command.ExecuteScalar()}");

		Console.WriteLine();
		Setup();

		var failures = 0;

		failures += Run("A  control: reader fully drained, String column",  () => DrainFully(StringTable));
		failures += Run("B  reader abandoned after 1 row, String column",   () => AbandonAfterFirstRow(StringTable));
		failures += Run("C  reader abandoned after 1 row, Float64 column",  () => AbandonAfterFirstRow(NumericTable));
		failures += Run("D  reader abandoned before any row, String column",() => AbandonWithoutRead(StringTable));
		failures += Run("E  reader abandoned after 1 row, async",           () => AbandonAfterFirstRowAsync(StringTable).GetAwaiter().GetResult());

		Cleanup();

		Console.WriteLine();
		Console.WriteLine(failures == 0
			? "NOT REPRODUCED - every case ran a follow-up command successfully."
			: $"REPRODUCED - {failures} case(s) broke the connection.");

		return failures == 0 ? 0 : 1;
	}

	private static void OnFirstChance(object? sender, FirstChanceExceptionEventArgs e)
	{
		if (_traceFirstChance)
			Console.WriteLine($"    [swallowed] {e.Exception.GetType().FullName}: {e.Exception.Message}");
	}

	private static ClickHouseConnection Open()
	{
		var connection = new ClickHouseConnection(_connectionString);
		connection.Open();
		return connection;
	}

	private static void Execute(ClickHouseConnection connection, string sql)
	{
		using var command = connection.CreateCommand(sql);
		command.ExecuteNonQuery();
	}

	private static void Setup()
	{
		using var connection = Open();

		Execute(connection, $"DROP TABLE IF EXISTS {StringTable}");
		Execute(connection, $"DROP TABLE IF EXISTS {NumericTable}");
		Execute(connection, $"CREATE TABLE {StringTable} (Id UInt64, Value String) ENGINE = MergeTree ORDER BY Id");
		Execute(connection, $"CREATE TABLE {NumericTable} (Id UInt64, Value Float64) ENGINE = MergeTree ORDER BY Id");
		Execute(connection, $"INSERT INTO {StringTable} SELECT number, toString(number) FROM numbers({RowCount})");
		Execute(connection, $"INSERT INTO {NumericTable} SELECT number, toFloat64(number) / 7 FROM numbers({RowCount})");

		Console.WriteLine($"Seeded {StringTable} and {NumericTable} with {RowCount} rows each.");
	}

	private static void Cleanup()
	{
		try
		{
			using var connection = Open();
			Execute(connection, $"DROP TABLE IF EXISTS {StringTable}");
			Execute(connection, $"DROP TABLE IF EXISTS {NumericTable}");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"cleanup failed: {ex.Message}");
		}
	}

	// Each case gets its own connection so a broken one cannot cascade into the next case.
	private static int Run(string name, Action body)
	{
		Console.WriteLine(name);
		try
		{
			body();
			Console.WriteLine("    ok - follow-up command succeeded");
			return 0;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"    FAILED - {ex.GetType().FullName}: {ex.Message}");
			return 1;
		}
		finally
		{
			_traceFirstChance = false;
		}
	}

	private static void DrainFully(string table)
	{
		using var connection = Open();

		using (var command = connection.CreateCommand($"SELECT Id, Value FROM {table}"))
		using (var reader = command.ExecuteReader())
		{
			while (reader.Read())
			{
			}
		}

		FollowUp(connection);
	}

	private static void AbandonAfterFirstRow(string table)
	{
		using var connection = Open();

		using (var command = connection.CreateCommand($"SELECT Id, Value FROM {table}"))
		{
			var reader = command.ExecuteReader();
			reader.Read();

			_traceFirstChance = true;
			reader.Dispose();   // cancel + drain happens here
			_traceFirstChance = false;
		}

		FollowUp(connection);
	}

	private static void AbandonWithoutRead(string table)
	{
		using var connection = Open();

		using (var command = connection.CreateCommand($"SELECT Id, Value FROM {table}"))
		{
			var reader = command.ExecuteReader();

			_traceFirstChance = true;
			reader.Dispose();
			_traceFirstChance = false;
		}

		FollowUp(connection);
	}

	private static async Task AbandonAfterFirstRowAsync(string table)
	{
		await using var connection = new ClickHouseConnection(_connectionString);
		await connection.OpenAsync();

		await using (var command = connection.CreateCommand($"SELECT Id, Value FROM {table}"))
		{
			var reader = await command.ExecuteReaderAsync();
			await reader.ReadAsync();

			_traceFirstChance = true;
			await reader.DisposeAsync();
			_traceFirstChance = false;
		}

		await using var followUp = connection.CreateCommand("SELECT 1");
		await followUp.ExecuteScalarAsync();
	}

	// The command that actually surfaces the break.
	private static void FollowUp(ClickHouseConnection connection)
	{
		using var command = connection.CreateCommand("SELECT 1");
		command.ExecuteScalar();
	}
}
