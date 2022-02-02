using System;
using System.Runtime.InteropServices;
using LinqToDB.Data;

namespace LinqToDB
{
	internal class DisposeCommandOnExceptionRegion : IDisposable
	{
		private readonly DataConnection _dataConnection;

		public DisposeCommandOnExceptionRegion(DataConnection dataConnection)
		{
			_dataConnection = dataConnection;
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
				_dataConnection.DisposeCommand();
		}
	}
}
