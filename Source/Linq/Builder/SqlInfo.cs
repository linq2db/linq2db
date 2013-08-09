using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using SqlBuilder;

	public class SqlInfo
	{
		public ISqlExpression   Sql;
		public SelectQuery      Query;
		public int              Index = -1;
		public readonly List<MemberInfo> Members = new List<MemberInfo>();

		public SqlInfo()
		{
		}

		public SqlInfo(MemberInfo mi)
		{
			Members.Add(mi);
		}

		public SqlInfo(IEnumerable<MemberInfo> mi)
		{
			Members.AddRange(mi);
		}

		public SqlInfo Clone()
		{
			return new SqlInfo(Members) { Sql = Sql, Query = Query, Index = Index };
		}

		public SqlInfo Clone(MemberInfo mi)
		{
			var info = Clone();

			if (Members.Count == 0 || Members[0] != mi)
				info.Members.Insert(0, mi);

			return info;
		}

		public bool CompareMembers(SqlInfo info)
		{
			return Members.Count == info.Members.Count && !Members.Where((t,i) => !t.EqualsTo(info.Members[i])).Any();
		}

		public bool CompareLastMember(SqlInfo info)
		{
			return
				Members.Count > 0 && info.Members.Count > 0 &&
				Members[Members.Count - 1].EqualsTo(info.Members[info.Members.Count - 1]);
		}
	}
}
