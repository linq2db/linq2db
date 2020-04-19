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
		public static LambdaExpression CreateAssociationQueryLambda(ExpressionBuilder builder, AssociationDescriptor association, 
			Type parentOriginalType,
			Type parentType, 
			Type objectType, bool inline, bool enforceDefault, 
			List<Tuple<MemberInfo, Expression?>[]>? loadWith, out bool isLeft)
		{
			var dataContextConstant = Expression.Constant(builder.DataContext, builder.DataContext.GetType());

			var definedQueryMethod  = association.GetQueryMethod(parentType, objectType);
			if (definedQueryMethod != null)
			{
				var body = definedQueryMethod.GetBody(definedQueryMethod.Parameters[0], dataContextConstant);
				definedQueryMethod = Expression.Lambda(body, definedQueryMethod.Parameters[0]);
			}

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

			if (loadWith != null)
			{
				var associationLoadWith = GetLoadWith(loadWith)?
					.FirstOrDefault(li => li.MemberInfo == association.MemberInfo);

				if (associationLoadWith != null)
				{
					var loadWithFunc = associationLoadWith.Filter?.EvaluateExpression() as Delegate;

					if (loadWithFunc != null)
					{
						var body = definedQueryMethod.Body.Unwrap();
						var childTableType = typeof(ITable<>).MakeGenericType(objectType);
						body = body.Transform(e =>
						{
							if (e.NodeType == ExpressionType.Call)
							{
								var mc = (MethodCallExpression)e;
								if (mc.IsSameGenericMethod(Methods.LinqToDB.GetTable) && mc.Type == childTableType)
								{
									var filtered = (IQueryable)loadWithFunc.DynamicInvoke(mc.EvaluateExpression());
									e = filtered.Expression;
								}
							}

							return e;
						});

						definedQueryMethod = Expression.Lambda(body, definedQueryMethod.Parameters);
					}

					if (associationLoadWith.NextLoadWith != null)
					{
						definedQueryMethod = (LambdaExpression)EnrichTablesWithLoadWith(definedQueryMethod, objectType,
							associationLoadWith.NextLoadWith, builder.MappingSchema);
					}
					
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

		public static AssociationContext BuildAssociationInline(ExpressionBuilder builder, BuildInfo buildInfo, TableBuilder.TableContext tableContext, AssociationDescriptor descriptor, bool inline, ref bool isOuter)
		{
			var elementType     = descriptor.GetElementType(builder.MappingSchema);
			var parentExactType = descriptor.GetParentElementType();
			
			var queryMethod = CreateAssociationQueryLambda(
				builder, descriptor, tableContext.OriginalType, parentExactType, elementType,
				inline, isOuter, tableContext.LoadWith, out isOuter);

			var parentRef   = new ContextRefExpression(queryMethod.Parameters[0].Type, tableContext);
			var body = queryMethod.GetBody(parentRef);

			var context = builder.BuildSequence(new BuildInfo(buildInfo, body, new SelectQuery()));

			var tableSource = buildInfo.SelectQuery.From.Tables.Last();
			var join = new SqlFromClause.Join(isOuter ? JoinType.OuterApply : JoinType.CrossApply, context.SelectQuery,
				null, inline, null);

			tableSource.Joins.Add(join.JoinedTable);
			
			return new AssociationContext(builder, tableContext, context);
		}

		public static IBuildContext BuildAssociationSubqueryInline(ExpressionBuilder builder, BuildInfo buildInfo, TableBuilder.TableContext tableContext, AssociationDescriptor descriptor, ref bool isOuter)
		{
			if (buildInfo.Parent == null)
				throw new InvalidOperationException();

			var elementType     = descriptor.GetElementType(builder.MappingSchema);
			var parentExactType = descriptor.GetParentElementType();
			
			var queryMethod = CreateAssociationQueryLambda(
				builder, descriptor, tableContext.OriginalType, parentExactType, elementType,
				false, isOuter, tableContext.LoadWith, out isOuter);

			var parentRef   = new ContextRefExpression(queryMethod.Parameters[0].Type, buildInfo.Parent);
			var body = queryMethod.GetBody(parentRef);

			var context = builder.BuildSequence(new BuildInfo(buildInfo, body, new SelectQuery()));

			if (buildInfo.SelectQuery.From.Tables.Count == 0)
			{
				buildInfo.SelectQuery.From.Table(context.SelectQuery);
			}
			else
			{
				var tableSource = buildInfo.SelectQuery.From.Tables.Last();
				var join = new SqlFromClause.Join(isOuter ? JoinType.OuterApply : JoinType.CrossApply,
					context.SelectQuery,
					null, false, null);

				tableSource.Joins.Add(join.JoinedTable);
			}			

			return buildInfo.Parent;
		}

		public static IBuildContext BuildAssociationSelectMany1(ExpressionBuilder builder, BuildInfo buildInfo, TableBuilder.TableContext tableContext, AssociationDescriptor descriptor, ref bool isOuter)
		{
			var elementType = descriptor.GetElementType(builder.MappingSchema);
			var parentRef   = new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(tableContext.ObjectType), tableContext);

			var queryMethod = CreateAssociationQueryLambda(
				builder, descriptor, tableContext.OriginalType, tableContext.ObjectType, elementType,
				true, isOuter, tableContext.LoadWith, out isOuter);

			var selectManyMethod = GetAssociationQueryExpression(
				queryMethod.Parameters[0], tableContext.ObjectType, elementType, parentRef, queryMethod, builder.MappingSchema);

			var saveParent = tableContext.Parent;
			// var context = builder.BuildSequence(new BuildInfo(tableContext, selectManyMethod, buildInfo.SelectQuery));
			var context = builder.BuildSequence(new BuildInfo(buildInfo, selectManyMethod, buildInfo.SelectQuery));

			builder.ReplaceParent(tableContext, context);

			return context;
		}


		public static IBuildContext BuildAssociationSelectMany(ExpressionBuilder builder, BuildInfo buildInfo, TableBuilder.TableContext tableContext, AssociationDescriptor descriptor, ref bool isOuter)
		{
			var elementType = descriptor.GetElementType(builder.MappingSchema);

			var queryMethod = CreateAssociationQueryLambda(
				builder, descriptor, tableContext.OriginalType, tableContext.ObjectType, elementType,
				false, isOuter, tableContext.LoadWith, out isOuter);

			var parentRef   = new ContextRefExpression(queryMethod.Parameters[0].Type, tableContext);
			var body = queryMethod.GetBody(parentRef);

			IBuildContext context;

			// if (buildInfo.SelectQuery.From.Tables.Count > 0 && buildInfo.Parent != null)
			// {
			// 	context = builder.BuildSequence(new BuildInfo(buildInfo, body, new SelectQuery()));
			//
			// 	var tableSource = buildInfo.SelectQuery.From.Tables.Last();
			// 	var join = new SqlFromClause.Join(isOuter ? JoinType.OuterApply : JoinType.CrossApply, context.SelectQuery,
			// 		null, false, null);
			// 	tableSource.Joins.Add(join.JoinedTable);
			// 	context = new AssociationContext(builder, buildInfo.Parent, context);
			// }
			// else
			{
				context = builder.BuildSequence(new BuildInfo(buildInfo, body));
			}

			return context;
		}

		public static IBuildContext BuildAssociationSubquery(ExpressionBuilder builder, BuildInfo buildInfo, TableBuilder.TableContext tableContext, AssociationDescriptor descriptor)
		{
			var elementType = descriptor.GetElementType(builder.MappingSchema);

			var queryLambda = CreateAssociationQueryLambda(
				builder, descriptor, tableContext.OriginalType, tableContext.ObjectType, elementType,
				true, false, tableContext.LoadWith, out _);


			var parentRef    = new ContextRefExpression(tableContext.ObjectType, tableContext);
			var expr         = queryLambda.GetBody(parentRef);

			var info = new BuildInfo(buildInfo, expr, buildInfo.SelectQuery);

			var context = builder.BuildSequence(info);
			return context;
		}

		public static IBuildContext BuildAssociationSubquery2(ExpressionBuilder builder, BuildInfo buildInfo, TableBuilder.TableContext tableContext, AssociationDescriptor descriptor, ref bool isOuter)
		{
			var elementType = descriptor.GetElementType(builder.MappingSchema);

			var queryLambda = CreateAssociationQueryLambda(
				builder, descriptor, tableContext.OriginalType, tableContext.ObjectType, elementType,
				false, isOuter, tableContext.LoadWith, out isOuter);


			var parentRef    = new ContextRefExpression(tableContext.ObjectType, tableContext);
			var expr         = queryLambda.GetBody(parentRef);

			var info = new BuildInfo(buildInfo, expr, buildInfo.SelectQuery);

			var context = builder.BuildSequence(info);
			return context;
		}

		public static Expression GetAssociationQueryExpression(Expression parentObjExpression, Type parentType, Type objectType, Expression parentTableExpression,
			LambdaExpression queryMethod, MappingSchema mappingSchema)
		{
			var resultParam = Expression.Parameter(objectType);

			var body    = queryMethod.Body.Unwrap();
			body        = EagerLoading.EnsureEnumerable(body, mappingSchema);
			queryMethod = Expression.Lambda(body, queryMethod.Parameters[0]);

			var selectManyMethodInfo = Methods.Queryable.SelectManyProjection.MakeGenericMethod(parentType, objectType, objectType);
			var resultLambda         = Expression.Lambda(resultParam, Expression.Parameter(parentType), resultParam);
			var selectManyMethod     = Expression.Call(null, selectManyMethodInfo, parentTableExpression, queryMethod, resultLambda);

			return selectManyMethod;
		}



		public static Expression EnrichTablesWithLoadWith(Expression expression, Type entityType, List<Tuple<MemberInfo, Expression?>[]> loadWith, MappingSchema mappingSchema)
		{
			var tableType     = typeof(ITable<>).MakeGenericType(entityType);
			var newExpression = expression.Transform(e =>
			{
				if (e.NodeType == ExpressionType.Call)
				{
					var mc = (MethodCallExpression)e;
					if (mc.IsQueryable("GetTable") && tableType.IsSameOrParentOf(mc.Type))
					{
						e = EnrichLoadWith(mc, entityType, loadWith, mappingSchema);
					}
				}

				return e;
			});

			return newExpression;
		}

		public static Expression EnrichLoadWith(Expression table, Type entityType, List<Tuple<MemberInfo, Expression?>[]> loadWith, MappingSchema mappingSchema)
		{
			IQueryable queryable  = (IQueryable)table.EvaluateExpression()!;

			foreach (var members in loadWith)
			{
				var pLoadWith = Expression.Parameter(entityType, "t");
				var isPrevList = false;

				Expression obj = pLoadWith;

				foreach (var member in members)
				{
					if (isPrevList)
						obj = new GetItemExpression(obj, mappingSchema);

					if (member.Item1.DeclaringType != obj.Type)
						obj = Expression.Convert(obj, member.Item1.DeclaringType);
					obj = Expression.MakeMemberAccess(obj, member.Item1);

					isPrevList = typeof(IEnumerable).IsSameOrParentOf(obj.Type);
				}

				var queryFilter = members[members.Length - 1].Item2;

				if (queryFilter == null)
				{
					var method = Methods.LinqToDB.LoadWith.MakeGenericMethod(entityType, obj.Type);

					var lambda = Expression.Lambda(obj, pLoadWith);
					queryable  = (IQueryable)method.Invoke(null, new object[] { queryable, lambda });
				}
				else
				{
					var method =
						(isPrevList
							? Methods.LinqToDB.LoadWithQueryMany
							: Methods.LinqToDB.LoadWithQuerySingle).MakeGenericMethod(entityType,
							EagerLoading.GetEnumerableElementType(obj.Type, mappingSchema));

					if (isPrevList)
						obj = EagerLoading.EnsureEnumerable(obj, mappingSchema);

					var lambda = Expression.Lambda(obj, pLoadWith);

					queryable = (IQueryable)method.Invoke(null,
						new object[] { queryable, lambda, queryFilter.EvaluateExpression()! });
				}
			}

			return queryable.Expression;
		}

		public static Delegate? GetLoadWithFunc(List<Tuple<MemberInfo, Expression?>[]>? loadWith, MemberInfo memberInfo)
		{
			Delegate? loadWithFunc = null;
			if (loadWith != null)
			{
				loadWithFunc = GetLoadWith(loadWith)?
					.FirstOrDefault(li => li.MemberInfo == memberInfo)?.Filter?.EvaluateExpression() as Delegate;
			}

			return loadWithFunc;
		}

		public class LoadWithItem
		{
			public MemberInfo  MemberInfo   = null!;
			public Expression? Filter;
			public List<Tuple<MemberInfo, Expression?>[]> NextLoadWith = null!;
		}


		public static List<LoadWithItem> GetLoadWith(List<Tuple<MemberInfo, Expression?>[]> infos)
		{
			return
			(
				from lw in infos
				select new
				{
					head = lw.First(),
					tail = lw.Skip(1).ToArray()
				}
				into info
				group info by new { MemberInfo = info.head.Item1, Filter = info.head.Item2 } into gr
				select new LoadWithItem
				{
					MemberInfo   = gr.Key.MemberInfo,
					Filter       = gr.Key.Filter,
					NextLoadWith = (from i in gr where i.tail.Length > 0 select i.tail).ToList()
				}
			).ToList();
		}

	}

}
