using System.Collections.Generic;
using System.Diagnostics;

namespace LinqToDB.Internal.Linq.Builder
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

		bool Equals(LoadWithEntity other)
		{
			if (MembersToLoad == null && other.MembersToLoad == null)
				return true;

			if (MembersToLoad == null || other.MembersToLoad == null)
				return false;

			if (MembersToLoad?.Count != other.MembersToLoad?.Count)
				return false;

			for(var i = 0; i < MembersToLoad!.Count; i++)
			{
				if (!MembersToLoad[i].Equals(other.MembersToLoad![i]))
					return false;
			}

			return true;
		}

		public override bool Equals(object? obj)
		{
			return ReferenceEquals(this, obj) || obj is LoadWithEntity other && Equals(other);
		}

		public override int GetHashCode()
		{
			return 0;
		}

		public string ToDebugString()
		{
			var str = string.Join(", ", ToDebugStrings());
			return str switch
			{
				null or "" => "[empty]",
				_ => str,
			};
		}
	}
}
