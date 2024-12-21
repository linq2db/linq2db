using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Mapping
{
	using Common;
	using Common.Internal;
	using Extensions;
	using Linq.Builder;
	using Expressions;

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
		/// <param name="expressionPredicate">Optional predicate expression source property or method.</param>
		/// <param name="predicate">Optional predicate expression.</param>
		/// <param name="expressionQueryMethod">Optional name of query method.</param>
		/// <param name="expressionQuery">Optional query expression.</param>
		/// <param name="storage">Optional association value storage field or property name.</param>
		/// <param name="associationSetterExpressionMethod">Optional name of setter method.</param>
		/// <param name="associationSetterExpression">Optional setter expression.</param>
		/// <param name="canBeNull">If <c>true</c>, association will generate outer join, otherwise - inner join.</param>
		/// <param name="aliasName">Optional alias for representation in SQL.</param>
		public AssociationDescriptor(
			MappingSchema mappingSchema,
			Type          type,
			MemberInfo    memberInfo,
			string[]      thisKey,
			string[]      otherKey,
			string?       expressionPredicate,
			Expression?   predicate,
			string?       expressionQueryMethod,
			Expression?   expressionQuery,
			string?       storage,
			string?       associationSetterExpressionMethod,
			Expression?   associationSetterExpression,
			bool?         canBeNull,
			string?       aliasName)
		{
			if (memberInfo == null) throw new ArgumentNullException(nameof(memberInfo));
			if (thisKey    == null) throw new ArgumentNullException(nameof(thisKey));
			if (otherKey   == null) throw new ArgumentNullException(nameof(otherKey));

			if (thisKey.Length == 0 && string.IsNullOrEmpty(expressionPredicate) && predicate == null && string.IsNullOrEmpty(expressionQueryMethod) && expressionQuery == null)
				throw new ArgumentOutOfRangeException(
					nameof(thisKey),
					$"Association '{type.Name}.{memberInfo.Name}' does not define keys.");

			if (thisKey.Length != otherKey.Length)
				throw new ArgumentException(
					$"Association '{type.Name}.{memberInfo.Name}' has different number of keys for parent and child objects.");

			MappingSchema                     = mappingSchema;
			MemberInfo                        = memberInfo;
			ThisKey                           = thisKey;
			OtherKey                          = otherKey;
			ExpressionPredicate               = expressionPredicate;
			Predicate                         = predicate;
			ExpressionQueryMethod             = expressionQueryMethod;
			ExpressionQuery                   = expressionQuery;
			Storage                           = storage;
			AssociationSetterExpressionMethod = associationSetterExpressionMethod;
			AssociationSetterExpression       = associationSetterExpression;
			CanBeNull                         = canBeNull ?? AnalyzeCanBeNull();
			AliasName                         = aliasName;
		}

		/// <summary>
		/// Gets MappingSchema for current descriptor.
		/// </summary>
		public MappingSchema MappingSchema     { get; }

		/// <summary>
		/// Gets association member (field, property or method).
		/// </summary>
		public MemberInfo  MemberInfo          { get; }
		/// <summary>
		/// Gets list of names of from (this) key members. Could be empty, if association has predicate expression.
		/// </summary>
		public string[]    ThisKey             { get; }
		/// <summary>
		/// Gets list of names of to (other) key members. Could be empty, if association has predicate expression.
		/// </summary>
		public string[]    OtherKey            { get; }
		/// <summary>
		/// Gets optional predicate expression source property or method.
		/// </summary>
		public string?     ExpressionPredicate { get; }
		/// <summary>
		/// Gets optional query method source property or method.
		/// </summary>
		public string?     ExpressionQueryMethod { get; }
		/// <summary>
		/// Gets optional query expression.
		/// </summary>
		public Expression? ExpressionQuery     { get; }
		/// <summary>
		/// Gets optional predicate expression.
		/// </summary>
		public Expression? Predicate           { get; }
		/// <summary>
		/// Gets optional association value storage field or property name. Used with LoadWith.
		/// </summary>
		public string?     Storage             { get; }
		/// <summary>
		/// Gets optional setter method source property or method.
		/// </summary>
		public string? AssociationSetterExpressionMethod { get; }
		/// <summary>
		/// Gets optional setter expression.
		/// </summary>
		public Expression? AssociationSetterExpression { get; }
		/// <summary>
		/// Gets join type, generated for current association.
		/// If <c>true</c>, association will generate outer join, otherwise - inner join.
		/// </summary>
		public bool        CanBeNull           { get; }
		/// <summary>
		/// Gets alias for association. Used in SQL generation process.
		/// </summary>
		public string?     AliasName           { get; }

		/// <summary>
		/// Parse comma-separated list of association key column members into string array.
		/// </summary>
		/// <param name="keys">Comma-separated (spaces allowed) list of association key column members.</param>
		/// <returns>Returns array with names of association key column members.</returns>
		public static string[] ParseKeys(string? keys)
		{
			return keys?.Replace(" ", "").Split(',') ?? [];
		}

		/// <summary>
		/// Generates table alias for association.
		/// </summary>
		/// <returns>Generated alias.</returns>
		public string GenerateAlias()
		{
			if (!string.IsNullOrEmpty(AliasName))
				return AliasName!;

			if (Configuration.Sql.AssociationAliasFormat != null)
				return string.Format(CultureInfo.InvariantCulture, Configuration.Sql.AssociationAliasFormat, MemberInfo.Name);

			return string.Empty;
		}

		bool? _isList;
		public bool IsList => _isList ??= MappingSchema.IsCollectionType(MemberInfo.GetMemberType());

		Type? _elementType;
		public Type GetElementType() => _elementType ??= EagerLoading.GetEnumerableElementType(MemberInfo.GetMemberType(), MappingSchema);

		public Type GetParentElementType()
		{
			if (MemberInfo.MemberType == MemberTypes.Method)
			{
				var methodInfo = (MethodInfo)MemberInfo;
				if (methodInfo.IsStatic)
				{
					var pms = methodInfo.GetParameters();
					if (pms.Length > 0)
					{
						return pms[0].ParameterType;
					}
				}
				else
				{
					return methodInfo.DeclaringType!;
				}

				throw new LinqToDBException($"Cannot retrieve declaring type form member {methodInfo}");
			}

			return MemberInfo.DeclaringType!;
		}

		/// <summary>
		/// Loads predicate expression from <see cref="ExpressionPredicate"/> member.
		/// </summary>
		/// <param name="parentType">Type of object that declares association</param>
		/// <param name="objectType">Type of object associated with expression predicate</param>
		/// <returns><c>null</c> of association has no custom predicate expression or predicate expression, specified
		/// by <see cref="ExpressionPredicate"/> member.</returns>
		public LambdaExpression? GetPredicate(Type parentType, Type objectType)
		{
			if (Predicate == null && string.IsNullOrEmpty(ExpressionPredicate))
				return null;

			Expression? predicate = null;

			var type = MemberInfo.DeclaringType;

			if (type == null)
				throw new ArgumentException($"Member '{MemberInfo.Name}' has no declaring type");

			if (!string.IsNullOrEmpty(ExpressionPredicate))
			{
				var members = type.GetStaticMembersEx(ExpressionPredicate!);

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
						var value = method.InvokeExt(null, []);
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

			var firstParameter = lambda.Parameters[0];
			if (!firstParameter.Type.IsSameOrParentOf(parentType) && !parentType.IsSameOrParentOf(firstParameter.Type))
			{
				throw new LinqToDBException($"First parameter of expression predicate should be '{parentType.Name}'");
			}

			if (lambda.Parameters[1].Type != objectType)
				throw new LinqToDBException($"Second parameter of expression predicate should be '{objectType.Name}'");

			if (lambda.ReturnType != typeof(bool))
				throw new LinqToDBException("Result type of expression predicate should be 'bool'");

			return lambda;
		}

		public bool HasQueryMethod()
		{
			return ExpressionQuery != null || !string.IsNullOrEmpty(ExpressionQueryMethod);
		}

		/// <summary>
		/// Loads query method expression from <see cref="ExpressionQueryMethod"/> member.
		/// </summary>
		/// <param name="parentType">Type of object that declares association</param>
		/// <param name="objectType">Type of object associated with query method expression</param>
		/// <returns><c>null</c> of association has no custom query method expression or query method expression, specified
		/// by <see cref="ExpressionQueryMethod"/> member.</returns>
		public LambdaExpression? GetQueryMethod(Type parentType, Type objectType)
		{
			if (!HasQueryMethod())
				return null;

			Expression queryExpression;

			var type = MemberInfo.DeclaringType;

			if (type == null)
				throw new ArgumentException($"Member '{MemberInfo.Name}' has no declaring type");

			if (!string.IsNullOrEmpty(ExpressionQueryMethod))
				queryExpression = type.GetExpressionFromExpressionMember<Expression>(ExpressionQueryMethod!);
			else
				queryExpression = ExpressionQuery!;

			var lambda = queryExpression as LambdaExpression;
			if (lambda == null || lambda.Parameters.Count < 1)
				if (!string.IsNullOrEmpty(ExpressionQueryMethod))
					throw new LinqToDBException(
						$"Invalid predicate expression in {type.Name}.{ExpressionQueryMethod}. Expected: Expression<Func<{parentType.Name}, IDataContext, IQueryable<{objectType.Name}>>>");
				else
					throw new LinqToDBException(
						$"Invalid predicate expression in {type.Name}. Expected: Expression<Func<{parentType.Name}, IDataContext, IQueryable<{objectType.Name}>>>");

			if (!lambda.Parameters[0].Type.IsSameOrParentOf(parentType))
				throw new LinqToDBException($"First parameter of expression predicate should be '{parentType.Name}'");

			if (!(typeof(IQueryable<>).IsSameOrParentOf(lambda.ReturnType) &&
			      lambda.ReturnType.GetGenericArguments()[0].IsSameOrParentOf(objectType)))
				throw new LinqToDBException($"Result type of expression predicate should be 'IQueryable<{objectType.Name}>'");

			return lambda;
		}

		private bool AnalyzeCanBeNull()
		{
			// Note that nullability of Collections can't be determined from types.
			// OUTER JOIN are usually materialized in non-nullable, but empty, collections.
			// For example, `IList<Product> Products` might well require an OUTER JOIN.
			// Neither `IList<Product>?` nor `IList<Product?>` would be correct.
			return Configuration.UseNullableTypesMetadata && !IsList && Nullability.TryAnalyzeMember(MemberInfo, out var isNullable)
				? isNullable
				: true;
		}

		#region Assignment helpers

		bool HasAssociationSetterMethod()
		{
			return AssociationSetterExpression != null || !string.IsNullOrEmpty(AssociationSetterExpressionMethod);
		}

		/// <summary>
		/// Loads setter method expression from <see cref="AssociationSetterExpression"/> member.
		/// </summary>
		/// <param name="memberType">Type of the storage member that declares association</param>
		/// <param name="objectType">Type of object associated with setter method expression</param>
		/// <returns><c>null</c> if association has no custom setter method expression specified
		/// by <see cref="AssociationSetterExpressionMethod"/> member.</returns>
		LambdaExpression? GetAssociationSetterMethod(Type memberType, Type objectType)
		{
			if (!HasAssociationSetterMethod())
				return null;

			Expression setExpression;

			var type = MemberInfo.DeclaringType;

			if (type == null)
				throw new ArgumentException($"Member '{MemberInfo.Name}' has no declaring type");

			if (!string.IsNullOrEmpty(AssociationSetterExpressionMethod))
				setExpression = type.GetExpressionFromExpressionMember<Expression>(AssociationSetterExpressionMethod!);
			else
				setExpression = AssociationSetterExpression!;

			var lambda = setExpression as LambdaExpression;
			if (lambda == null || lambda.Parameters.Count != 2)
				if (!string.IsNullOrEmpty(AssociationSetterExpressionMethod))
					throw new LinqToDBException(
						$"Invalid setter expression in {type.Name}.{AssociationSetterExpressionMethod}. Expected: Expression<Action<{memberType.Name}, {objectType.Name}>>");
				else
					throw new LinqToDBException(
						$"Invalid setter expression in {type.Name}. Expected: Expression<Action<{memberType.Name}, {objectType.Name}>>");

			if (!lambda.Parameters[0].Type.IsSameOrParentOf(memberType))
				throw new LinqToDBException($"First parameter of setter expression should be '{memberType.Name}'");

			if (lambda.ReturnType != typeof(void))
				throw new LinqToDBException("Result type of setter expression should be 'void'");

			return lambda;
		}

		/// <summary>
		/// Get the association assignment expression, accounting for <see cref="Storage"/> and <see cref="AssociationSetterExpression" />
		/// </summary>
		/// <param name="value">Association value expression</param>
		/// <param name="memberInfo">Member info</param>
		/// <returns></returns>
		internal LambdaExpression? GetAssociationAssignmentLambda(Expression value, MemberInfo memberInfo)
		{
			if (Storage == null && !HasAssociationSetterMethod())
				return null;

			var entityParam = Expression.Parameter(memberInfo.DeclaringType!, "e");

			var storageMember = Storage != null
				? ExpressionHelper.PropertyOrField(entityParam, Storage)
				: Expression.MakeMemberAccess(entityParam, memberInfo);

			Expression body;
			if (HasAssociationSetterMethod())
			{
				var setMethod = GetAssociationSetterMethod(storageMember.Type, value.Type)!;
				body = setMethod.GetBody(storageMember, value);
			}
			else
			{
				body = Expression.Assign(storageMember, value);
			}

			return Expression.Lambda(body, entityParam);
		}

		/// <summary>
		/// Gets the desired type for the association value to be used by the assignment expression
		/// returned by <see cref="GetAssociationAssignmentLambda" />
		/// </summary>
		/// <param name="memberInfo"></param>
		/// <param name="parentType"></param>
		/// <param name="objectType"></param>
		/// <returns></returns>
		internal Type GetAssociationDesiredAssignmentType(MemberInfo memberInfo, Type parentType, Type objectType)
		{
			var storageMember = Storage != null
				? ExpressionHelper.GetPropertyOrFieldMemberInfo(parentType, Storage)
				: memberInfo;

			if (HasAssociationSetterMethod())
			{
				var defaultSetterValueType = IsList
					? typeof(IEnumerable<>).MakeGenericType(objectType)
					: objectType;

				var setterMethod = GetAssociationSetterMethod(
					storageMember.GetMemberType(),
					defaultSetterValueType)!;

				return setterMethod.Parameters[1].Type;
			}

			return storageMember.GetMemberType();
		}

		public override string ToString()
		{
			return MemberInfo.Name;
		}

		#endregion

	}
}
