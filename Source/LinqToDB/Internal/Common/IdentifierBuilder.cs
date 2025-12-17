using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

using LinqToDB.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Linq;

namespace LinqToDB.Internal.Common
{
	/// <summary>
	/// Internal infrastructure API.
	/// Provides functionality for <see cref="IConfigurationID.ConfigurationID"/> generation.
	/// </summary>
	public readonly struct IdentifierBuilder : IDisposable
	{
		readonly ObjectPool<StringBuilder>.RentedElement _sb;

		public IdentifierBuilder()
		{
			_sb = Pools.StringBuilder.Allocate();
		}

		public IdentifierBuilder(object? data)
		{
			_sb = Pools.StringBuilder.Allocate();

			Add(data);
		}

		static IdentifierBuilder()
		{
			Query.CacheCleaners.Enqueue(ClearCache);
		}

		static void ClearCache()
		{
			_expressions.Clear();
			_types.      Clear();
			_identifiers.Clear();
			_objects.    Clear();
		}

		public IdentifierBuilder Add(IConfigurationID? data)
		{
			return Add(data?.ConfigurationID);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IdentifierBuilder Add(string? data)
		{
			_sb.Value
				.Append('.')
				.Append(data)
				;
			return this;
		}

		public IdentifierBuilder Add(bool data)
		{
			_sb.Value
				.Append('.')
				.Append(data ? '1' : '0')
				;
			return this;
		}

		public IdentifierBuilder Add(object? data)
		{
			return Add(GetObjectID(data));
		}

		public IdentifierBuilder Add(Delegate? data)
		{
			return Add(GetObjectID(data?.Method));
		}

		public IdentifierBuilder Add(Type? data)
		{
			return Add(GetObjectID(data));
		}

		public IdentifierBuilder Add(int? data)
		{
			return Add(data == null ? string.Empty : GetIntID(data.Value));
		}

		public IdentifierBuilder Add(string format, object? data)
		{
			_sb.Value
				.Append('.')
				.AppendFormat(CultureInfo.InvariantCulture, format, data)
				;
			return this;
		}

		public IdentifierBuilder AddRange(IEnumerable? items)
		{
			if (items == null)
				Add(string.Empty);
			else
			{
				foreach (var item in items)
					Add(GetObjectID(item));
			}

			return this;
		}

		public IdentifierBuilder AddTypes(IEnumerable? items)
		{
			if (items == null)
				Add(string.Empty);
			else
				foreach (var item in items)
					Add(GetObjectID(item?.GetType()));

			return this;
		}

		static          int                              _identifierCounter;
		static readonly ConcurrentDictionary<string,int> _identifiers = new ();

		public int CreateID()
		{
			var key = _sb.Value.ToString();
			var id  = _identifiers.GetOrAdd(key, static _ => CreateNextID());

#if DEBUG
			System.Diagnostics.Debug.WriteLine(FormattableString.Invariant($"CreateID => ({id}) : '{key}'"));
#endif

			return id;
		}

		public static int CreateNextID() => Interlocked.Increment(ref _identifierCounter);

		static          int                               _typeCounter;
		static readonly ConcurrentDictionary<Type,string> _types = new ();

		public static string GetObjectID(Type? obj)
		{
			return obj == null ? string.Empty : _types.GetOrAdd(obj, static _ => Interlocked.Increment(ref _typeCounter).ToString(NumberFormatInfo.InvariantInfo));
		}

		static          int                              _expressionCounter;
		static readonly ConcurrentDictionary<string,int> _expressions = new ();

		public static int GetObjectID(Expression? ex)
		{
			if (ex == null)
				return 0;

			var key = ex.GetDebugView();

			return _expressions.GetOrAdd(key, static _ => Interlocked.Increment(ref _expressionCounter));
		}

		static          int                                  _methodCounter;
		static readonly ConcurrentDictionary<MethodInfo,int> _methods = new ();

		public static string GetObjectID(MethodInfo? m)
		{
			return GetIntID(m == null ? 0 : _methods.GetOrAdd(m, static _ => Interlocked.Increment(ref _methodCounter)));
		}

		static          int                                 _objectCounter;
		static readonly ConcurrentDictionary<object,string> _objects = new ();

		public static string GetObjectID(object? obj)
		{
			return obj switch
			{
				IConfigurationID c => c.ConfigurationID.ToString(NumberFormatInfo.InvariantInfo),
				Type t             => GetObjectID(t),
				Delegate d         => GetObjectID(d.Method),
				int  i             => GetIntID(i),
				null               => string.Empty,
				string str         => str,
				IEnumerable col    => GetIEnumerableID(col),
				Expression ex      => GetObjectID(ex).ToString(NumberFormatInfo.InvariantInfo),
				TimeSpan ts        => ts.Ticks.ToString(NumberFormatInfo.InvariantInfo),
				_                  => GetOrAddObject(obj),
			};

			static string GetIEnumerableID(IEnumerable col)
			{
				var type = col.GetType().GetListItemType();

				using var sb = Pools.StringBuilder.Allocate();

				sb.Value
					.Append('[')
					.Append(GetObjectID(type))
					.Append('.')
					;

				var len = sb.Value.Length;

				foreach (var item in col)
				{
					if (item != null)
					{
						var t = item.GetType();

						if (t != type)
						{
							sb.Value
								.Append(GetObjectID(t))
								.Append(':');
						}

						sb.Value
							.Append(GetObjectID(item))
							.Append(',');
					}
				}

				if (sb.Value.Length > len)
					sb.Value.Length--;

				sb.Value.Append(']');

				return sb.Value.ToString();
			}

			static string GetOrAddObject(object o)
			{
				try
				{
					return _objects.GetOrAdd(o, static _ => Interlocked.Increment(ref _objectCounter).ToString(NumberFormatInfo.InvariantInfo));
				}
				catch (InvalidOperationException)
				{
					if (o is IComparable c)
						lock (_buggyObjects)
						{
							var id = _buggyObjects.Find(bo => c.CompareTo(bo.obj) == 0);

							if (id.obj == null)
							{
								id = (o, new ());
								_buggyObjects.Add(id);
							}

							return GetOrAddObject(id.id);
						}

					throw;
				}
			}
		}

		static readonly List<(object? obj,object id)> _buggyObjects = new ();
		static readonly string?[]                     _intToString  = new string?[300];

		static string GetIntID(int id)
		{
			if (id >= 0 && id < _intToString.Length)
			{
				var value = _intToString[id];
				if (value == null)
					_intToString[id] = value = id.ToString(NumberFormatInfo.InvariantInfo);
				return value;
			}

			return id.ToString(NumberFormatInfo.InvariantInfo);
		}

		public void Dispose()
		{
			_sb.Dispose();
		}
	}
}
