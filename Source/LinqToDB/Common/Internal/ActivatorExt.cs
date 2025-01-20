using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace LinqToDB.Common.Internal
{
	/// <summary>
	/// Internal APIs to call reflection-based invoke operations with automatic unwrap of
	/// <see cref="TargetInvocationException"/> on error.
	/// </summary>
	public static class ActivatorExt
	{
		/// <summary>
		/// Creates instance of <paramref name="type"/> type. Ensures that instance is not <c>null</c>.
		/// </summary>
		public static object CreateInstance(Type type) => CreateInstance(type, false);

		/// <summary>
		/// Creates instance of <paramref name="type"/> and asserts it inherits/implements <typeparamref name="T"/>.
		/// </summary>
		public static T CreateInstance<T>(Type type)
			where T : class
			=> CreateInstance<T>(type, false);

		/// <summary>
		/// Creates instance of <typeparamref name="T"/> type.
		/// </summary>
		public static T CreateInstance<T>()
		{
			try
			{
#pragma warning disable RS0030 // Do not use banned APIs
				return Activator.CreateInstance<T>();
#pragma warning restore RS0030 // Do not use banned APIs
			}
			catch (TargetInvocationException tex) when (tex.InnerException != null)
			{
				throw tex.InnerException;
			}
		}

		/// <summary>
		/// Creates instance of <paramref name="type"/> type.
		/// </summary>
		public static object CreateInstance(Type type, bool nonPublic)
		{
			try
			{
#pragma warning disable RS0030 // Do not use banned APIs
				return Activator.CreateInstance(type, nonPublic)
#pragma warning restore RS0030 // Do not use banned APIs
					// caller used Nullable<T> type?
					?? throw new InvalidOperationException($"Instance of type '{type.FullName}' cannot be created by this API.");
			}
			catch (TargetInvocationException tex) when (tex.InnerException != null)
			{
				throw tex.InnerException;
			}
		}

		/// <summary>
		/// Creates instance of <paramref name="type"/> and asserts it inherits/implements <typeparamref name="T"/>.
		/// </summary>
		public static T CreateInstance<T>(Type type, bool nonPublic)
			where T : class
		{
			try
			{
#pragma warning disable RS0030 // Do not use banned APIs
				var instance = Activator.CreateInstance(type, nonPublic);
#pragma warning restore RS0030 // Do not use banned APIs

				if (instance is T target)
					return target;

				throw new LinqToDBException($"Type '{type.FullName}' must be assignable to '{typeof(T).FullName}'.");
			}
			catch (TargetInvocationException tex) when (tex.InnerException != null)
			{
				throw tex.InnerException;
			}
		}

		/// <summary>
		/// Creates instance of <paramref name="type"/> type.
		/// </summary>
		public static object CreateInstance(Type type, params object?[]? args)
		{
			try
			{
#pragma warning disable RS0030 // Do not use banned APIs
				return Activator.CreateInstance(type, args)
#pragma warning restore RS0030 // Do not use banned APIs
					// caller used Nullable<T> type?
					?? throw new InvalidOperationException($"Instance of type '{type.FullName}' cannot be created by this API.");
			}
			catch (TargetInvocationException tex) when (tex.InnerException != null)
			{
				throw tex.InnerException;
			}
		}

		/// <summary>
		/// Creates instance of <paramref name="type"/> and asserts it inherits/implements <typeparamref name="T"/>.
		/// </summary>
		public static T CreateInstance<T>(Type type, params object?[]? args)
			where T : class
		{
			try
			{
#pragma warning disable RS0030 // Do not use banned APIs
				var instance = Activator.CreateInstance(type, args);
#pragma warning restore RS0030 // Do not use banned APIs

				if (instance is T target)
					return target;

				throw new LinqToDBException($"Type '{type.FullName}' must be assignable to '{typeof(T).FullName}'.");
			}
			catch (TargetInvocationException tex) when (tex.InnerException != null)
			{
				throw tex.InnerException;
			}
		}


		#region ConstructorInfo Extensions

		/// <summary>
		/// Creates object instance by invoking contructor.
		/// </summary>
		public static object InvokeExt(this ConstructorInfo ctor, object?[]? parameters)
		{
			try
			{
#pragma warning disable RS0030 // Do not use banned APIs
				return ctor.Invoke(parameters);
#pragma warning restore RS0030 // Do not use banned APIs
			}
			catch (TargetInvocationException tex) when (tex.InnerException != null)
			{
				throw tex.InnerException;
			}
		}

		/// <summary>
		/// Creates object instance by invoking contructor and asserts created instance inherits/implements <typeparamref name="T"/>.
		/// </summary>
		public static T InvokeExt<T>(this ConstructorInfo ctor, object?[]? parameters)
			where T : class
		{
			try
			{
#pragma warning disable RS0030 // Do not use banned APIs
				var instance = ctor.Invoke(parameters);
#pragma warning restore RS0030 // Do not use banned APIs

				if (instance is T target)
					return target;

				throw new LinqToDBException($"Type '{ctor.DeclaringType!.FullName}' must be assignable to '{typeof(T).FullName}'.");
			}
			catch (TargetInvocationException tex) when (tex.InnerException != null)
			{
				throw tex.InnerException;
			}
		}

		#endregion

		#region Delegate Extensions

		/// <summary>
		/// Invokes delegate.
		/// </summary>
		public static object? DynamicInvokeExt(this Delegate method, params object?[]? args)
		{
			try
			{
#pragma warning disable RS0030 // Do not use banned APIs
				return method.DynamicInvoke(args);
#pragma warning restore RS0030 // Do not use banned APIs
			}
			catch (TargetInvocationException tex) when (tex.InnerException != null)
			{
				throw tex.InnerException;
			}
		}

		/// <summary>
		/// Invokes delegate and asserts it return value of <typeparamref name="T"/> type.
		/// </summary>
		public static T DynamicInvokeExt<T>(this Delegate method, params object?[]? args)
		{
			try
			{
#pragma warning disable RS0030 // Do not use banned APIs
				var result = method.DynamicInvoke(args);
#pragma warning restore RS0030 // Do not use banned APIs

				if (result is T target) return target;
				if (result is null) return (T)result!;

				throw new LinqToDBException($"Returned value expected to be assignable to '{typeof(T).FullName}' but had '{result?.GetType().FullName}' type.");
			}
			catch (TargetInvocationException tex) when (tex.InnerException != null)
			{
				throw tex.InnerException;
			}
		}

		#endregion

		#region MethodBase Extensions

		/// <summary>
		/// Invokes method.
		/// </summary>
		public static object? InvokeExt(this MethodBase method, object? obj, object?[]? parameters)
		{
			try
			{
#pragma warning disable RS0030 // Do not use banned APIs
				return method.Invoke(obj, parameters);
#pragma warning restore RS0030 // Do not use banned APIs
			}
			catch (TargetInvocationException tex) when (tex.InnerException != null)
			{
				throw tex.InnerException;
			}
		}

		/// <summary>
		/// Invokes method and asserts it return value of <typeparamref name="T"/> type.
		/// </summary>
		public static T InvokeExt<T>(this MethodBase method, object? obj, object?[]? parameters)
		{
			try
			{
#pragma warning disable RS0030 // Do not use banned APIs
				var result = method.Invoke(obj, parameters);
#pragma warning restore RS0030 // Do not use banned APIs

				if (result is T target) return target;
				if (result is null) return (T)result!;

				throw new LinqToDBException($"Returned value expected to be assignable to '{typeof(T).FullName}' but had '{result?.GetType().FullName}' type.");
			}
			catch (TargetInvocationException tex) when (tex.InnerException != null)
			{
				throw tex.InnerException;
			}
		}

		#endregion
	}
}
