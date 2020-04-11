using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Mapping;
	using Reflection;

	static class AssociationHelper
	{
		// Returns
		// (ParentType p, TDataContext dc) => dc.GetTable<ObjectType>().Where(...)
		// (ParentType p, TDataContext dc) => dc.GetTable<ObjectType>().Where(...).DefaultIfEmpty
		public static LambdaExpression CreateAssociationQueryLambda(ExpressionBuilder builder, AssociationDescriptor association, Type parentType, Type objectType, bool inline, bool enforceDefault, out bool isLeft)
		{
			var definedQueryMethod = association.GetQueryMethod(parentType, objectType);

			var shouldAddDefaultIfEmpty = enforceDefault;

			if (definedQueryMethod == null)
			{
				var parentParam = Expression.Parameter(parentType, "parent");
				var childParam  = Expression.Parameter(objectType, "child");

				var parentAccessor = TypeAccessor.GetAccessor(parentType);
				var childAccessor  = TypeAccessor.GetAccessor(objectType);

				Expression? predicate = null;
				for (var i = 0; i < association.ThisKey.Length; i++)
				{
					var parentName   = association.ThisKey[i];
					var parentMember = parentAccessor.Members.Find(m => m.MemberInfo.Name == parentName);

					if (parentMember == null)
						throw new LinqException("Association key '{0}' not found for type '{1}.", parentName,
							parentType);

					var childName = association.OtherKey[i];
					var childMember = childAccessor.Members.Find(m => m.MemberInfo.Name == childName);

					if (childMember == null)
						throw new LinqException("Association key '{0}' not found for type '{1}.", childName,
							objectType);

					var current = ExpressionBuilder.Equal(builder.MappingSchema,
						Expression.MakeMemberAccess(parentParam, parentMember.MemberInfo),
						Expression.MakeMemberAccess(childParam, childMember.MemberInfo));

					predicate = predicate == null ? current : Expression.AndAlso(predicate, current);
				}

				var expressionPredicate = association.GetPredicate(parentType, objectType);

				if (expressionPredicate != null)
				{
					shouldAddDefaultIfEmpty = true;

					var replacedBody = expressionPredicate.GetBody(parentParam, childParam);

					predicate = predicate == null ? replacedBody : Expression.AndAlso(predicate, replacedBody);
				}

				if (predicate == null)
					throw new LinqException("Can not generate Association predicate");

				if (inline && !shouldAddDefaultIfEmpty)
				{
					var ed = builder.MappingSchema.GetEntityDescriptor(objectType);
					if (ed.QueryFilterFunc != null)
						shouldAddDefaultIfEmpty = true;
				}

				var dcParam    = Expression.Parameter(builder.DataContext.GetType(), "dc");
				var queryParam = Expression.Call(Methods.LinqToDB.GetTable.MakeGenericMethod(objectType), dcParam);

				var filterLambda = Expression.Lambda(predicate, childParam);
				Expression body  = Expression.Call(Methods.Queryable.Where.MakeGenericMethod(objectType), queryParam,
					filterLambda);

				definedQueryMethod = Expression.Lambda(body, parentParam, dcParam);
			}
			else
			{
				shouldAddDefaultIfEmpty = true;
				var bodyExpression = definedQueryMethod.Body.Unwrap();
				if (bodyExpression.NodeType == ExpressionType.Call)
				{
					var mc = (MethodCallExpression)bodyExpression;
					if (mc.IsSameGenericMethod(Methods.Queryable.DefaultIfEmpty, Methods.Queryable.DefaultIfEmptyValue))
						shouldAddDefaultIfEmpty = false;
				}
			}

			// if (objectType != OriginalType)
			// {
			// 	var body = definedQueryMethod.Body.Unwrap();
			// 	body = Expression.Call(Methods.Queryable.OfType.MakeGenericMethod(OriginalType), body);
			// 	body = EagerLoading.EnsureEnumerable(body, Builder.MappingSchema);
			// 	definedQueryMethod = Expression.Lambda(body, definedQueryMethod.Parameters);
			// }
			//
			if (inline && shouldAddDefaultIfEmpty)
			{
				var body = definedQueryMethod.Body.Unwrap();
				body = Expression.Call(Methods.Queryable.DefaultIfEmpty.MakeGenericMethod(objectType), body);
				definedQueryMethod = Expression.Lambda(body, definedQueryMethod.Parameters);
			}

			definedQueryMethod = (LambdaExpression)builder.ConvertExpressionTree(definedQueryMethod);
			definedQueryMethod = (LambdaExpression)builder.ConvertExpression(definedQueryMethod);
			definedQueryMethod = (LambdaExpression)definedQueryMethod.OptimizeExpression()!;

			isLeft = shouldAddDefaultIfEmpty;
			return definedQueryMethod;
		}

	}

}
