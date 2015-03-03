using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Mapping
{
	using Common;
	using Extensions;

	public class AssociationDescriptor
	{
		public AssociationDescriptor(
			Type       type,
			MemberInfo memberInfo,
			string[]   thisKey,
			string[]   otherKey,
			string     storage,
			bool       canBeNull,
			string     joinCondition)
		{
			if (memberInfo == null) throw new ArgumentNullException("memberInfo");

			if (joinCondition == null)
			{
				if (thisKey    == null) throw new ArgumentNullException("thisKey");
				if (otherKey   == null) throw new ArgumentNullException("otherKey");

				if (thisKey.Length == 0)
					throw new ArgumentOutOfRangeException(
						"thisKey",
						string.Format("Association '{0}.{1}' does not define keys.", type.Name, memberInfo.Name));

				if (thisKey.Length != otherKey.Length)
					throw new ArgumentException(
						string.Format(
							"Association '{0}.{1}' has different number of keys for parent and child objects.",
							type.Name, memberInfo.Name));
			}

			Type          = type;
			MemberInfo    = memberInfo;
			ThisKey       = thisKey;
			OtherKey      = otherKey;
			Storage       = storage;
			CanBeNull     = canBeNull;
			JoinCondition = joinCondition;
		}

		public Type       Type          { get; set; }
		public MemberInfo MemberInfo    { get; set; }
		public string[]   ThisKey       { get; set; }
		public string[]   OtherKey      { get; set; }
		public string     Storage       { get; set; }
		public bool       CanBeNull     { get; set; }
		public string     JoinCondition { get; set; }

		public static string[] ParseKeys(string keys)
		{
			return keys == null ? Array<string>.Empty : keys.Replace(" ", "").Split(',');
		}

		LambdaExpression _joinCondition;

		public LambdaExpression GetJoinCondition()
		{
			if (_joinCondition != null || JoinCondition == null)
				return _joinCondition;

			var expr = Expression.Call(Type, JoinCondition, Array<Type>.Empty);
			var call = Expression.Lambda<Func<LambdaExpression>>(Expression.Convert(expr, typeof (LambdaExpression)));
			var l    = call.Compile()();

			var memberType = MemberInfo.GetMemberType();
			var isList     = typeof(IEnumerable).IsSameOrParentOf(memberType) && memberType.IsGenericTypeEx();

			if (isList)
				memberType = memberType.GetGenericArguments()[0];

			if (l.Parameters.Count != 2 || l.Parameters[0].Type != Type || l.Parameters[1].Type != memberType)
			{
				throw new ArgumentException(
					string.Format(
						"Association '{0}.{1}': expected a name of static method returning 'Expression<Func<{0},{2},bool>>'.",
						Type, MemberInfo.Name, memberType));
			}

			return _joinCondition = l;
		}
	}
}
