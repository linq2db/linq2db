using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	public static class AstExtensions
	{
		public static IEnumerable<TGroup> EnumerateMemberGroups<TGroup>(this IEnumerable<IMemberGroup> groups)
			where TGroup : IMemberGroup
		{
			foreach (var group in groups)
			{
				if (group is RegionGroup regionGroup)
				{
					foreach (var region in regionGroup.Members)
					{
						foreach (var memberGroup in region.Members.EnumerateMemberGroups<TGroup>())
						{
							yield return memberGroup;
						}
					}
				}
				else if (group is TGroup memberGroup)
					yield return memberGroup;
			}
		}

		public static IEnumerable<TElement> EnumerateMembers<TGroup, TElement>(this IEnumerable<IMemberGroup> groups)
			where TGroup : MemberGroup<TElement>
			where TElement : IGroupElement
		{
			foreach (var group in groups.EnumerateMemberGroups<TGroup>())
				foreach (var element in group.Members)
					yield return element;
		}
	}
}
