using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB;
using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.Linq.Builder
{
	[DebuggerDisplay("{ToDebugString()}")]
	sealed class LoadWithInfo
	{
		public LoadWithInfo()
		{
		}

		public LoadWithInfo(MemberInfo memberInfo, bool shouldLoad)
		{
			MemberInfo = memberInfo;
			ShouldLoad = shouldLoad;
		}

		public MemberInfo?       MemberInfo   { get; }
		public LambdaExpression? MemberFilter { get; set; }
		public Expression?       FilterFunc   { get; set; }
		public bool              ShouldLoad   { get; set; }

		public List<LoadWithInfo>? NextInfos { get; set; }

		private bool Equals(LoadWithInfo other)
		{
			return Equals(MemberInfo, other.MemberInfo);
		}

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((LoadWithInfo)obj);
		}

		public override int GetHashCode()
		{
			return MemberInfo?.GetHashCode() ?? 0;
		}

		IEnumerable<MemberInfo[]> EnumerateChains(MemberInfo[] currentPath, List<LoadWithInfo> items)
		{
			foreach (var item in items)
			{
				if (item.MemberInfo == null)
					continue;

				var nextPath = currentPath.Concat(new MemberInfo[] { item.MemberInfo }).ToArray();

				if (item.NextInfos?.Count > 0)
				{
					foreach (var si in EnumerateChains(nextPath, item.NextInfos))
						yield return si;
				}
				else
				{
					yield return nextPath;
				}
			}
		}

		public string ToDebugString()
		{
			if (NextInfos == null || NextInfos.Count == 0)
			{
				if (MemberInfo == null)
					return "[empty]";
				return $"[{MemberInfo.Name}]";
			}

			using var sb = Pools.StringBuilder.Allocate();
			var currentPath = MemberInfo != null
				? new[] { MemberInfo }
				: [];

			foreach (var info in EnumerateChains(currentPath, NextInfos))
			{
				sb.Value.Append(string.Join(".", info.Select(mi => mi.Name)))
					.Append(", ");
			}

			sb.Value.Length -= 2;

			return sb.Value.ToString();
		}
	}
}
