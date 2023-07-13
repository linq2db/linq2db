using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace LinqToDB.Common.Internal
{
	using Expressions;
	using Extensions;

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

		static readonly ConcurrentDictionary<MemberInfo, Info> _cache           = new();
		static readonly Info                                   _defaultInfo     = new();
		static readonly Info                                   _isQueryableInfo = new() { IsQueryable = true };

		public static Info GetMemberInfo(MemberInfo member)
		{
			if (member is MethodInfo { IsGenericMethod: true } mi)
			{
				var gm = mi.GetGenericMethodDefinitionCached();

				return _cache.GetOrAdd(gm, static m =>
				{
					var attrs = m.GetAttribute<IsQueryableAttribute>();

					return attrs != null ? _isQueryableInfo : _defaultInfo;
				});
			}

			return _defaultInfo;
		}

		public class Info
		{
			public bool IsQueryable { get; init; }
		}
	}
}
