using System;
using System.Collections.Concurrent;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Extensions;

namespace LinqToDB.Common.Internal
{
	static class MemberCache
	{
		static MemberCache()
		{
			//Query.CacheCleaners.Enqueue(ClearCache);
		}

		public static void ClearCache()
		{
			_cache.Clear();
		}

		static readonly ConcurrentDictionary<MemberInfo,Info> _cache   = new();
		static readonly Info                                  _default = new();

		public static Info GetMemberInfo(MemberInfo member)
		{
			if (member is MethodInfo { IsGenericMethod: true } mi)
			{
				var gm = mi.GetGenericMethodDefinitionCached();

				return _cache.GetOrAdd(gm, static m =>
				{
					var attrs = m.GetAttribute<IsQueryableAttribute>();

					return attrs != null ? new() { IsQueryable = true } : _default;
				});
			}

			return _default;
		}

		public class Info
		{
			public bool IsQueryable;
		}
	}
}
