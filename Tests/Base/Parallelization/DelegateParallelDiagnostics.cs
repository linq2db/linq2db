using System;

namespace NUnit.ParallelByResource
{
	/// <summary>Adapts an <see cref="Action{T}"/> to <see cref="IParallelDiagnostics"/>.</summary>
	public sealed class DelegateParallelDiagnostics : IParallelDiagnostics
	{
		readonly Action<string> _log;

		public DelegateParallelDiagnostics(Action<string> log)
		{
			_log = log ?? throw new ArgumentNullException(nameof(log));
		}

		public void Log(string message) => _log(message);
	}
}
