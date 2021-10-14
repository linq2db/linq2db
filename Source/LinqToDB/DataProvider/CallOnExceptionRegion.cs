using System;
using System.Runtime.InteropServices;
using LinqToDB.Data;

namespace LinqToDB
{
	/// <summary>
	/// Implements disposable region, which will call provided action, if region execution terminated due to
	/// exception.
	/// </summary>
	internal class CallOnExceptionRegion : IDisposable
	{
		private readonly DataConnection         _dc;
		private readonly Action<DataConnection> _action;

		public CallOnExceptionRegion(DataConnection dataConnection, Action<DataConnection> action)
		{
			_dc     = dataConnection;
			_action = action;
		}

		void IDisposable.Dispose()
		{
			// https://stackoverflow.com/questions/2830073/
			if (
#if NETFRAMEWORK || NETCOREAPP3_1
				// API not exposed till netcoreapp3.0
				// https://github.com/dotnet/corefx/pull/31169
				Marshal.GetExceptionPointers() != IntPtr.Zero ||
#endif
#pragma warning disable CS0618 // GetExceptionCode obsolete
				Marshal.GetExceptionCode() != 0)
#pragma warning restore CS0618
				_action(_dc);
		}
	}
}
