using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LinqToDB.Common;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using SqlQuery;

	public class SqlInfo
	{
		public readonly ISqlExpression   Sql;
		public readonly SelectQuery?     Query;
		public readonly int              Index;

		public readonly MemberInfo[]     MemberChain;

		public SqlInfo(MemberInfo[] mi, ISqlExpression sql, SelectQuery? query = null, int index = -1)
		{
			MemberChain = mi;
			Sql   = sql;
			Query = query;
			Index = index;
		}

		public SqlInfo(ISqlExpression sql, SelectQuery? query = null, int index = -1): this(Array<MemberInfo>.Empty, sql, query, index)
		{
		}

		public SqlInfo(MemberInfo mi, ISqlExpression sql, SelectQuery? query = null, int index = -1) : this(new[] { mi }, sql, query, index)
		{
		}

		public SqlInfo(IEnumerable<MemberInfo> mi, ISqlExpression sql, SelectQuery? query = null, int index = -1) : this(mi.ToArray(), sql, query, index)
		{
		}

		public SqlInfo(IEnumerable<MemberInfo> mi, ISqlExpression sql, int index) : this(mi, sql, null, index)
		{
		}


		//TODO: possibly remove and update usages
		public SqlInfo Clone(MemberInfo mi)
		{
			if (MemberChain.Length == 0 || MemberChain[0] != mi)
				return new SqlInfo(new [] {mi}.Concat(MemberChain), Sql, Query, Index);

			return this;
		}

		public bool CompareMembers(SqlInfo info)
		{
			return MemberChain.Length == info.MemberChain.Length &&
			       !MemberChain.Where((t, i) => !t.EqualsTo(info.MemberChain[i])).Any();
		}

		public bool CompareLastMember(SqlInfo info)
		{
			return
				MemberChain.Length > 0 && info.MemberChain.Length > 0 &&
				MemberChain[MemberChain.Length - 1].EqualsTo(info.MemberChain[info.MemberChain.Length - 1]);
		}

		public SqlInfo AppendMember(MemberInfo mi)
		{
			if (MemberChain.Length == 0)
				return WithMember(mi);

			return WithMembers(Array<MemberInfo>.Append(MemberChain, mi));
		}

		public SqlInfo WithMembers(IEnumerable<MemberInfo> mi)
		{
			return new SqlInfo(mi, Sql, Query, Index);
		}

		public SqlInfo WithMember(MemberInfo mi)
		{
			return new SqlInfo(mi, Sql, Query, Index);
		}

		public SqlInfo WithSql(ISqlExpression sql)
		{
			if (Sql == sql)
				return this;
			return new SqlInfo(MemberChain, sql, Query, Index);
		}

		public SqlInfo WithIndex(int index)
		{
			if (index == Index)
				return this;
			return new SqlInfo(MemberChain, Sql, Query, index);
		}

		public SqlInfo WithQuery(SelectQuery? query)
		{
			if (Query == query)
				return this;
			return new SqlInfo(MemberChain, Sql, query, Index);
		}

	}
}
