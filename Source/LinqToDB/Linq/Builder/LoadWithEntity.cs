using System.Collections.Generic;
using System.Diagnostics;

namespace LinqToDB.Linq.Builder
{
	[DebuggerDisplay("{ToDebugString()}")]
	sealed class LoadWithEntity
	{
		public LoadWithEntity? Parent     { get; set; }

		public List<LoadWithMember>? MembersToLoad { get; set; }

		public IEnumerable<string> ToDebugStrings()
		{
			if (MembersToLoad == null || MembersToLoad.Count == 0)
				yield break;

			foreach (var info in MembersToLoad)
			{
				var memberInfoStr = info.ToDebugString();

				if (info.Entity is { MembersToLoad.Count: > 0 })
				{
					foreach (var subItem in info.Entity.ToDebugStrings())
					{
						yield return $"{memberInfoStr}.{subItem}";
					}
				}
				else
				{
					yield return memberInfoStr;
				}
			}
		}

		public string ToDebugString()
		{
			var str = string.Join(", ", ToDebugStrings());
			if (str is null or "")
				return "[empty]";
			return str;
		}
	}
}
