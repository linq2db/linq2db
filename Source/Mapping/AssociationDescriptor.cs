using System;
using System.Linq.Expressions;
using System.Reflection;

using JNotNull = JetBrains.Annotations.NotNullAttribute;

namespace LinqToDB.Mapping
{
	using Common;
	using Extensions;

	public class AssociationDescriptor
	{
		public AssociationDescriptor(
			[JNotNull] Type       type,
			[JNotNull] MemberInfo memberInfo,
			[JNotNull] string[]   thisKey,
			[JNotNull] string[]   otherKey,
			           string     expressionPredicate,
			           string     storage,
			           bool       canBeNull)
		{
			if (memberInfo == null) throw new ArgumentNullException("memberInfo");
			if (thisKey    == null) throw new ArgumentNullException("thisKey");
			if (otherKey   == null) throw new ArgumentNullException("otherKey");

			if (thisKey.Length == 0 && expressionPredicate.IsNullOrEmpty())
				throw new ArgumentOutOfRangeException(
					"thisKey",
					string.Format("Association '{0}.{1}' does not define keys.", type.Name, memberInfo.Name));

			if (thisKey.Length != otherKey.Length)
				throw new ArgumentException(
					string.Format(
						"Association '{0}.{1}' has different number of keys for parent and child objects.",
						type.Name, memberInfo.Name));

			MemberInfo          = memberInfo;
			ThisKey             = thisKey;
			OtherKey            = otherKey;
			ExpressionPredicate = expressionPredicate;
			Storage             = storage;
			CanBeNull           = canBeNull;
		}

		public MemberInfo MemberInfo          { get; set; }
		public string[]   ThisKey             { get; set; }
		public string[]   OtherKey            { get; set; }
		public string     ExpressionPredicate { get; set; }
		public string     Storage             { get; set; }
		public bool       CanBeNull           { get; set; }

		public static string[] ParseKeys(string keys)
		{
			return keys == null ? Array<string>.Empty : keys.Replace(" ", "").Split(',');
		}

		public LambdaExpression GetPredicate()
		{
			if (string.IsNullOrEmpty(ExpressionPredicate))
				return null;

			var type = MemberInfo.DeclaringType;

			if (type == null)
				throw new ArgumentException(string.Format("Member '{0}' has no declaring type", MemberInfo.Name));

			var members = type.GetStaticMembersEx(ExpressionPredicate);

			if (members.Length == 0)
				throw new LinqToDBException(string.Format("Static member '{0}' for type '{1}' not found", ExpressionPredicate, type.Name));

			if (members.Length > 1)
				throw new LinqToDBException(string.Format("Ambiguous members '{0}' for type '{1}' has been found", ExpressionPredicate, type.Name));

			Expression predicate = null;

			var propInfo = members[0] as PropertyInfo;

			if (propInfo != null)
			{
				var value = propInfo.GetValue(null, null);
				if (value == null)
					return null;

				predicate = value as Expression;
				if (predicate == null)
					throw new LinqToDBException(string.Format("Property '{0}' for type '{1}' should return expression",
						ExpressionPredicate, type.Name));
			}
			else
			{
				var method = members[0] as MethodInfo;
				if (method != null)
				{
					if (method.GetParameters().Length > 0)
						throw new LinqToDBException(string.Format("Method '{0}' for type '{1}' should have no parameters", ExpressionPredicate, type.Name));
					var value = method.Invoke(null, Array<object>.Empty);
					if (value == null)
						return null;

					predicate = value as Expression;
					if (predicate == null)
						throw new LinqToDBException(string.Format("Method '{0}' for type '{1}' should return expression", ExpressionPredicate, type.Name));
				}
			}
			if (predicate == null)
				throw new LinqToDBException(string.Format("Member '{0}' for type '{1}' should be static property or method", ExpressionPredicate, type.Name));

			var lambda = predicate as LambdaExpression;
			if (lambda == null || lambda.Parameters.Count != 2)
				throw new LinqToDBException("Invalid predicate expression");

			return lambda;
		}
	}
}
