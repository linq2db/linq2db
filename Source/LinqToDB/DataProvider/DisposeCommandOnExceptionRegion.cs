using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using LinqToDB.Data;

namespace LinqToDB
{
	internal class DisposeCommandOnExceptionRegion : IExecutionScope
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
#if NETFRAMEWORK || NETCOREAPP3_1_OR_GREATER
				// API not exposed till netcoreapp3.0
				// https://github.com/dotnet/corefx/pull/31169
				Marshal.GetExceptionPointers() != IntPtr.Zero ||
#endif
#pragma warning disable CS0618 // GetExceptionCode obsolete
				Marshal.GetExceptionCode() != 0)
#pragma warning restore CS0618
				_dataConnection.DisposeCommand();
		}

#if NATIVE_ASYNC
		ValueTask IAsyncDisposable.DisposeAsync()
		{
			// https://stackoverflow.com/questions/2830073/
			if (
#if NETCOREAPP3_1_OR_GREATER
				// API not exposed till netcoreapp3.0
				// https://github.com/dotnet/corefx/pull/31169
				Marshal.GetExceptionPointers() != IntPtr.Zero ||
#endif
#pragma warning disable CS0618 // GetExceptionCode obsolete
				Marshal.GetExceptionCode() != 0)
#pragma warning restore CS0618
#if NETSTANDARD2_1PLUS
				return _dataConnection.DisposeCommandAsync();
#else
				_dataConnection.DisposeCommand();
#endif

			return default;
		}
#endif
		}
}
