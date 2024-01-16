using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using LinqToDB.Data;

namespace LinqToDB
{
	internal sealed class DisposeCommandOnExceptionRegion : IExecutionScope
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
				Marshal.GetExceptionPointers() != IntPtr.Zero ||
#pragma warning disable CS0618 // GetExceptionCode obsolete
				Marshal.GetExceptionCode() != 0)
#pragma warning restore CS0618
				_dataConnection.DisposeCommand();
		}

		ValueTask IAsyncDisposable.DisposeAsync()
		{
			// https://stackoverflow.com/questions/2830073/
			if (
				Marshal.GetExceptionPointers() != IntPtr.Zero ||
#pragma warning disable CS0618 // GetExceptionCode obsolete
				Marshal.GetExceptionCode() != 0)
#pragma warning restore CS0618
#if NET6_0_OR_GREATER
				return _dataConnection.DisposeCommandAsync();
#else
				_dataConnection.DisposeCommand();
#endif

			return default;
		}
	}
}
