using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using Extensions;
	using LinqToDB.Expressions;
	using Mapping;
	using Reflection;

	static class AssociationHelper
	{
		static readonly MethodInfo[] DefaultIfEmptyMethods = new [] { Methods.Queryable.DefaultIfEmpty, Methods.Queryable.DefaultIfEmptyValue };

		// Returns
		// (ParentType p) => dc.GetTable<ObjectType>().Where(...)
		// (ParentType p) => dc.GetTable<ObjectType>().Where(...).DefaultIfEmpty
		public static LambdaExpression CreateAssociationQueryLambda(
			ExpressionBuilder     builder,
			MappingSchema         mappingSchema,
			AccessorMember        onMember,
			AssociationDescriptor association,
			Type                  parentOriginalType,
			Type                  parentType,
			Type                  objectType,
			Expression?           additionalCondition,
			bool                  inline,
			bool?                 enforceDefault,
			LoadWithInfo?         loadWith,
			MemberInfo[]?         loadWithPath,
			out bool?             isOuter)
		{
			Expression dataContextExpr = SqlQueryRootExpression.Create(mappingSchema, builder.DataContext.GetType());

			// We are trying to keep fast cache hit behaviour, so cache check should be added only if needed
			//
			bool shouldAddCacheCheck = false;

			bool cacheCheckAdded = false;

			LambdaExpression? definedQueryMethod  = null;
			if (association.HasQueryMethod())
			{
				/*// here we tell for Expression Comparer to compare optimized Association expressions
				//
				definedQueryMethod = (LambdaExpression)builder.AddQueryableMemberAccessors((association, parentType, objectType), onMember, builder.DataContext, static (context, mi, dc) =>
				{
					var queryLambda         = context.association.GetQueryMethod(context.parentType, context.objectType) ?? throw new InvalidOperationException();
					var optimizationContext = new ExpressionTreeOptimizationContext(dc);
					var optimizedExpr       = optimizationContext.ExposeExpression(queryLambda);
					    optimizedExpr       = optimizationContext.ExpandQueryableMethods(optimizedExpr);
					return optimizedExpr;
				});*/


				definedQueryMethod = association.GetQueryMethod(parentType, objectType) ?? throw new InvalidOperationException();
				cacheCheckAdded = true;

				var parameterMatch = new Dictionary<ParameterExpression, Expression>();
				if (onMember.Arguments == null)
				{
					if (definedQueryMethod.Parameters.Count > 1 && typeof(IDataContext).IsSameOrParentOf(definedQueryMethod.Parameters[1].Type))
						parameterMatch.Add(definedQueryMethod.Parameters[1], dataContextExpr);
				}
				else
				{
					var definedCount = definedQueryMethod.Parameters.Count;
					var argumentsCount = onMember.Arguments.Count;
					var diff = definedCount - argumentsCount;
					for (int i = definedCount - 1; i >= diff; i--)
					{
						parameterMatch.Add(definedQueryMethod.Parameters[i], onMember.Arguments[i - diff]);
					}
				}

				var body = definedQueryMethod.Body.Transform(parameterMatch, static (parameterMatch, e) =>
				{
					if (e.NodeType == ExpressionType.Parameter &&
					    parameterMatch.TryGetValue((ParameterExpression)e, out var newExpression))
					{
						return newExpression;
					}

					return e;
				});

				definedQueryMethod = Expression.Lambda(body, definedQueryMethod.Parameters[0]);
			}

			var shouldAddDefaultIfEmpty = enforceDefault == true;

			if (definedQueryMethod == null)
			{
				var parentParam = Expression.Parameter(parentType, "parent");
				var childParam  = Expression.Parameter(objectType, association.GenerateAlias());

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
					shouldAddDefaultIfEmpty = shouldAddDefaultIfEmpty || (association.CanBeNull && inline);
					shouldAddCacheCheck     = true;

					var replacedBody = expressionPredicate.GetBody(parentParam, childParam);

					predicate = predicate == null ? replacedBody : Expression.AndAlso(predicate, replacedBody);
				}

				if (predicate == null)
					throw new LinqException("Can not generate Association predicate");

				if (inline && !shouldAddDefaultIfEmpty)
				{
					var ed = builder.MappingSchema.GetEntityDescriptor(objectType, builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated);
					if (ed.QueryFilterFunc != null)
					{
						shouldAddDefaultIfEmpty = true;
						shouldAddCacheCheck     = true;
					}
				}

				var queryParam = Expression.Call(Methods.LinqToDB.GetTable.MakeGenericMethod(objectType), dataContextExpr);

				if (additionalCondition != null)
				{
					predicate = Expression.AndAlso(additionalCondition, predicate);
				}

				var filterLambda = Expression.Lambda(predicate, childParam);
				Expression body  = Expression.Call(Methods.Queryable.Where.MakeGenericMethod(objectType), queryParam,
					Expression.Quote(filterLambda));

				definedQueryMethod = Expression.Lambda(body, parentParam);
			}
			else
			{
				shouldAddDefaultIfEmpty = true;
				var bodyExpression = definedQueryMethod.Body.Unwrap();
				if (bodyExpression.NodeType == ExpressionType.Call)
				{
					var mc = (MethodCallExpression)bodyExpression;
					if (mc.IsSameGenericMethod(DefaultIfEmptyMethods))
						shouldAddDefaultIfEmpty = false;
				}

				if (additionalCondition != null)
				{
					var newBody          = definedQueryMethod.Body;
					var objParam         = Expression.Parameter(objectType);
					var additionalLambda = Expression.Lambda(additionalCondition, objParam);
					if (typeof(IQueryable<>).IsSameOrParentOf(definedQueryMethod.Body.Type))
					{
						newBody = Expression.Call(Methods.Queryable.Where.MakeGenericMethod(objectType),
							newBody,
							Expression.Quote(additionalLambda));
					}
					else
					{
						newBody = Expression.Call(Methods.Enumerable.Where.MakeGenericMethod(objectType), 
							newBody,
							additionalLambda);
					}
					definedQueryMethod = Expression.Lambda(newBody, definedQueryMethod.Parameters);
				}

			}

			if (!cacheCheckAdded && shouldAddCacheCheck)
			{
				// here we tell for Expression Comparer to compare optimized Association expressions
				//
				var closureExpr    = definedQueryMethod;
				definedQueryMethod = (LambdaExpression)builder.AddQueryableMemberAccessors(closureExpr, onMember, builder.DataContext, static (closureExpr, mi, dc) =>
				{
					var optimizationContext = new ExpressionTreeOptimizationContext(dc);
					var optimizedExpr = ExpressionBuilder.ExposeExpression(closureExpr, dc, optimizationContext, optimizeConditions : true, compactBinary : true);
					    optimizedExpr       = optimizedExpr.OptimizeExpression(dc.MappingSchema)!;
					return optimizedExpr;
				});
			}

			if (loadWith != null)
			{
				var newPath = new[] { association.MemberInfo };
				var path = loadWithPath == null || loadWithPath.Length == 0
					? newPath
					: loadWithPath.Concat(newPath).ToArray();

				var body = definedQueryMethod.Body;

				body = Expression.Call(
					Methods.LinqToDB.LoadWithInternal.MakeGenericMethod(body.Type),
					body,
					Expression.Constant(loadWith),
					Expression.Constant(path, typeof(MemberInfo[])));

				definedQueryMethod = Expression.Lambda(body, definedQueryMethod.Parameters);
			}

			if (loadWith?.NextInfos != null)
			{
				var associationLoadWith = loadWith.NextInfos
					.FirstOrDefault(li =>
						MemberInfoEqualityComparer.Default.Equals(li.MemberInfo, association.MemberInfo));

				associationLoadWith ??= loadWith.NextInfos
					.FirstOrDefault(li =>
						li.MemberInfo?.Name == association.MemberInfo.Name);

				if (associationLoadWith != null &&
				    (associationLoadWith.MemberFilter != null || associationLoadWith.FilterFunc != null))
				{
					var body = definedQueryMethod.Body.Unwrap();

					var memberFilter = associationLoadWith.MemberFilter;
					if (memberFilter != null)
					{
						var elementType = EagerLoading.GetEnumerableElementType(memberFilter.Parameters[0].Type,
							builder.MappingSchema);
						var filtered   = Expression.Convert(body, typeof(IEnumerable<>).MakeGenericType(elementType));
						var filterBody = memberFilter.GetBody(filtered);
						body = Expression.Call(
							Methods.Queryable.AsQueryable.MakeGenericMethod(objectType), filterBody);
					}

					var loadWithFunc = associationLoadWith.FilterFunc;

					if (loadWithFunc != null)
					{
						loadWithFunc = loadWithFunc.Unwrap();
						if (loadWithFunc is LambdaExpression lambda)
						{
							body = lambda.GetBody(body);
						}
						else
						{
							var filterDelegate = builder.EvaluateExpression<Delegate>(loadWithFunc) ??
							                     throw new LinqException($"Cannot convert filter function '{loadWithFunc}' to Delegate.");

							var argumentType = filterDelegate.GetType().GetGenericArguments()[0].GetGenericArguments()[0];
							// check for fake argument q => q
							if (argumentType.IsSameOrParentOf(objectType))
							{

								var query = ExpressionQueryImpl.CreateQuery(objectType, builder.DataContext, body);
								var filtered = (IQueryable)filterDelegate.DynamicInvoke(query)!;
								body = filtered.Expression;
							}
						}
					}

					definedQueryMethod = Expression.Lambda(body, definedQueryMethod.Parameters);

				}
			}

			if (parentOriginalType != parentType)
			{
				// add discriminator filter
				var ed = builder.MappingSchema.GetEntityDescriptor(parentOriginalType, builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated);
				foreach (var inheritanceMapping in ed.InheritanceMapping)
				{
					if (inheritanceMapping.Type == parentType)
					{
						var objParam     = Expression.Parameter(objectType, "o");
						var filterLambda = Expression.Lambda(ExpressionBuilder.Equal(builder.MappingSchema,
							Expression.MakeMemberAccess(definedQueryMethod.Parameters[0], inheritanceMapping.Discriminator.MemberInfo),
							Expression.Constant(inheritanceMapping.Code)), objParam);

						var body = definedQueryMethod.Body.Unwrap();
						body = Expression.Call(Methods.Queryable.Where.MakeGenericMethod(objectType),
							body, Expression.Quote(filterLambda));
						definedQueryMethod = Expression.Lambda(body, definedQueryMethod.Parameters);

						shouldAddDefaultIfEmpty = true;
						break;
					}
				}
			}

			if (enforceDefault == false)
			{
				shouldAddDefaultIfEmpty = false;
			}

			if (inline)
			{
				var body = definedQueryMethod.Body.Unwrap();
				body = Expression.Call(
					(shouldAddDefaultIfEmpty ? Methods.Queryable.SingleOrDefault : Methods.Queryable.Single)
					.MakeGenericMethod(objectType), body);

				definedQueryMethod = Expression.Lambda(body, definedQueryMethod.Parameters);
				isOuter = true;
			}
			else
			{
				if (shouldAddDefaultIfEmpty)
				{
					var body = definedQueryMethod.Body.Unwrap();

					body = Expression.Call(
						(typeof(IQueryable<>).IsSameOrParentOf(body.Type)
							? Methods.Queryable.DefaultIfEmpty
							: Methods.Enumerable.DefaultIfEmpty).MakeGenericMethod(objectType), body);

					definedQueryMethod = Expression.Lambda(body, definedQueryMethod.Parameters);
					isOuter = true;
				}
				else
				{
					isOuter = false;
				}
			}

			definedQueryMethod = (LambdaExpression)builder.ConvertExpressionTree(definedQueryMethod);
			definedQueryMethod = (LambdaExpression)definedQueryMethod.OptimizeExpression(builder.MappingSchema)!;

			return definedQueryMethod;
		}

		public static Expression BuildAssociationQuery(ExpressionBuilder builder, ContextRefExpression tableContext, 
			AccessorMember onMember, AssociationDescriptor descriptor, Expression? additionalCondition, bool inline, LoadWithInfo? loadwith, MemberInfo[]? loadWithPath, ref bool? isOuter)
		{
			var elementType     = descriptor.GetElementType(builder.MappingSchema);
			var parentExactType = descriptor.GetParentElementType();

			var queryMethod = CreateAssociationQueryLambda(
				builder, tableContext.BuildContext.MappingSchema, onMember, descriptor, elementType /*tableContext.OriginalType*/, parentExactType, elementType,
				additionalCondition,
				inline, isOuter, loadwith, loadWithPath, out isOuter);

			var correctedContext = tableContext.WithType(parentExactType);

			var body = queryMethod.GetBody(correctedContext);

			return body;
		}

	}

}
