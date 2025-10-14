using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Internal.Linq
{
	[DebuggerDisplay("Member: {MemberInfo.Name}")]
	sealed class AccessorMember
	{
		public AccessorMember(Expression expression)
		{
			if (expression is MethodCallExpression mc)
			{
				MemberInfo = mc.Method;
				Arguments  = mc.Arguments;
			}
			else if (expression is MemberExpression ma)
			{
				MemberInfo = ma.Member;
			}
			else
			{
				throw new InvalidOperationException($"Expression '{expression}' cannot be used in association.");
			}
		}

		public MemberInfo MemberInfo { get; }
		public ReadOnlyCollection<Expression>? Arguments { get; }

		bool Equals(AccessorMember other)
		{
			if (!MemberInfo.Equals(other.MemberInfo))
				return false;

			if (Arguments == other.Arguments)
				return true;

			if (Arguments == null || other.Arguments == null || Arguments.Count != other.Arguments.Count)
				return false;

			for (int i = 0; i < Arguments.Count; i++)
			{
				if (!Arguments[i].Equals(other.Arguments[i]))
					return false;
			}

			return true;
		}

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((AccessorMember)obj);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(MemberInfo, Arguments);
		}
	}
}
