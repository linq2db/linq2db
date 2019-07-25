using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB;
using LinqToDB.Expressions;

namespace Tests.Playground
{
	public class EagerLoadingProbes
	{
		private static readonly MethodInfo[] _tupleConstructors = 
		{
			MemberHelper.MethodOf(() => Tuple.Create(0, 0)).GetGenericMethodDefinition(),
			MemberHelper.MethodOf(() => Tuple.Create(0, 0, 0)).GetGenericMethodDefinition(),
			MemberHelper.MethodOf(() => Tuple.Create(0, 0, 0, 0)).GetGenericMethodDefinition(),
			MemberHelper.MethodOf(() => Tuple.Create(0, 0, 0, 0, 0)).GetGenericMethodDefinition(),
			MemberHelper.MethodOf(() => Tuple.Create(0, 0, 0, 0, 0, 0)).GetGenericMethodDefinition(),
			MemberHelper.MethodOf(() => Tuple.Create(0, 0, 0, 0, 0, 0, 0)).GetGenericMethodDefinition(),
			MemberHelper.MethodOf(() => Tuple.Create(0, 0, 0, 0, 0, 0, 0, 0)).GetGenericMethodDefinition(),
		};

		private static readonly MethodInfo _queryWithDetailsInternalMethodInfo = MemberHelper.MethodOf(() =>
			QueryWithDetailsInternal<int, int, int>((IQueryable<int>)null, null, null, null)).GetGenericMethodDefinition();

		class EagerLoadingContext<T, TKey>
		{
			private Dictionary<TKey, List<T>> _items;

			public void Add(TKey key, T item)
			{
				List<T> list;
				if (_items == null)
				{
					_items = new Dictionary<TKey, List<T>>();
					list = new List<T>();
					_items.Add(key, list);
				}
				else if (!_items.TryGetValue(key, out list))
				{
					list = new List<T>();
					_items.Add(key, list);
				}
				list.Add(item);
			}

			public List<T> GetList(TKey key)
			{
				if (_items == null || !_items.TryGetValue(key, out var list))
					return new List<T>();
				return list;
			}
		}

		private static Expression GenerateKeyExpression(Expression[] members, int startIndex)
		{
			var count = members.Length - startIndex;
			if (count == 0)
				throw new ArgumentException();

			if (count == 1)
				return members[startIndex];

			Expression[] arguments;

			if (count > 8)
			{
				count = 8;
				arguments = new Expression[count];
				Array.Copy(members, startIndex, arguments, 0, count - 1);
				arguments[count - 1] = GenerateKeyExpression(members, startIndex + count);
			}
			else
			{
				arguments = new Expression[count];
				Array.Copy(members, startIndex, arguments, 0, count);
			}

			var constructor = _tupleConstructors[count - 2];

			var typedConstructorPlain = constructor.MakeGenericMethod(arguments.Select(a => a.Type).ToArray());

			return Expression.Call(typedConstructorPlain, arguments);
		}

		public static List<T> QueryWithDetails<T, TD>(
			IDataContext dc,
			IQueryable<T> mainQuery,
			Expression<Func<T, IEnumerable<TD>>> detailQueryExpression,
			Action<T, List<TD>> detailSetter)
		{
			var ed = dc.MappingSchema.GetEntityDescriptor(typeof(T));
			var keys = ed.Columns.Where(c => c.IsPrimaryKey).ToArray();
			if (keys.Length == 0) 
				keys = ed.Columns.Where(c => dc.MappingSchema.IsScalarType(c.MemberType)).ToArray();

			if (keys.Length == 0)
				throw new LinqToDBException($"Can not retrieve key fro type '{typeof(T).Name}'");

			var objParam = Expression.Parameter(typeof(T), "obj");
			var properties = keys.Select(k => (Expression)Expression.MakeMemberAccess(objParam, k.MemberInfo))
				.ToArray();
			var getKeyExpr = Expression.Lambda(GenerateKeyExpression(properties, 0), objParam);

			var method = _queryWithDetailsInternalMethodInfo.MakeGenericMethod(new[] { typeof(T), typeof(TD), getKeyExpr.Body.Type });

			var result = method.Invoke(null, new object[] { mainQuery, detailQueryExpression, getKeyExpr, detailSetter });
			return (List<T>)result;
		}

		private static List<T> QueryWithDetailsInternal<T, TD, TKey>(
			IQueryable<T> mainQuery, 
			Expression<Func<T, IEnumerable<TD>>> detailQueryExpression,
			Expression<Func<T, TKey>> getKeyExpression,
			Action<T, List<TD>> detailSetter)
		{
			var detailQuery = mainQuery.SelectMany(detailQueryExpression, (m, d) => new { MasterKey = getKeyExpression.Compile().Invoke(m), Detail = d});
			var detailsWithKey = detailQuery.ToList();
			var detailDictionary = new Dictionary<TKey, List<TD>>();

			foreach (var d in detailsWithKey)
			{
				if (!detailDictionary.TryGetValue(d.MasterKey, out var list))
				{
					list = new List<TD>();
					detailDictionary.Add(d.MasterKey, list);
				}
				list.Add(d.Detail);
			}

			var mainEntities = mainQuery.ToList();
			var getKeyFunc = getKeyExpression.Compile();

			foreach (var entity in mainEntities)
			{
				var key = getKeyFunc(entity);
				if (!detailDictionary.TryGetValue(key, out var ds))
					ds = new List<TD>();
				detailSetter(entity, ds);
			}

			return mainEntities;
		}
	}
}
