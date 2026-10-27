namespace Tests.Wasm
{
	// The contract between the WASM app and the Tests.Wasm host (server route, stdout report); compiled into both.
	static class WasmReport
	{
		public const string Route  = "api/linq2db";
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

		// JSON property names of a report line.
		public static class Property
		{
			public const string Configuration   = "configuration";
			public const string Operations      = "operations";
			public const string Name            = "name";
			public const string Result          = "result";
			public const string ErrorType       = "errorType";
			public const string ErrorMessage    = "errorMessage";
			public const string Error           = "error";
			public const string ResolveAttempts = "resolveAttempts";
		}
	}
}
