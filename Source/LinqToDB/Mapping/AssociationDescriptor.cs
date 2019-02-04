using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Expressions;
using LinqToDB.Linq.Builder;
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
		/// <param name="expressionPredicate">Optional predicate expression source property or method.</param>
		/// <param name="predicate">Optional predicate expression.</param>
		/// <param name="expressionQueryMethod">Optional name of query method.</param>
		/// <param name="expressionQuery">Optional query expression.</param>
		/// <param name="storage">Optional association value storage field or property name.</param>
		/// <param name="canBeNull">If <c>true</c>, association will generate outer join, otherwise - inner join.</param>
		/// <param name="aliasName">Optional alias for representation in SQL.</param>
		public AssociationDescriptor(
			[JNotNull] Type       type,
			[JNotNull] MemberInfo memberInfo,
			           string[]   thisKey,
			           string[]   otherKey,
			           string     expressionPredicate,
			           Expression predicate,
			           string     expressionQueryMethod,
			           Expression expressionQuery,
			           string     storage,
			           bool       canBeNull,
					   string     aliasName)
		{
			if (memberInfo == null) throw new ArgumentNullException(nameof(memberInfo));
			if (thisKey    == null) throw new ArgumentNullException(nameof(thisKey));
			if (otherKey   == null) throw new ArgumentNullException(nameof(otherKey));

			if (thisKey.Length == 0 && expressionPredicate.IsNullOrEmpty() && predicate == null && expressionQueryMethod.IsNullOrEmpty() && expressionQuery == null)
				throw new ArgumentOutOfRangeException(
					nameof(thisKey),
					$"Association '{type.Name}.{memberInfo.Name}' does not define keys.");

			if (thisKey.Length != otherKey.Length)
				throw new ArgumentException(
					$"Association '{type.Name}.{memberInfo.Name}' has different number of keys for parent and child objects.");

			MemberInfo            = memberInfo;
			ThisKey               = thisKey;
			OtherKey              = otherKey;
			ExpressionPredicate   = expressionPredicate;
			Predicate             = predicate;
			ExpressionQueryMethod = expressionQueryMethod;
			ExpressionQuery       = expressionQuery;
			Storage               = storage;
			CanBeNull             = canBeNull;
			AliasName             = aliasName;
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
		/// Gets or sets optional predicate expression source property or method.
		/// </summary>
		public string     ExpressionPredicate { get; set; }
		/// <summary>
		/// Gets or sets optional query method source property or method.
		/// </summary>
		public string     ExpressionQueryMethod { get; set; }
		/// <summary>
		/// Gets or sets optional query expression.
		/// </summary>
		public Expression ExpressionQuery     { get; set; }
		/// <summary>
		/// Gets or sets optional predicate expression.
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
		/// Gets or sets alias for association. Used in SQL generation process.
		/// </summary>
		public string     AliasName           { get; set; }

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

		/// <summary>
		/// Loads query method expression from <see cref="ExpressionQueryMethod"/> member.
		/// </summary>
		/// <param name="parentType">Type of object that declares association</param>
		/// <param name="objectType">Type of object associated with query method expression</param>
		/// <returns><c>null</c> of association has no custom query method expression or query method expression, specified
		/// by <see cref="ExpressionQueryMethod"/> member.</returns>
		public LambdaExpression GetQueryMethod(Type parentType, Type objectType)
		{
			if (ExpressionQuery == null && ExpressionQueryMethod.IsNullOrEmpty())
				return null;

			Expression queryExpression = null;

			var type = MemberInfo.DeclaringType;

			if (type == null)
				throw new ArgumentException($"Member '{MemberInfo.Name}' has no declaring type");

			if (!string.IsNullOrEmpty(ExpressionQueryMethod))
			{
				var members = type.GetStaticMembersEx(ExpressionQueryMethod);

				if (members.Length == 0)
					throw new LinqToDBException($"Static member '{ExpressionQueryMethod}' for type '{type.Name}' not found");

				if (members.Length > 1)
					throw new LinqToDBException($"Ambiguous members '{ExpressionQueryMethod}' for type '{type.Name}' has been found");

				var propInfo = members[0] as PropertyInfo;

				if (propInfo != null)
				{
					var value = propInfo.GetValue(null, null);
					if (value == null)
						return null;

					queryExpression = value as Expression;
					if (queryExpression == null)
						throw new LinqToDBException($"Property '{ExpressionQueryMethod}' for type '{type.Name}' should return expression");
				}
				else
				{
					var method = members[0] as MethodInfo;
					if (method != null)
					{
						if (method.GetParameters().Length > 0)
							throw new LinqToDBException($"Method '{ExpressionQueryMethod}' for type '{type.Name}' should have no parameters");
						var value = method.Invoke(null, Array<object>.Empty);
						if (value == null)
							return null;

						queryExpression = value as Expression;
						if (queryExpression == null)
							throw new LinqToDBException($"Method '{ExpressionQueryMethod}' for type '{type.Name}' should return expression");
					}
				}
				if (queryExpression == null)
					throw new LinqToDBException(
						$"Member '{ExpressionQueryMethod}' for type '{type.Name}' should be static property or method");
			}
			else
				queryExpression = ExpressionQuery;

			var lambda = queryExpression as LambdaExpression;
			if (lambda == null || lambda.Parameters.Count != 2)
				if (!string.IsNullOrEmpty(ExpressionQueryMethod))
					throw new LinqToDBException(
						$"Invalid predicate expression in {type.Name}.{ExpressionQueryMethod}. Expected: Expression<Func<{parentType.Name}, IDataContext, IQueryable<{objectType.Name}>>>");
				else
					throw new LinqToDBException(
						$"Invalid predicate expression in {type.Name}. Expected: Expression<Func<{parentType.Name}, IDataContext, IQueryable<{objectType.Name}>>>");

			if (!lambda.Parameters[0].Type.IsSameOrParentOf(parentType))
				throw new LinqToDBException($"First parameter of expression predicate should be '{parentType.Name}'");

			if (typeof(IDataContext) != lambda.Parameters[1].Type)
				throw new LinqToDBException("Second parameter of expression predicate should be 'IDataContext'");

			if (!(typeof(IQueryable<>).IsSameOrParentOf(lambda.ReturnType) &&
			      lambda.ReturnType.GetGenericArguments()[0].IsSameOrParentOf(objectType)))
				throw new LinqToDBException("Result type of expression predicate should be 'IQueryable<{objectType.Name}>'");

			return lambda;
		}

		internal static MethodInfo getTableMethodInfo = 
			MemberHelper.MethodOf<IDataContext>(ctx => ctx.GetTable<object>()).GetGenericMethodDefinition();

		internal static MethodInfo whereMethodInfo = 
			MemberHelper.MethodOf<IQueryable<object>>(q => q.Where(e => true)).GetGenericMethodDefinition();

		internal static MethodInfo loadWithMethodInfo = 
			MemberHelper.MethodOf<ITable<object>>(q => q.LoadWith(e => null)).GetGenericMethodDefinition();

		private static Expression ApplyLoadWith(Expression getTableExpression, List<MemberInfo[]> loadWith)
		{
			if (loadWith == null || loadWith.Count == 0)
				return getTableExpression;

			var associationType = getTableExpression.Type.GetGenericArguments()[0];

			foreach (var members in loadWith)
			{
				var pLoadWith  = Expression.Parameter(associationType, "t");
				var isPrevList = false;

				Expression obj = pLoadWith;

				foreach (var member in members)
				{
					if (isPrevList)
						obj = new GetItemExpression(obj);

					obj = Expression.MakeMemberAccess(obj, member);

					isPrevList = typeof(IEnumerable).IsSameOrParentOf(obj.Type);
				}

				getTableExpression =
					Expression.Call(null, loadWithMethodInfo.MakeGenericMethod(associationType), pLoadWith);
			}

			return getTableExpression;
		}

		public LambdaExpression GetAssociationExpression(Type parentType, Type objectType, List<MemberInfo[]> loadWith, MappingSchema mappingSchema)
		{
			var result = GetQueryMethod(parentType, objectType);
			if (result != null)
			{
				if (loadWith != null)
				{
					result = (LambdaExpression)result.Transform(e =>
					{
						if (e.NodeType == ExpressionType.Call)
						{
							var mc = (MethodCallExpression)e;
							if (mc.Method.GetGenericMethodDefinition() == getTableMethodInfo &&
							    mc.Method.GetGenericArguments()[0] == objectType)
							{
								e = ApplyLoadWith(mc, loadWith);
							}
						}
						return e;
					});
				}

				return result;
			}

			var lContext = Expression.Parameter(typeof(IDataContext), "ctx");
			var lParent  = Expression.Parameter(parentType, "parent");
			var lChild   = Expression.Parameter(objectType, "child");

			Expression expr = null;

			var thisEntityDescriptor = mappingSchema.GetEntityDescriptor(parentType);
			var otherEntityDescriptor = mappingSchema.GetEntityDescriptor(objectType);

			for (var i = 0; i < ThisKey.Length; i++)
			{
				var thisKey = ThisKey[i];
				var otherKey = OtherKey[i];

				var thisColumn = thisEntityDescriptor.Columns.Find(cd => cd.MemberName == thisKey);
				var otherColumn = otherEntityDescriptor.Columns.Find(cd => cd.MemberName == otherKey);

//				var thisMemberInfo  = parentType.GetPropertyEx(thisKey) as MemberInfo  ?? parentType.GetFieldEx(thisKey);
//				var otherMemberInfo = objectType.GetPropertyEx(otherKey) as MemberInfo ?? objectType.GetFieldEx(otherKey);

				var thisProp = Expression.Call(null,
					ReflectionExtensions.SQLPropertyMethod.MakeGenericMethod(thisColumn.MemberType),
					lParent, Expression.Constant(thisKey));

				var otherProp = Expression.Call(null,
					ReflectionExtensions.SQLPropertyMethod.MakeGenericMethod(otherColumn.MemberType),
					lChild, Expression.Constant(otherKey));

//				var thisProp  = Expression.PropertyOrField(lParent, thisKey);
//				var otherProp = Expression.PropertyOrField(lChild,  otherKey);

				var ex = ExpressionBuilder.Equal(mappingSchema, otherProp, thisProp);

				expr = expr == null ? ex : Expression.AndAlso(expr, ex);
			}

			var predicate = GetPredicate(parentType, objectType);
			if (predicate != null)
			{
				var body = predicate.GetBody(lParent, lChild);
				expr = expr == null ? body : Expression.AndAlso(expr, body);
			}

			// transform to
			// (lParent, lContext) => lContext.GetTable<objectType>().Where(expr)

			var getTable = (Expression)Expression.Call(null, getTableMethodInfo.MakeGenericMethod(objectType), lContext);
			    getTable = ApplyLoadWith(getTable, loadWith);
			var where    = Expression.Call(null, whereMethodInfo.MakeGenericMethod(objectType), getTable, Expression.Lambda(expr, lChild));
			    result   = Expression.Lambda(where, lParent, lContext);

			return result;
		}
	}
}
