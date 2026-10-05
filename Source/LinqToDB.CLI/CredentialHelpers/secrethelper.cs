// Minimal linq2db-cli credential helper (protocol 1) that keeps credentials in a JSON file.
//
// An illustration of the credential helper protocol, not a secure store: the file is plain text protected only by
// file permissions (owner-only on Linux and macOS). Use a helper over your platform's secret store for real secrets.
//
// Run it with "dotnet run" from the linq2db-cli configuration (the first run builds it, hence the timeout):
//
//     "credentialHelper": { "command": "dotnet", "args": ["run", "/path/to/secrethelper.cs", "--"], "timeout": 120 }
//
// or build it once and configure the produced executable (secrethelper, or secrethelper.exe on Windows):
//
//     dotnet build secrethelper.cs -o ~/.local/lib/linq2db-secrethelper
//     dotnet linq2db credentials set --credential-helper ~/.local/lib/linq2db-secrethelper/secrethelper --profile dev --user reader
//
// The file is $SECRETHELPER_FILE, or ~/.linq2db-secrethelper.json by default.
//
// Protocol summary: the verb (get, store, erase, list) is the only argument; the request is key=value lines on standard
// input (protocol, target, username, password); the answer is key=value lines on standard output; a failure is a
// non-zero exit code with a reason on standard error.

// A file-based app defaults to NativeAOT, which downloads the AOT toolchain on the first run and disables
// reflection-based JSON; this helper needs neither.
#:property PublishAot=false

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

var request = ReadRequest();

if (request.GetValueOrDefault("protocol") != "1")
{
	Answer("unsupported=protocol\n");
	return 0;
}

var verb   = args.Length == 1 ? args[0] : string.Empty;
var target = request.GetValueOrDefault("target") ?? string.Empty;

try
{
	switch (verb)
	{
		case "get":
		{
			var store = Load();

			if (store[target] is JsonObject entry)
				Answer($"username={entry["username"]?.GetValue<string>()}\npassword={entry["password"]?.GetValue<string>()}\n");

			return 0;
		}

		case "store":
		{
			using var _ = Lock();
			var store = Load();

			store[target] = new JsonObject
			{
				["username"] = request.GetValueOrDefault("username") ?? string.Empty,
				["password"] = request.GetValueOrDefault("password") ?? string.Empty,
			};

			Save(store);
			return 0;
		}

		case "erase":
		{
			using var _ = Lock();
			var store   = Load();
			var removed = store.Remove(target);

			if (removed)
				Save(store);

			Answer(removed ? "removed=true\n" : "removed=false\n");
			return 0;
		}

		case "list":
		{
			var answer = new StringBuilder();

			foreach (var (key, value) in Load())
				answer.Append($"target={key}\nusername={value?["username"]?.GetValue<string>()}\n\n");

			Answer(answer.ToString());
			return 0;
		}

		default:
			Answer("unsupported=verb\n");
			return 0;
	}
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
{
	// Never report "not found" or success when the store cannot be read or written.
	Console.Error.WriteLine($"secrethelper: {ex.Message}");
	return 1;
}

// The protocol is UTF-8 on every OS; Console.Out would use the console code page on Windows.
static void Answer(string text)
{
	using var output = Console.OpenStandardOutput();
	var bytes = new UTF8Encoding(false).GetBytes(text);
	output.Write(bytes, 0, bytes.Length);
}

static Dictionary<string, string> ReadRequest()
{
	var result = new Dictionary<string, string>(StringComparer.Ordinal);

	using var input = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false));

	while (input.ReadLine() is { } line)
	{
		var separator = line.IndexOf('=');

		if (separator > 0)
			result[line.Substring(0, separator)] = line.Substring(separator + 1);
	}

	return result;
}

static string GetStorePath()
{
	var path = Environment.GetEnvironmentVariable("SECRETHELPER_FILE");

	return string.IsNullOrEmpty(path)
		? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".linq2db-secrethelper.json")
		: path;
}

static JsonObject Load()
{
	var path = GetStorePath();

	if (!File.Exists(path))
		return [];

	return JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? throw new JsonException($"'{path}' does not contain a JSON object.");
}

// Serializes concurrent changes: the helper may run in several processes at once (a CLI command and an MCP server).
static FileStream Lock()
{
	var path = GetStorePath() + ".lock";

	for (var attempt = 0; ; attempt++)
	{
		try
		{
			return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
		}
		catch (IOException) when (attempt < 50)
		{
			Thread.Sleep(100);
		}
	}
}

static void Save(JsonObject store)
{
	var path      = GetStorePath();
	var temporary = $"{path}.{Environment.ProcessId}.tmp";
	var options   = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };

	if (!OperatingSystem.IsWindows())
		options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;

	using (var stream = new FileStream(temporary, options))
	using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
		store.WriteTo(writer);

	// The rename replaces the file atomically, so a concurrent reader sees either the old or the new store.
	File.Move(temporary, path, overwrite: true);
}
