using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using SqlQuery;

	public class SqlInfo
	{
		public ISqlExpression   Sql;
		public SelectQuery      Query;
		public int              Index = -1;
		public readonly List<MemberInfo> MemberChain = new List<MemberInfo>();

		public SqlInfo()
		{
		}

		public SqlInfo(MemberInfo mi)
		{
			MemberChain.Add(mi);
		}

		public SqlInfo(IEnumerable<MemberInfo> mi)
		{
			MemberChain.AddRange(mi);
		}

		public SqlInfo Clone()
		{
			return new SqlInfo(MemberChain) { Sql = Sql, Query = Query, Index = Index };
		}

		public SqlInfo Clone(MemberInfo mi)
		{
			var info = Clone();

			if (MemberChain.Count == 0 || MemberChain[0] != mi)
				info.MemberChain.Insert(0, mi);

			return info;
		}

		public bool CompareMembers(SqlInfo info)
		{
			return MemberChain.Count == info.MemberChain.Count && !MemberChain.Where((t,i) => !t.EqualsTo(info.MemberChain[i])).Any();
		}

		public bool CompareLastMember(SqlInfo info)
		{
			return
				MemberChain.Count > 0 && info.MemberChain.Count > 0 &&
				MemberChain[MemberChain.Count - 1].EqualsTo(info.MemberChain[info.MemberChain.Count - 1]);
		}
	}
}
