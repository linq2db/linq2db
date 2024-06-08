using System;
using System.Dynamic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace LinqToDB
{
	using Common;

	// implementation based on code from https://github.com/dotnet/runtime/issues/12587
	/// <summary>
	/// This class is used as COM object wrapper instead of dynamic keyword, as dynamic for COM is not supported on .net core till v5.
	/// See original issue https://github.com/dotnet/runtime/issues/12587.
	/// </summary>
	internal sealed class ComWrapper : DynamicObject, IDisposable
	{
		private object _instance;

		/// <summary>
		/// Caller method should use [SecuritySafeCritical] attribute. We don't put it here, as
		/// <paramref name="progID"/> value is not static here.
		/// </summary>
		/// <param name="progID">ID of COM class to create.</param>
		/// <returns>Dynamic disposable(!) wrapper over COM object.</returns>
		[SecurityCritical]
		public static dynamic Create(string progID)
		{
#if NETFRAMEWORK
			return new ComWrapper(Activator.CreateInstance(Type.GetTypeFromProgID(progID, true)!)!);
#else
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return new ComWrapper(Activator.CreateInstance(Type.GetTypeFromProgID(progID, true)!)!);
			}
#endif

			throw new PlatformNotSupportedException();
		}

		public static dynamic Wrap(object instance)
		{
			if (instance is null)
				throw new ArgumentNullException(nameof(instance));

			if (!instance.GetType().IsCOMObject)
				throw new ArgumentException("Object must be a COM object", nameof(instance));

			return new ComWrapper(instance);
		}

		private ComWrapper(object instance)
		{
			_instance = instance;
		}

		public override bool TryGetMember(GetMemberBinder binder, out object? result)
		{
			result = _instance.GetType().InvokeMember(binder.Name, BindingFlags.GetProperty, Type.DefaultBinder, _instance, [], null, CultureInfo.InvariantCulture, null);

			return true;
		}

		public override bool TrySetMember(SetMemberBinder binder, object? value)
		{
			_instance.GetType().InvokeMember(binder.Name, BindingFlags.SetProperty, Type.DefaultBinder, _instance, new object?[] { value }, null, CultureInfo.InvariantCulture, null);

			return true;
		}

		public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
		{
			result = _instance.GetType().InvokeMember(binder.Name, BindingFlags.InvokeMethod, Type.DefaultBinder, _instance, args, null, CultureInfo.InvariantCulture, null);

			return true;
		}

		[SecuritySafeCritical]
		void IDisposable.Dispose()
		{
			var instance = Interlocked.Exchange(ref _instance, null!);
			if (instance != null)
			{
#if NETFRAMEWORK
				Marshal.ReleaseComObject(instance);
#else
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					Marshal.ReleaseComObject(instance);
				}
#endif

				GC.SuppressFinalize(this);
			}
		}
	}
}
