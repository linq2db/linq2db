using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Reflection;

	internal partial class ExpressionBuilder
	{
		public List<LoadWithInfo> GetTableLoadWith(ITableContext table)
		{
			var loadWith = table.LoadWithRoot;
			if (table.LoadWithPath != null)
			{
				foreach (var memberInfo in table.LoadWithPath)
				{
					var found = loadWith.NextInfos?.FirstOrDefault(li =>
						MemberInfoEqualityComparer.Default.Equals(li.MemberInfo, memberInfo));

					found ??= loadWith.NextInfos?.FirstOrDefault(li => li.MemberInfo?.Name == memberInfo.Name);

					if (found != null)
					{
						loadWith = found;
					}
					else
					{
						loadWith.NextInfos ??= new();
						var newInfo = new LoadWithInfo(memberInfo, false);
						loadWith.NextInfos.Add(newInfo);

						loadWith = newInfo;
					}
				}

				if (loadWith.NextInfos != null)
					return loadWith.NextInfos;
			}

			loadWith.NextInfos ??= new();

			// ToList() is important here
			return loadWith.NextInfos.ToList();
		}
	}
}
