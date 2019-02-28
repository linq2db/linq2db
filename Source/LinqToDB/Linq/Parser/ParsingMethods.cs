using System.Linq;
using System.Reflection;
using LinqToDB.Expressions;

namespace LinqToDB.Linq.Parser
{
	public static class ParsingMethods
	{
		public static readonly MethodInfo WhereMethod       = MemberHelper.MethodOf<IQueryable<object>>(q => q.Where(_ => true)).GetGenericMethodDefinition();
		public static readonly MethodInfo SelectMethod      = MemberHelper.MethodOf<IQueryable<object>>(q => q.Select(_ => true)).GetGenericMethodDefinition();
		public static readonly MethodInfo TakeMethod        = MemberHelper.MethodOf<IQueryable<object>>(q => q.Take(1)).GetGenericMethodDefinition();
		public static readonly MethodInfo SkipMethod        = MemberHelper.MethodOf<IQueryable<object>>(q => q.Skip(1)).GetGenericMethodDefinition();
		public static readonly MethodInfo SelectManyMethod1 = MemberHelper.MethodOf<IQueryable<object>>(q => q.SelectMany(_ => q)).GetGenericMethodDefinition();
		public static readonly MethodInfo SelectManyMethod2 = MemberHelper.MethodOf<IQueryable<object>>(q => q.SelectMany(_ => q, (o, o1) => o)).GetGenericMethodDefinition();
		public static readonly MethodInfo UnionMethod       = MemberHelper.MethodOf<IQueryable<object>>(q => q.Union(q)).GetGenericMethodDefinition();
		public static readonly MethodInfo JoinMethod        = MemberHelper.MethodOf<IQueryable<object>>(q => q.Join(q, _ => 1, _ => 2, (q1, q2) => 0)).GetGenericMethodDefinition();

		public static readonly MethodInfo GetTableMethod    = MemberHelper.MethodOf<IDataContext>(dc => dc.GetTable<object>()).GetGenericMethodDefinition();
	}
}
