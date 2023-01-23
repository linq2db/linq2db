﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;

namespace LinqToDB.Common.Internal
{
	using Expressions;
	using Linq;

	/// <summary>
	/// Internal infrastructure API.
	/// Provides functionality for <see cref="IConfigurationID.ConfigurationID"/> generation.
	/// </summary>
	public sealed class IdentifierBuilder : IDisposable
	{
		readonly ObjectPool<StringBuilder>.RentedElement _pooledElement;
		StringBuilder? _stringBuilder;

		public IdentifierBuilder()
		{
			_pooledElement = Pools.StringBuilder.Allocate();
			_stringBuilder = _pooledElement.Value;
		}

		public IdentifierBuilder(object? data)
		{
			_pooledElement = Pools.StringBuilder.Allocate();
			_stringBuilder = _pooledElement.Value;

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
			_stringBuilder!
				.Append('.')
				.Append(data?.ConfigurationID)
				;
			return this;
		}

		public IdentifierBuilder Add(string? data)
		{
			_stringBuilder!
				.Append('.')
				.Append(data)
				;
			return this;
		}

		public IdentifierBuilder Add(bool data)
		{
			_stringBuilder!
				.Append('.')
				.Append(data ? "1" : "0")
				;
			return this;
		}

		public IdentifierBuilder Add(object? data)
		{
			_stringBuilder!
				.Append('.')
				.Append(GetObjectID(data))
				;
			return this;
		}

		public IdentifierBuilder Add(Delegate? data)
		{
			_stringBuilder!
				.Append('.')
				.Append(data?.Method)
				;
			return this;
		}

		public IdentifierBuilder Add(int? data)
		{
			_stringBuilder!
				.Append('.')
				.Append(data == null ? string.Empty : GetIntID(data.Value))
				;
			return this;
		}

		public IdentifierBuilder Add(string format, object? data)
		{
			_stringBuilder!
				.Append('.')
				.AppendFormat(format, data)
				;
			return this;
		}

		public IdentifierBuilder AddRange(IEnumerable items)
		{
			foreach (var item in items)
				Add(GetObjectID(item));
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
			var key = _stringBuilder!.ToString();
			var id  = _identifiers.GetOrAdd(key, static _ => CreateNextID());

#if DEBUG
			Debug.WriteLine($"CreateID => ({id}) : '{key}'");
#endif

			return id;
		}

		public static int CreateNextID() => Interlocked.Increment(ref _identifierCounter);

		static          int                               _typeCounter;
		static readonly ConcurrentDictionary<Type,string> _types = new ();

		public static string GetObjectID(Type? obj)
		{
			return obj == null ? string.Empty : _types.GetOrAdd(obj, static _ => Interlocked.Increment(ref _typeCounter).ToString());
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
				IConfigurationID c => c.ConfigurationID.ToString(),
				Type t             => GetObjectID(t),
				Delegate d         => GetObjectID(d.Method),
				int  i             => GetIntID(i),
				null               => string.Empty,
				string str         => str,
				IEnumerable col    => $"[{string.Join(",", col.Cast<object?>().Select(GetObjectID))}]",
				Expression ex      => GetObjectID(ex).ToString(),
				TimeSpan ts        => ts.Ticks.ToString(),
				_                  => GetOrAddObject(obj)
			};

			static string GetOrAddObject(object o)
			{
				try
				{
					return _objects.GetOrAdd(o, static _ => Interlocked.Increment(ref _objectCounter).ToString());
				}
				catch (InvalidOperationException)
				{
					if (o is IComparable c)
						lock (_buggyObjects)
						{
							var id = _buggyObjects.FirstOrDefault(bo => c.CompareTo(bo.obj) == 0);

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
					_intToString[id] = value = id.ToString();
				return value;
			}

			return id.ToString();
		}

		private void Dispose()
		{
			if (_stringBuilder != null)
			{
				_pooledElement.Dispose();
				_stringBuilder = null;
			}
		}

		~IdentifierBuilder()
		{
			Dispose();
		}

		void IDisposable.Dispose()
		{
			Dispose();
			GC.SuppressFinalize(this);
		}
	}
}
