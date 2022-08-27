using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;

namespace LinqToDB.Common.Internal
{
	using Expressions;
	using Linq;

	class IdentifierBuilder
	{
		public IdentifierBuilder()
		{
		}

		public IdentifierBuilder(object? data)
		{
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

		static          int                                 _objectCounter;
		static readonly ConcurrentDictionary<object,string> _objects = new ();

		public static string GetObjectID(object? obj)
		{
			return obj switch
			{
				Type t => GetObjectID(t),
				null   => string.Empty,
				_      => GetOrAddObject(obj)
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
	}
}
