﻿using System;
using System.Dynamic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using LinqToDB.Common;

namespace LinqToDB
{
	// implementation based on code from https://github.com/dotnet/runtime/issues/12587
	/// <summary>
	/// This class is used as COM object wrapper instead of dynamic keyword, as dynamic for COM is not supported on .net core till v5.
	/// See original issue https://github.com/dotnet/runtime/issues/12587.
	/// </summary>
	internal class ComWrapper : DynamicObject, IDisposable
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
			return new ComWrapper(Activator.CreateInstance(Type.GetTypeFromProgID(progID, true)!)!);
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
			result = _instance.GetType().InvokeMember(binder.Name, BindingFlags.GetProperty, Type.DefaultBinder, _instance, Array<object?>.Empty);

			return true;
		}

		public override bool TrySetMember(SetMemberBinder binder, object? value)
		{
			_instance.GetType().InvokeMember(binder.Name, BindingFlags.SetProperty, Type.DefaultBinder, _instance, new object?[] { value });

			return true;
		}

		public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object? result)
		{
			result = _instance.GetType().InvokeMember(binder.Name, BindingFlags.InvokeMethod, Type.DefaultBinder, _instance, args);

			return true;
		}

		[SecuritySafeCritical]
		void IDisposable.Dispose()
		{
			var instance = Interlocked.Exchange(ref _instance, null!);
			if (instance != null)
			{
				Marshal.ReleaseComObject(instance);
				GC.SuppressFinalize(this);
			}
		}
	}
}
