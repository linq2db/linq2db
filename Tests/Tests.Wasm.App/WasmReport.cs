namespace Tests.Wasm
{
	// The stdout report contract between the WASM app and the Tests.Wasm host; compiled into both.
	static class WasmReport
	{
		public const string Prefix = "L2DB-WASM-REPORT ";

		public const string Init                = nameof(Init);
		public const string Select              = nameof(Select);
		public const string SelectParameterized = nameof(SelectParameterized);
		public const string Update              = nameof(Update);
		public const string Insert              = nameof(Insert);
		public const string Merge               = nameof(Merge);
		public const string RoundTrip           = nameof(RoundTrip);

		// Recorded when a configuration throws outside its operations, so it still gets a report line.
		public const string Unexpected          = nameof(Unexpected);
	}
}
