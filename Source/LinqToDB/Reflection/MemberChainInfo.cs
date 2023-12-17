using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LinqToDB.Reflection
{
	public class MemberChainInfo : MemberInfo
	{
		public MemberChainInfo(MemberInfo[] members)
		{
			if (members.Length == 0)
				throw new ArgumentException($"Empty member chain in {nameof(MemberChainInfo)} ctor");
			this.members = members;
		}
		private readonly MemberInfo[] members;
		private MemberInfo Head => members[0];
		private MemberInfo Tail => members[^1];

		public override Type? DeclaringType => Head.DeclaringType;

		public override MemberTypes MemberType => MemberTypes.Custom;

		public override string Name => string.Join(".", members.Select(m => m.Name));

		public override Type? ReflectedType => Head.ReflectedType;

		public override object[] GetCustomAttributes(bool inherit) => Head.GetCustomAttributes(inherit);

		public override object[] GetCustomAttributes(Type attributeType, bool inherit) => Head.GetCustomAttributes(attributeType, inherit);

		public override bool IsDefined(Type attributeType, bool inherit) => Head.IsDefined(attributeType, inherit);

		public override bool Equals(object? obj)
		{
			return obj is MemberChainInfo info
				&& members.Length == info.members.Length
				&& members.Zip(info.members).All(m => m.First.Equals(m.Second));
		}

		public override int GetHashCode()
		{
			return members.Aggregate(0, (i, m) => i ^ m.GetHashCode());
		}

		public Type ReturnType => Tail.GetMemberType();

	}

	public static class MemberChainInfoHelper
	{
		public static MemberChainInfo GetMemberChainInfo(this Expression expression) => new MemberChainInfo(GetMemberInfoSequence(expression).ToArray());
		private static IEnumerable<MemberInfo> GetMemberInfoSequence(Expression? expression)
		{
			if (expression == null)
				yield break;
			MemberInfo? mem=null;
			Expression? expr = null;
			switch (expression)
			{
				case MemberExpression ma:
					mem = ma.Member;
					expr = ma.Expression;
					break;
				case MethodCallExpression ma:
					mem = ma.Method;
					expr = ma.Object;
					break;
				default:
					yield break;
			}
			foreach (var m in GetMemberInfoSequence(expr))
				yield return m;
			yield return mem;
		}
	}
}
