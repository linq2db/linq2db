using System.Collections.Concurrent;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Linq;

namespace LinqToDB.Common.Internal
{
	static class MemberCache
	{
		static MemberCache()
		{
			Query.CacheCleaners.Enqueue(ClearCache);
		}

		public static void ClearCache()
		{
			_cache.Clear();
		}

		static readonly ConcurrentDictionary<MemberInfo,Info> _cache           = new();
		static readonly Info                                  _defaultInfo     = new();
		static readonly Info                                  _isQueryableInfo = new() { IsQueryable = true };

		public static Info GetMemberInfo(MemberInfo member)
		{
			if (member is MethodInfo { IsGenericMethod: true } mi)
			{
				return _cache.GetOrAdd(
					mi.GetGenericMethodDefinitionCached(),
					static m => m.HasAttribute<IsQueryableAttribute>() ? _isQueryableInfo : _defaultInfo);
			}

			return _defaultInfo;
		}

		public sealed class Info
		{
			public bool IsQueryable { get; init; }
		}
	}
}
