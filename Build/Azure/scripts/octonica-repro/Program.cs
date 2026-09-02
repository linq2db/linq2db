// Repro harness for Octonica.ClickHouseClient against ClickHouse 26.8.
//
// Symptom seen in CI: a command fails with
//
//     Octonica.ClickHouseClient.Exceptions.ClickHouseException : The connection is closed.
//     ErrorCode: 3
//
// That is ClickHouseErrorCodes.ConnectionClosed, thrown from the ConnectionSession constructor
// (ClickHouseConnection.cs:1041-1044) because TcpClient is already null. It is a *secondary*
// symptom: a previous operation on the connection called SetFailed, which nulls TcpClient via
// ReleaseOnFailure and disposes the socket. Server-side, ClickHouse logs exactly one
// NETWORK_ERROR (210) "Broken pipe, while writing to socket", i.e. the client hung up while the
// server was still writing the result.
//
// ClickHouseDataReader.Close(disposing: true) discards whatever went wrong:
//     catch (Exception ex) { State = Broken; await _session.SetFailed(ex, false, async); if (disposing) return; }
// The FirstChanceException hook below prints that discarded exception.
//
// Usage: OctonicaRepro "Host=localhost;Port=9000;Database=default;User=default;Password="

using System.Runtime.ExceptionServices;
using Octonica.ClickHouseClient;

internal static class Program
{
	private const string StringTable  = "octonica_repro_s";
	private const string NumericTable = "octonica_repro_n";
	private const string BooleanTable = "BooleanTable";
	private const int    RowCount     = 200_000;

	// 2 * 3 * 3 * 4 * 3 * 4 * 3 * 4 - the cross product linq2db's BooleanTests.Test builds.
	private const int BooleanRowCount = 10368;

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
		Console.WriteLine();

		var failures = 0;

		failures += Run("A  plain SELECT, reader fully drained (control)",   () => DrainFully(StringTable));
		failures += Run("B  plain SELECT, abandoned after 1 row",            () => AbandonAfterFirstRow(StringTable));
		failures += Run("C  plain SELECT, abandoned after 1 row, Float64",   () => AbandonAfterFirstRow(NumericTable));
		failures += Run("D  plain SELECT, abandoned before any row",         () => AbandonWithoutRead(StringTable));
		failures += Run("E  plain SELECT, abandoned after 1 row, async",     () => AbandonAfterFirstRowAsync(StringTable).GetAwaiter().GetResult());
		failures += Run("F  BooleanTable GROUP BY, fully drained",           DrainFailingQuery);
		failures += Run("G  BooleanTable GROUP BY, abandoned after 1 row",   AbandonFailingQueryAfterFirstRow);
		failures += Run("H  BooleanTable GROUP BY, abandoned before any row",AbandonFailingQueryWithoutRead);

		Cleanup();

		Console.WriteLine();
		Console.WriteLine(failures == 0
			? "NOT REPRODUCED - every case ran a follow-up command successfully."
			: $"REPRODUCED - {failures} case(s) failed.");

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
		Execute(connection, $"DROP TABLE IF EXISTS {BooleanTable}");

		Execute(connection, $"CREATE TABLE {StringTable} (Id UInt64, Value String) ENGINE = MergeTree ORDER BY Id");
		Execute(connection, $"CREATE TABLE {NumericTable} (Id UInt64, Value Float64) ENGINE = MergeTree ORDER BY Id");
		Execute(connection, $"INSERT INTO {StringTable} SELECT number, toString(number) FROM numbers({RowCount})");
		Execute(connection, $"INSERT INTO {NumericTable} SELECT number, toFloat64(number) / 7 FROM numbers({RowCount})");
		Console.WriteLine($"Seeded {StringTable} / {NumericTable} with {RowCount} rows each.");

