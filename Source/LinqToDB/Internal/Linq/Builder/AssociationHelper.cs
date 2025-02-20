using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB;
using LinqToDB.Common.Internal;
using LinqToDB.Extensions;
using LinqToDB.Interceptors;
using LinqToDB.Internal.Expressions;
using LinqToDB.Mapping;
using LinqToDB.Reflection;

namespace LinqToDB.Internal.Linq.Builder
{
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
			TranslationModifier   modifier,
			LoadWithInfo?         loadWith,
			MemberInfo[]?         loadWithPath,
			out bool?             isOptional)
		{
			Expression dataContextExpr = SqlQueryRootExpression.Create(mappingSchema, builder.DataContext.GetType());

			// We are trying to keep fast cache hit behaviour, so cache check should be added only if needed
			//
			bool shouldAddCacheCheck = false;

			bool cacheCheckAdded = false;

			LambdaExpression? definedQueryMethod  = null;
			if (association.HasQueryMethod())
			{
				// Closure should handle only association, objectType and parentType.
				// Here we tell for EqualsToVisitor to compare optimized Association expressions
				definedQueryMethod = (LambdaExpression)builder.ParametersContext.RegisterDynamicExpressionAccessor(Expression.Constant(association), builder.DataContext, mappingSchema, (dc, _) =>
				{
					var associationExpression = association.GetQueryMethod(parentType, objectType) ?? throw new InvalidOperationException();

					if (dc is IInterceptable<IQueryExpressionInterceptor> { Interceptor: { } interceptor })
					{
						associationExpression = (LambdaExpression)interceptor.ProcessExpression(associationExpression,
							new QueryExpressionArgs(dc, associationExpression, QueryExpressionArgs.ExpressionKind.AssociationExpression));
					}

					var optimizationContext = new ExpressionTreeOptimizationContext(dc);
					associationExpression = (LambdaExpression)ExpressionBuilder.ExposeExpression(associationExpression, dc, optimizationContext, null, optimizeConditions : true, compactBinary : true);
					return associationExpression;
				});

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

				var parentMembers = GetMemberAccessors(parentType, mappingSchema);
				var parentOriginalMembers = GetMemberAccessors(parentOriginalType, mappingSchema);
				var childMembers = GetMemberAccessors(objectType, mappingSchema);

				Expression? predicate = null;
				for (var i = 0; i < association.ThisKey.Length; i++)
				{
					var parentName   = association.ThisKey[i];
					var parentMember = parentMembers.Find(m => m.MemberInfo.Name == parentName);
					var currentParentParam = (Expression)parentParam;

					if (parentMember == null)
					{
						parentMember = parentOriginalMembers.Find(m => m.MemberInfo.Name == parentName);
						currentParentParam = Expression.Convert(currentParentParam, parentOriginalType);
					}

					if (parentMember == null)
						throw new LinqToDBException($"Association key '{parentName}' not found for type '{parentType}.");

					var childName = association.OtherKey[i];
					var childMember = childMembers.Find(m => m.MemberInfo.Name == childName);

					if (childMember == null)
						throw new LinqToDBException($"Association key '{childName}' not found for type '{objectType}.");

					var current = ExpressionBuilder.Equal(builder.MappingSchema,
						Expression.MakeMemberAccess(currentParentParam, parentMember.MemberInfo),
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
					throw new LinqToDBException("Cannot generate Association predicate");

				if (inline && !shouldAddDefaultIfEmpty)
				{
					var ed = builder.MappingSchema.GetEntityDescriptor(objectType, builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated);
					if (ed.QueryFilterLambda != null)
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
				definedQueryMethod = (LambdaExpression)builder.ParametersContext.RegisterDynamicExpressionAccessor(closureExpr, builder.DataContext, mappingSchema, (dc, ms) =>
				{
					var optimizationContext = new ExpressionTreeOptimizationContext(dc);
					var optimizedExpr       = ExpressionBuilder.ExposeExpression(closureExpr, dc, optimizationContext, null, optimizeConditions : true, compactBinary : true);
					optimizedExpr = optimizedExpr.OptimizeExpression(dc.MappingSchema);
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
												 throw new LinqToDBException($"Cannot convert filter function '{loadWithFunc}' to Delegate.");

							var argumentType = filterDelegate.GetType().GetGenericArguments()[0].GetGenericArguments()[0];
							// check for fake argument q => q
							if (argumentType.IsSameOrParentOf(objectType))
							{

								var query    = ExpressionQueryImpl.CreateQuery(objectType, builder.DataContext, body);
								var filtered = filterDelegate.DynamicInvokeExt<IQueryable>(query);
								body         = filtered.Expression;
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

			isOptional = shouldAddDefaultIfEmpty;

			var bodyWithModifier = Expression.Call(null, Methods.LinqToDB.ApplyModifierInternal.MakeGenericMethod(objectType), definedQueryMethod.Body, Expression.Constant(modifier));
			definedQueryMethod = Expression.Lambda(bodyWithModifier, definedQueryMethod.Parameters);

			if (inline)
			{
				var body = definedQueryMethod.Body.Unwrap();
				body = Expression.Call(
					(shouldAddDefaultIfEmpty ? Methods.Queryable.SingleOrDefault : Methods.Queryable.Single)
					.MakeGenericMethod(objectType), body);

				definedQueryMethod = Expression.Lambda(body, definedQueryMethod.Parameters);
			}

			definedQueryMethod = (LambdaExpression)builder.ConvertExpressionTree(definedQueryMethod);
			definedQueryMethod = (LambdaExpression)definedQueryMethod.OptimizeExpression(builder.MappingSchema)!;

			return definedQueryMethod;
		}

		public static Expression BuildAssociationQuery(ExpressionBuilder builder, ContextRefExpression tableContext,
			AccessorMember onMember, AssociationDescriptor descriptor, Expression? additionalCondition, bool inline, TranslationModifier modifier, LoadWithInfo? loadwith, MemberInfo[]? loadWithPath,
			ref bool? isOptional)
		{
			var elementType     = descriptor.GetElementType();
			var parentExactType = descriptor.GetParentElementType();

			var queryMethod = CreateAssociationQueryLambda(
				builder, tableContext.BuildContext.MappingSchema, onMember, descriptor, elementType /*tableContext.OriginalType*/, parentExactType, elementType,
				additionalCondition,
				inline, isOptional, modifier, loadwith, loadWithPath, out isOptional);

			var correctedContext = tableContext.WithType(queryMethod.Parameters[0].Type);

			var body = queryMethod.GetBody(correctedContext);

			return body;
		}

		static List<MemberAccessor> GetMemberAccessors(Type type, MappingSchema mappingSchema)
		{
			var typeAccessor = TypeAccessor.GetAccessor(type);

			var dynamicColumnAccessors = mappingSchema.GetDynamicColumns(type)
				.Select(it => new MemberAccessor(typeAccessor, it, mappingSchema.GetEntityDescriptor(type)));

			return typeAccessor.Members.Concat(dynamicColumnAccessors).ToList();
		}
	}
}
