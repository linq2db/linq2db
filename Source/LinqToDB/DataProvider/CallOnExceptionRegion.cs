using System;
using System.Runtime.InteropServices;

namespace LinqToDB
{
	/// <summary>
	/// Implements disposable region, which will call provided action, if region execution terminated due to
	/// exception.
	/// </summary>
	internal class CallOnExceptionRegion : IDisposable
	{
		private readonly Action _action;

		public CallOnExceptionRegion(Action action)
		{
			_action = action;
		}

		void IDisposable.Dispose()
		{
			// https://stackoverflow.com/questions/2830073/
			if (
#if NET45 || NET46
				// API not exposed till netcoreapp3.0
				// https://github.com/dotnet/corefx/pull/31169
				Marshal.GetExceptionPointers() != IntPtr.Zero ||
#endif
#pragma warning disable CS0618 // GetExceptionCode obsolete
				Marshal.GetExceptionCode() != 0)
#pragma warning restore CS0618
				_action();
		}
	}
}
