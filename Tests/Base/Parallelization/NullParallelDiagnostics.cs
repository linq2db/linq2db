namespace NUnit.ParallelByResource
{
	/// <summary>No-op <see cref="IParallelDiagnostics"/> (the default).</summary>
	public sealed class NullParallelDiagnostics : IParallelDiagnostics
	{
		public static readonly NullParallelDiagnostics Instance = new();

		NullParallelDiagnostics() { }

		public void Log(string message) { }
	}
}
