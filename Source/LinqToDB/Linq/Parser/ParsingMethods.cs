using System.Linq;
using System.Reflection;
using LinqToDB.Expressions;

namespace LinqToDB.Linq.Parser
{
	public static class ParsingMethods
	{
		public static readonly MethodInfo Where                = MemberHelper.MethodOf<IQueryable<object>>(q => q.Where(_ => true)).GetGenericMethodDefinition();
		public static readonly MethodInfo Select               = MemberHelper.MethodOf<IQueryable<object>>(q => q.Select(_ => true)).GetGenericMethodDefinition();
		public static readonly MethodInfo Take                 = MemberHelper.MethodOf<IQueryable<object>>(q => q.Take(1)).GetGenericMethodDefinition();
		public static readonly MethodInfo Skip                 = MemberHelper.MethodOf<IQueryable<object>>(q => q.Skip(1)).GetGenericMethodDefinition();
		public static readonly MethodInfo Any                  = MemberHelper.MethodOf<IQueryable<object>>(q => q.Any()).GetGenericMethodDefinition();
		public static readonly MethodInfo AnyPredicate         = MemberHelper.MethodOf<IQueryable<object>>(q => q.Any(_ => true)).GetGenericMethodDefinition();
		public static readonly MethodInfo SelectMany           = MemberHelper.MethodOf<IQueryable<object>>(q => q.SelectMany(_ => q)).GetGenericMethodDefinition();
		public static readonly MethodInfo SelectManyProjection = MemberHelper.MethodOf<IQueryable<object>>(q => q.SelectMany(_ => q, (o, o1) => o)).GetGenericMethodDefinition();
		public static readonly MethodInfo Union                = MemberHelper.MethodOf<IQueryable<object>>(q => q.Union(q)).GetGenericMethodDefinition();
		public static readonly MethodInfo Concat               = MemberHelper.MethodOf<IQueryable<object>>(q => q.Concat(q)).GetGenericMethodDefinition();
		public static readonly MethodInfo Join                 = MemberHelper.MethodOf<IQueryable<object>>(q => q.Join(q, _ => 1, _ => 2, (q1, q2) => 0)).GetGenericMethodDefinition();
		public static readonly MethodInfo GroupBy              = MemberHelper.MethodOf<IQueryable<object>>(q => q.GroupBy(_ => 1, _ => 2, (q1, q2) => 0)).GetGenericMethodDefinition();

		public static readonly MethodInfo GetTable             = MemberHelper.MethodOf<IDataContext>(dc => dc.GetTable<object>()).GetGenericMethodDefinition();
	}
}
