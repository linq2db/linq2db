namespace NUnit.ParallelByResource
{
	/// <summary>
	/// Optional logging hook for <see cref="ResourceLaneDispatcher"/>. The dispatcher emits lane /
	/// gate routing trace through this sink; a host can wire it to its own logger or leave it off.
	/// </summary>
	public interface IParallelDiagnostics
	{
		void Log(string message);
	}
}
