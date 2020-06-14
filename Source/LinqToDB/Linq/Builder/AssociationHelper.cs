using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;
	using Mapping;
	using Reflection;

	static class AssociationHelper
	{
		// Returns
		// (ParentType p) => dc.GetTable<ObjectType>().Where(...)
		// (ParentType p) => dc.GetTable<ObjectType>().Where(...).DefaultIfEmpty
		public static LambdaExpression CreateAssociationQueryLambda(ExpressionBuilder builder, AccessorMember onMember, AssociationDescriptor association, 
			Type parentOriginalType,
			Type parentType, 
			Type objectType, bool inline, bool enforceDefault, 
			List<LoadWithInfo[]>? loadWith, out bool isLeft)
		{
			var dataContextConstant = Expression.Constant(builder.DataContext, builder.DataContext.GetType());

			// We are trying to keep fast cache hit behaviour, so cache check should be added only if needed
			//
			bool shouldAddCacheCheck = false;

			bool cacheCheckAdded = false;

			LambdaExpression? definedQueryMethod  = null;
			if (association.HasQueryMethod())
			{
				// here we tell for Expression Comparer to compare optimized Association expressions
				//
				definedQueryMethod = (LambdaExpression)builder.AddQueryableMemberAccessors(onMember, builder.DataContext, (mi, dc) =>
				{
					var queryLambda         = association.GetQueryMethod(parentType, objectType) ?? throw new InvalidOperationException();
					var optimizationContext = new ExpressionTreeOptimizationContext(dc);
					var optimizedExpr       = optimizationContext.ExposeExpression(queryLambda);
					    optimizedExpr       = optimizationContext.ExpandQueryableMethods(optimizedExpr);
					    optimizedExpr       = optimizedExpr.OptimizeExpression()!;
					return optimizedExpr;
				});

				cacheCheckAdded = true;

				var parameterMatch = new Dictionary<ParameterExpression, Expression>();
				if (onMember.Arguments == null)
				{
					if (definedQueryMethod.Parameters.Count > 1 && typeof(IDataContext).IsSameOrParentOf(definedQueryMethod.Parameters[1].Type))
						parameterMatch.Add(definedQueryMethod.Parameters[1], dataContextConstant);
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

				var body = definedQueryMethod.Body.Transform(e =>
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

			var shouldAddDefaultIfEmpty = enforceDefault;

			if (definedQueryMethod == null)
			{
				var parentParam = Expression.Parameter(parentType, "parent");
				var childParam  = Expression.Parameter(objectType, association.AliasName);

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
					shouldAddCacheCheck = true;

					var replacedBody = expressionPredicate.GetBody(parentParam, childParam);

					predicate = predicate == null ? replacedBody : Expression.AndAlso(predicate, replacedBody);
				}
				
				if (predicate == null)
					throw new LinqException("Can not generate Association predicate");

				if (inline && !shouldAddDefaultIfEmpty)
				{
					var ed = builder.MappingSchema.GetEntityDescriptor(objectType);
					if (ed.QueryFilterFunc != null)
					{
						shouldAddDefaultIfEmpty = true;
						shouldAddCacheCheck = true;
					}
				}

				var queryParam = Expression.Call(Methods.LinqToDB.GetTable.MakeGenericMethod(objectType), dataContextConstant);

				var filterLambda = Expression.Lambda(predicate, childParam);
				Expression body  = Expression.Call(Methods.Queryable.Where.MakeGenericMethod(objectType), queryParam,
					filterLambda);

				definedQueryMethod = Expression.Lambda(body, parentParam);
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

			if (!cacheCheckAdded && shouldAddCacheCheck)
			{
				// here we tell for Expression Comparer to compare optimized Association expressions
				//
				var closureExpr    = definedQueryMethod;
				definedQueryMethod = (LambdaExpression)builder.AddQueryableMemberAccessors(onMember, builder.DataContext, (mi, dc) =>
				{
					var optimizationContext = new ExpressionTreeOptimizationContext(dc);
					var optimizedExpr       = optimizationContext.ExposeExpression(closureExpr);
					    optimizedExpr       = optimizationContext.ExpandQueryableMethods(optimizedExpr);
					    optimizedExpr       = optimizedExpr.OptimizeExpression()!;
					return optimizedExpr;
				});
			}

			if (loadWith != null)
			{
				var associationLoadWith = GetLoadWith(loadWith)?
					.FirstOrDefault(li => li.Info.MemberInfo == association.MemberInfo);

				if (associationLoadWith != null &&
				    (associationLoadWith.Info.MemberFilter != null || associationLoadWith.Info.FilterFunc != null))
				{
					var body = definedQueryMethod.Body.Unwrap();

					var memberFilter = associationLoadWith.Info.MemberFilter;
					if (memberFilter != null)
					{
						var elementType = EagerLoading.GetEnumerableElementType(memberFilter.Parameters[0].Type,
							builder.MappingSchema);
						var filtered   = Expression.Convert(body, typeof(IEnumerable<>).MakeGenericType(elementType));
						var filterBody = memberFilter.GetBody(filtered);
						body = Expression.Call(
							Methods.Queryable.AsQueryable.MakeGenericMethod(objectType), filterBody);
					}

					var loadWithFunc = associationLoadWith.Info.FilterFunc;
						
					if (loadWithFunc != null)
					{
						loadWithFunc = loadWithFunc.Unwrap();
						if (loadWithFunc is LambdaExpression lambda)
						{
							body = lambda.GetBody(body);
						}
						else
						{
							var filterDelegate = loadWithFunc.EvaluateExpression<Delegate>() ??
							                     throw new LinqException("Cannot convert filter function '{loadWithFunc}' to Delegate.");

							var arumentType = filterDelegate.GetType().GetGenericArguments()[0].GetGenericArguments()[0];
							// check for fake argument q => q
							if (arumentType.IsSameOrParentOf(objectType))
							{
								
								var query = ExpressionQueryImpl.CreateQuery(objectType, builder.DataContext, body);
								var filtered = (IQueryable)filterDelegate.DynamicInvoke(query);
								body = filtered.Expression;
							}
						}
					}

					definedQueryMethod = Expression.Lambda(body, definedQueryMethod.Parameters);

				}

				if (associationLoadWith?.NextLoadWith != null && associationLoadWith.NextLoadWith.Count > 0)
				{
					definedQueryMethod = (LambdaExpression)EnrichTablesWithLoadWith(builder.DataContext, definedQueryMethod, objectType,
						associationLoadWith.NextLoadWith, builder.MappingSchema);
				}

			}

			if (parentOriginalType != parentType)
			{
				// add discriminator filter
				var ed = builder.MappingSchema.GetEntityDescriptor(parentOriginalType);
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
							body, filterLambda);
						definedQueryMethod = Expression.Lambda(body, definedQueryMethod.Parameters);

						shouldAddDefaultIfEmpty = true;
						break;
					}
				}
			}

			if (inline && shouldAddDefaultIfEmpty)
			{
				var body = definedQueryMethod.Body.Unwrap();
				body = Expression.Call(Methods.Queryable.DefaultIfEmpty.MakeGenericMethod(objectType), body);
				definedQueryMethod = Expression.Lambda(body, definedQueryMethod.Parameters);
				isLeft = true;
			}
			else
			{
				isLeft = false;
			}

			definedQueryMethod = (LambdaExpression)builder.ConvertExpressionTree(definedQueryMethod);
			definedQueryMethod = (LambdaExpression)builder.ConvertExpression(definedQueryMethod);
			definedQueryMethod = (LambdaExpression)definedQueryMethod.OptimizeExpression()!;

			return definedQueryMethod;
		}

		public static IBuildContext BuildAssociationInline(ExpressionBuilder builder, BuildInfo buildInfo, TableBuilder.TableContext tableContext, 
			AccessorMember onMember, AssociationDescriptor descriptor, bool inline, ref bool isOuter)
		{
			var elementType     = descriptor.GetElementType(builder.MappingSchema);
			var parentExactType = descriptor.GetParentElementType();
			
			var queryMethod = CreateAssociationQueryLambda(
				builder, onMember, descriptor, tableContext.OriginalType, parentExactType, elementType,
				inline, isOuter, tableContext.LoadWith, out isOuter);

			var parentRef   = new ContextRefExpression(queryMethod.Parameters[0].Type, tableContext);
			var body = queryMethod.GetBody(parentRef);

			var context = builder.BuildSequence(new BuildInfo(tableContext, body, new SelectQuery()));

			var tableSource = tableContext.SelectQuery.From.Tables.First();
			var join = new SqlFromClause.Join(isOuter ? JoinType.OuterApply : JoinType.CrossApply, context.SelectQuery,
				descriptor.GenerateAlias(), true, null);

			tableSource.Joins.Add(join.JoinedTable);
			
			return new AssociationContext(builder, descriptor, tableContext, context, join.JoinedTable);
		}

		public static IBuildContext BuildAssociationSelectMany(ExpressionBuilder builder, BuildInfo buildInfo, TableBuilder.TableContext tableContext, 
			AccessorMember onMember, AssociationDescriptor descriptor, ref bool isOuter)
		{
			var elementType = descriptor.GetElementType(builder.MappingSchema);

			var queryMethod = CreateAssociationQueryLambda(
				builder, onMember, descriptor, tableContext.OriginalType, tableContext.ObjectType, elementType,
				false, isOuter, tableContext.LoadWith, out isOuter);

			var parentRef   = new ContextRefExpression(queryMethod.Parameters[0].Type, tableContext);
			var body = queryMethod.GetBody(parentRef);

			IBuildContext context;

			context = builder.BuildSequence(new BuildInfo(buildInfo, body));
			context.SelectQuery.From.Tables[0].Alias = descriptor.GenerateAlias();

			return context;
		}

		public static Expression EnrichTablesWithLoadWith(IDataContext dataContext, Expression expression, Type entityType, List<LoadWithInfo[]> loadWith, MappingSchema mappingSchema)
		{
			var tableType     = typeof(ITable<>).MakeGenericType(entityType);
			var newExpression = expression.Transform(e =>
			{
				if (e.NodeType == ExpressionType.Call)
				{
					var mc = (MethodCallExpression)e;
					if (mc.IsQueryable("GetTable") && tableType.IsSameOrParentOf(mc.Type))
					{
						e = EnrichLoadWith(dataContext, mc, entityType, loadWith, mappingSchema);
					}
				}

				return e;
			});

			return newExpression;
		}

		public static Expression EnrichLoadWith(IDataContext dataContext, Expression table, Type entityType, List<LoadWithInfo[]> loadWith, MappingSchema mappingSchema)
		{
			var args = new List<Expression>(2);
			var currentObj = table;
			foreach (var members in loadWith)
			{
				var currentEntityType = entityType;
				var isPrevEnumerable = false;
				Type? prevMemberType = null;

				foreach (var member in members)
				{
					args.Clear();
					args.Add(currentObj);

					var memberType = member.MemberInfo.GetMemberType();
					var isEnumerableMember =
						EagerLoading.IsEnumerableType(memberType, mappingSchema);

					var desiredType = member.MemberInfo.IsMethodEx() ? currentEntityType : member.MemberInfo.DeclaringType;

					var entityParam = Expression.Parameter(currentEntityType, "e");
					var loadBody    = desiredType == currentEntityType
						? (Expression)entityParam
						: Expression.Convert(entityParam, desiredType);

					loadBody = Expression.MakeMemberAccess(loadBody, member.MemberInfo);
					if (member.MemberFilter != null)
						loadBody = member.MemberFilter.GetBody(loadBody);

					var hasFilterFunc = member.FilterFunc != null;

					if (isEnumerableMember && hasFilterFunc)
					{
						var propType = EagerLoading.GetEnumerableElementType(memberType, mappingSchema);
						var enumerableType = typeof(IEnumerable<>).MakeGenericType(propType);
						if (loadBody.Type != enumerableType)
							loadBody = Expression.Convert(loadBody, enumerableType);
					}

					args.Add(Expression.Quote(Expression.Lambda(loadBody, entityParam)));

					if (hasFilterFunc)
						args.Add(member.FilterFunc!);

					MethodInfo method;
					if (prevMemberType == null)
					{
						method = !hasFilterFunc
							? Methods.LinqToDB.LoadWith
							: isEnumerableMember
								? Methods.LinqToDB.LoadWithManyFilter
								: Methods.LinqToDB.LoadWithSingleFilter;


						var propType = memberType;
						if (hasFilterFunc && isEnumerableMember)
						{
							propType = EagerLoading.GetEnumerableElementType(propType, mappingSchema);
						}
						method = method.MakeGenericMethod(entityType, propType);
					}
					else
					{
						if (isPrevEnumerable)
						{
							if (!hasFilterFunc)
								method = Methods.LinqToDB.ThenLoadFromMany;
							else if (isEnumerableMember)
								method = Methods.LinqToDB.ThenLoadFromManyManyFilter;
							else
								method = Methods.LinqToDB.ThenLoadFromManySingleFilter;
						}
						else
						{
							if (!hasFilterFunc)
								method = Methods.LinqToDB.ThenLoadFromSingle;
							else if (isEnumerableMember)
								method = Methods.LinqToDB.ThenLoadFromSingleManyFilter;
							else
								method = Methods.LinqToDB.ThenLoadFromSingleSingleFilter;
						}

						var propType = memberType;
						if (hasFilterFunc && isEnumerableMember)
						{
							propType = EagerLoading.GetEnumerableElementType(propType, mappingSchema);
						}						
						method = method.MakeGenericMethod(entityType, prevMemberType, propType);
					}

					currentObj = Expression.Call(method, args);
					
					isPrevEnumerable  = isEnumerableMember && !hasFilterFunc;

					if (isEnumerableMember)
						memberType = EagerLoading.GetEnumerableElementType(memberType, mappingSchema);

					prevMemberType    = memberType;
					currentEntityType = memberType;
				}

			}

			return currentObj;
		}

		public static Delegate? GetLoadWithFunc(List<LoadWithInfo[]>? loadWith, MemberInfo memberInfo)
		{
			Delegate? loadWithFunc = null;
			if (loadWith != null)
			{
				loadWithFunc = GetLoadWith(loadWith)?
					.FirstOrDefault(li => li.Info.MemberInfo == memberInfo)?.Info.FilterFunc?.EvaluateExpression() as Delegate;
			}

			return loadWithFunc;
		}

		public class LoadWithItem
		{
			public LoadWithInfo Info  = null!;
			public List<LoadWithInfo[]> NextLoadWith = null!;
		}


		public static List<LoadWithItem> GetLoadWith(List<LoadWithInfo[]> infos)
		{
			var result =
			(
				from lw in infos
				select new
				{
					head = lw.First(),
					tail = lw.Skip(1).ToArray()
				}
				into info
				group info by info.head into gr
				select new LoadWithItem
				{
					Info = gr.Key,
					NextLoadWith = (from i in gr where i.tail.Length > 0 select i.tail).ToList()
				}
			).ToList();

			return result;
		}

	}

}
