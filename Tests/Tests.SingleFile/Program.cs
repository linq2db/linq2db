using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;

// Smoke test that verifies linq2db works from a PublishSingleFile bundle.
//
// When published with `-p:PublishSingleFile=true --self-contained`, Assembly.Location
// returns an empty string. The old provider-detection heuristic probed
// File.Exists(Path.GetDirectoryName(Assembly.Location) + "/XYZ.dll") and either
// fell back to the CWD (wrong answer likely) or picked an adapter whose underlying
// ADO.NET driver assembly isn't actually referenced — the exact failure reported in
// linq2db discussion #5488.
//
// This binary references Microsoft.Data.Sqlite ONLY (no System.Data.SQLite). If
// detection works correctly it picks SQLiteProvider.Microsoft, the connection opens,
// a query returns two rows, and we exit 0. Any assembly-load exception bubbles up
// and exits non-zero, failing the pipeline step.

try
{
	var options = new DataOptions()
		.UseSQLite("Data Source=:memory:", SQLiteProvider.AutoDetect);

	using var db = new DataConnection(options);

	db.Execute("CREATE TABLE t(id INT, name TEXT)");
	db.Execute("INSERT INTO t VALUES(1, 'a')");
	db.Execute("INSERT INTO t VALUES(2, 'b')");

	var rows = db.FromSql<Row>("SELECT id, name FROM t ORDER BY id").ToArray();

	if (rows.Length != 2 || rows[0].Name != "a" || rows[1].Name != "b")
	{
		Console.Error.WriteLine($"single-file smoke test failed: unexpected rows [{string.Join(",", rows.Select(r => $"{r.Id}:{r.Name}"))}]");
		return 1;
	}

	Console.WriteLine("ok");
	return 0;
}
catch (Exception ex)
{
	Console.Error.WriteLine($"single-file smoke test failed with exception: {ex}");
	return 2;
}

file sealed record Row(int Id, string Name);
