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
		/// <param name="predicate">Optional predicate expresssion.</param>
		/// <param name="storage">Optional association value storage field or property name.</param>
		/// <param name="canBeNull">If <c>true</c>, association will generate outer join, otherwise - inner join.</param>
		public AssociationDescriptor(
			[JNotNull] Type       type,
			[JNotNull] MemberInfo memberInfo,
			           string[]   thisKey,
			           string[]   otherKey,
			           string     expressionPredicate,
					   Expression predicate,
			           string     storage,
			           bool       canBeNull)
		{
			if (memberInfo == null) throw new ArgumentNullException(nameof(memberInfo));
			if (thisKey    == null) throw new ArgumentNullException(nameof(thisKey));
			if (otherKey   == null) throw new ArgumentNullException(nameof(otherKey));

			if (thisKey.Length == 0 && expressionPredicate.IsNullOrEmpty() && predicate == null)
				throw new ArgumentOutOfRangeException(
					nameof(thisKey),
					$"Association '{type.Name}.{memberInfo.Name}' does not define keys.");

			if (thisKey.Length != otherKey.Length)
				throw new ArgumentException(
					$"Association '{type.Name}.{memberInfo.Name}' has different number of keys for parent and child objects.");

			MemberInfo          = memberInfo;
			ThisKey             = thisKey;
			OtherKey            = otherKey;
			ExpressionPredicate = expressionPredicate;
			Predicate           = predicate;
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
		/// Gets or sets optional predicate expresssion.
		/// </summary>
		public Expression Predicate           { get; set; }
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
			return keys?.Replace(" ", "").Split(',') ?? Array<string>.Empty;
		}

		/// <summary>
		/// Loads predicate expression from <see cref="ExpressionPredicate"/> member.
		/// </summary>
		/// <param name="parentType">Type of object that declares association</param>
		/// <param name="objectType">Type of object associated with expression predicate</param>
		/// <returns><c>null</c> of association has no custom predicate expression or predicate expression, specified
		/// by <see cref="ExpressionPredicate"/> member.</returns>
		public LambdaExpression GetPredicate(Type parentType, Type objectType)
		{
			if (Predicate == null && string.IsNullOrEmpty(ExpressionPredicate))
				return null;

			Expression predicate = null;

			var type = MemberInfo.DeclaringType;

			if (type == null)
				throw new ArgumentException($"Member '{MemberInfo.Name}' has no declaring type");

			if (!string.IsNullOrEmpty(ExpressionPredicate))
			{ 
				var members = type.GetStaticMembersEx(ExpressionPredicate);

				if (members.Length == 0)
					throw new LinqToDBException($"Static member '{ExpressionPredicate}' for type '{type.Name}' not found");

				if (members.Length > 1)
					throw new LinqToDBException($"Ambiguous members '{ExpressionPredicate}' for type '{type.Name}' has been found");

				var propInfo = members[0] as PropertyInfo;

				if (propInfo != null)
				{
					var value = propInfo.GetValue(null, null);
					if (value == null)
						return null;

					predicate = value as Expression;
					if (predicate == null)
						throw new LinqToDBException($"Property '{ExpressionPredicate}' for type '{type.Name}' should return expression");
				}
				else
				{
					var method = members[0] as MethodInfo;
					if (method != null)
					{
						if (method.GetParameters().Length > 0)
							throw new LinqToDBException($"Method '{ExpressionPredicate}' for type '{type.Name}' should have no parameters");
						var value = method.Invoke(null, Array<object>.Empty);
						if (value == null)
							return null;

						predicate = value as Expression;
						if (predicate == null)
							throw new LinqToDBException($"Method '{ExpressionPredicate}' for type '{type.Name}' should return expression");
					}
				}
				if (predicate == null)
					throw new LinqToDBException(
						$"Member '{ExpressionPredicate}' for type '{type.Name}' should be static property or method");
			}
			else 
				predicate = Predicate;

			var lambda = predicate as LambdaExpression;
			if (lambda == null || lambda.Parameters.Count != 2)
				if (!string.IsNullOrEmpty(ExpressionPredicate))
					throw new LinqToDBException(
						$"Invalid predicate expression in {type.Name}.{ExpressionPredicate}. Expected: Expression<Func<{parentType.Name}, {objectType.Name}, bool>>");
				else
					throw new LinqToDBException(
						$"Invalid predicate expression in {type.Name}. Expected: Expression<Func<{parentType.Name}, {objectType.Name}, bool>>");

			if (!lambda.Parameters[0].Type.IsSameOrParentOf(parentType))
				throw new LinqToDBException($"First parameter of expression predicate should be '{parentType.Name}'");

			if (lambda.Parameters[1].Type != objectType)
				throw new LinqToDBException($"Second parameter of expression predicate should be '{objectType.Name}'");

			if (lambda.ReturnType != typeof(bool))
				throw new LinqToDBException("Result type of expression predicate should be 'bool'");

			return lambda;
		}
	}
}
