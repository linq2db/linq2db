using System.Reflection;
using LinqToDB.Expressions;
using LinqToDB.Linq.Parser.Builders;

namespace LinqToDB.Linq.Relinq
{
	public static class ReflectionMethods
	{
		public static readonly MethodInfo GetTable = MemberHelper.MethodOf<IDataContext>(dc => dc.GetTable<object>()).GetGenericMethodDefinition();
	}
}
