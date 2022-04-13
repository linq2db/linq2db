using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using LinqToDB.Expressions;

namespace LinqToDB.Common.Internal
{
	class IdentifierBuilder
	{
		public IdentifierBuilder()
		{
		}

		public IdentifierBuilder(object? data)
		{
			Add(data);
		}

		readonly StringBuilder _stringBuilder = new ();

		public IdentifierBuilder Add(string? data)
		{
			_stringBuilder
				.Append('.')
				.Append(data)
				;
			return this;
		}

		public IdentifierBuilder Add(object? data)
		{
			_stringBuilder
				.Append('.')
				.Append(data)
				;
			return this;
		}

		static          int                              _identifierCounter;
		static readonly ConcurrentDictionary<string,int> _identifiers = new ();

		public int CreateID()
		{
			var key = _stringBuilder.ToString();
			var id  = _identifiers.GetOrAdd(key, static _ => CreateNextID());

#if DEBUG
			Debug.WriteLine($"CreateID: '{key}' ({id})");
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

		static          int                                 _objectCounter;
		static readonly ConcurrentDictionary<object,string> _objects = new ();

		public static string GetObjectID(object? obj)
		{
			return obj switch
			{
				Type t => GetObjectID(t),
				null   => string.Empty,
				_      => _objects.GetOrAdd(obj, static _ => Interlocked.Increment(ref _objectCounter).ToString())
			};
		}
	}
}
