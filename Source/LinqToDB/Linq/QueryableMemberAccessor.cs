using System;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq
{
	class QueryableMemberAccessor
	{
		public Expression                  Expression = null!;
		public Func<MemberInfo, IDataContext, Expression> Accessor   = null!;
	}
}
