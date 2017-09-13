using System;
using System.Linq.Expressions;
using System.Reflection;

using JNotNull = JetBrains.Annotations.NotNullAttribute;

namespace LinqToDB.Mapping
{
	using Common;
	using Extensions;

	/// <summary>
	/// Stores association descriptor.
	/// </summary>
	public class AssociationDescriptor
	{
		/// <summary>
		/// Creates descriptor instance.
		/// </summary>
		/// <param name="type">From (this) side entity mapping type.</param>
		/// <param name="memberInfo">Association member (field, property or method).</param>
		/// <param name="thisKey">List of names of from (this) key members.</param>
		/// <param name="otherKey">List of names of to (other) key members.</param>
		/// <param name="expressionPredicate">Optional predicate expresssion source property or method.</param>
		/// <param name="storage">Optional association value storage field or property name.</param>
		/// <param name="canBeNull">If <c>true</c>, association will generate outer join, otherwise - inner join.</param>
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

		/// <summary>
		/// Gets or sets association member (field, property or method).
		/// </summary>
		public MemberInfo MemberInfo          { get; set; }
		/// <summary>
		/// Gets or sets list of names of from (this) key members. Could be empty, if association has predicate expression.
		/// </summary>
		public string[]   ThisKey             { get; set; }
		/// <summary>
		/// Gets or sets list of names of to (other) key members. Could be empty, if association has predicate expression.
		/// </summary>
		public string[]   OtherKey            { get; set; }
		/// <summary>
		/// Gets or sets optional predicate expresssion source property or method.
		/// </summary>
		public string     ExpressionPredicate { get; set; }
		/// <summary>
		/// Gets or sets optional association value storage field or property name. Used with LoadWith.
		/// </summary>
		public string     Storage             { get; set; }
		/// <summary>
		/// Gets or sets join type, generated for current association.
		/// If <c>true</c>, association will generate outer join, otherwise - inner join.
		/// </summary>
		public bool       CanBeNull           { get; set; }

		/// <summary>
		/// Parse comma-separated list of association key column members into string array.
		/// </summary>
		/// <param name="keys">Comma-separated (spaces allowed) list of association key column members.</param>
		/// <returns>Returns array with names of association key column members.</returns>
		public static string[] ParseKeys(string keys)
		{
			return keys == null ? Array<string>.Empty : keys.Replace(" ", "").Split(',');
		}

		/// <summary>
		/// Loads predicate expression from <see cref="ExpressionPredicate"/> member.
		/// </summary>
		/// <returns><c>null</c> of association has no custom predicate expression or predicate expression, specified
		/// by <see cref="ExpressionPredicate"/> member.</returns>
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