		// Exactly the table linq2db's BooleanTests.Test creates: [PrimaryKey] int Id plus bool /
		// bool? / int / int? / decimal / decimal? / double / double?, mapped to these ClickHouse
		// types, with ENGINE = MergeTree() ORDER BY Id because Id is a primary key.
		Execute(connection, $@"CREATE TABLE {BooleanTable}
(
	Id       Int32,
	Boolean  Bool,
	BooleanN Nullable(Bool),
	Int32    Int32,
	Int32N   Nullable(Int32),
	Decimal  Decimal128(10),
	DecimalN Nullable(Decimal128(10)),
	Double   Float64,
	DoubleN  Nullable(Float64)
)
ENGINE = MergeTree()
ORDER BY Id");

		// The same cross product, laid out by positional strides so every combination appears once.
		Execute(connection, $@"INSERT INTO {BooleanTable}
SELECT
	toInt32(number) + 1                                                                     AS Id,
	intDiv(number, 5184) % 2 = 0                                                            AS Boolean,
	multiIf(intDiv(number, 1728) % 3 = 0, true, intDiv(number, 1728) % 3 = 1, false, NULL)  AS BooleanN,
	toInt32(intDiv(number, 576) % 3) - 1                                                    AS Int32,
	if(intDiv(number, 144) % 4 = 3, NULL, toInt32(intDiv(number, 144) % 4) - 1)             AS Int32N,
	toDecimal128(toFloat64(toInt32(intDiv(number, 48) % 3) - 1) / 10, 10)                   AS Decimal,
	if(intDiv(number, 12) % 4 = 3, NULL, toDecimal128(toFloat64(toInt32(intDiv(number, 12) % 4) - 1) / 10, 10)) AS DecimalN,
	toFloat64(toInt32(intDiv(number, 4) % 3) - 1) / 10                                      AS Double,
	if(number % 4 = 3, NULL, toFloat64(toInt32(number % 4) - 1) / 10)                       AS DoubleN
FROM numbers({BooleanRowCount})");

		using var check = connection.CreateCommand($"SELECT count() FROM {BooleanTable}");
		Console.WriteLine($"Seeded {BooleanTable} with {check.ExecuteScalar()} rows (expected {BooleanRowCount}).");
	}

	private static void Cleanup()
	{
		try
		{
			using var connection = Open();
			Execute(connection, $"DROP TABLE IF EXISTS {StringTable}");
			Execute(connection, $"DROP TABLE IF EXISTS {NumericTable}");
			Execute(connection, $"DROP TABLE IF EXISTS {BooleanTable}");
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
			Console.WriteLine("    ok");
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

	// ---- plain SELECT shapes -------------------------------------------------------------

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
			reader.Dispose();
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

	// ---- the query that actually fails in CI ---------------------------------------------

	// 24 columns: the grouping key plus 23 COUNT(CASE WHEN ... THEN 1 ELSE NULL END) aggregates
	// over Bool / Nullable(Bool) / Int32 / Nullable(Int32) / Decimal128(10) / Nullable(Decimal128)
	// / Float64 / Nullable(Float64), grouped by Id so it yields one row per source row.
	private static void DrainFailingQuery()
	{
		using var connection = Open();

		var rows = 0;

		_traceFirstChance = true;
		using (var command = connection.CreateCommand(FailingQuery.Sql))
		using (var reader = command.ExecuteReader())
		{
			while (reader.Read())
				rows++;
		}
		_traceFirstChance = false;

		Console.WriteLine($"    read {rows} rows (expected {BooleanRowCount})");

		FollowUp(connection);

		if (rows != BooleanRowCount)
			throw new InvalidOperationException($"expected {BooleanRowCount} rows, got {rows}");
	}

	private static void AbandonFailingQueryAfterFirstRow()
	{
		using var connection = Open();

		using (var command = connection.CreateCommand(FailingQuery.Sql))
		{
			var reader = command.ExecuteReader();
			reader.Read();

			_traceFirstChance = true;
			reader.Dispose();
			_traceFirstChance = false;
		}

		FollowUp(connection);
	}

	private static void AbandonFailingQueryWithoutRead()
	{
		using var connection = Open();

		using (var command = connection.CreateCommand(FailingQuery.Sql))
		{
			var reader = command.ExecuteReader();

			_traceFirstChance = true;
			reader.Dispose();
			_traceFirstChance = false;
		}

		FollowUp(connection);
	}

	// The command that surfaces a broken connection.
	private static void FollowUp(ClickHouseConnection connection)
	{
		using var command = connection.CreateCommand("SELECT 1");
		command.ExecuteScalar();
	}
}
