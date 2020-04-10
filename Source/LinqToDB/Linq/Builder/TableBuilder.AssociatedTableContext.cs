using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using Extensions;
	using LinqToDB.Expressions;
	using Mapping;
	using SqlQuery;

	partial class TableBuilder
	{
		public class AssociatedTableContext : TableContext
		{
			public readonly TableContext             ParentAssociation;
			public readonly SqlJoinedTable?          ParentAssociationJoin;
			public readonly AssociationDescriptor    Association;
			public readonly bool                     IsList;
			public readonly bool                     IsSubQuery;
			public          int                      RegularConditionCount;
			public          LambdaExpression?        ExpressionPredicate;
			public readonly bool                     IsLeft;

			public override IBuildContext? Parent
			{
				get { return ParentAssociation.Parent; }
				set { }
			}

			Dictionary<ISqlExpression, SqlField>? _replaceMap;
			internal IBuildContext                _innerContext;

			public AssociatedTableContext(
				ExpressionBuilder     builder,
				TableContext          parent,
				AssociationDescriptor association,
				bool                  forceLeft,
				bool                  asSubquery
			)
				: base(builder, asSubquery ? new SelectQuery() { ParentSelect = parent.SelectQuery } : parent.SelectQuery)
			{
				if (builder     == null) throw new ArgumentNullException(nameof(builder));
				if (parent      == null) throw new ArgumentNullException(nameof(parent));
				if (association == null) throw new ArgumentNullException(nameof(association));

				var type = association.MemberInfo.GetMemberType();
				var left = forceLeft || association.CanBeNull;

				if (!left && parent is AssociatedTableContext parentAssociation)
				{
					left = parentAssociation.IsLeft;
				}

				if (typeof(IEnumerable).IsSameOrParentOf(type))
				{
					var eTypes = type.GetGenericArguments(typeof(IEnumerable<>));
					type       = eTypes != null && eTypes.Length > 0 ? eTypes[0] : type.GetListItemType();
					IsList     = true;
				}

				OriginalType       = type;
				ObjectType         = GetObjectType();
				EntityDescriptor   = Builder.MappingSchema.GetEntityDescriptor(ObjectType);
				InheritanceMapping = EntityDescriptor.InheritanceMapping;
				SqlTable           = new SqlTable(builder.MappingSchema, ObjectType);

				Association       = association;
				ParentAssociation = parent;
				IsSubQuery        = asSubquery;

				if (asSubquery)
				{
					BuildSubQuery(builder, parent, association, left);
					Init(false);
					return;
				}

				SqlJoinedTable join = null;

				//TODO: Find better way to understand for which class association is building
				var parentObjectType = Association.MemberInfo.DeclaringType;
				if (Association.MemberInfo.IsMethodEx())
					parentObjectType = parent.ObjectType;


				// var parentContext = parent.Parent;
				 var parentContext = parent;
				// while (parentContext is ExpressionContext || parentContext is AssociatedTableContext)
				// {
				// 	parentContext = parentContext.Parent;
				// }
				//
				// if (parentContext == null)
				// 	parentContext = parent;

				var parentRef = new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(parentObjectType), parentContext);

				var queryMethod = GetSelectManyQueryMethod(parentObjectType, ObjectType, IsList, left, out IsLeft);

				var selectManyMethod = GetAssociationQueryExpression(Expression.Constant(builder.DataContext),
					queryMethod.Parameters[0], parentObjectType, parentRef, queryMethod);

				// var parentContext = parent.Parent;
				// var parentQuery = parentContext == null ? parent.SelectQuery : parentContext.SelectQuery;
				//
				// var buildInfo = new BuildInfo(parentContext, selectManyMethod, parentQuery)
				// 	{ IsAssociationBuilt = true };


				var parentQuery = parentContext == null ? parent.SelectQuery : parentContext.SelectQuery;

				var buildInfo = new BuildInfo((IBuildContext)null, selectManyMethod, parentQuery)
					{ IsAssociationBuilt = true };

				// var saveParent = parent.Parent;

				_innerContext = builder.BuildSequence(buildInfo);

				// Builder.ReplaceParent(_innerContext, saveParent);

				// _innerContext.Parent = this;

				MarkIsWeak();

				SetTableAlias(association, _innerContext.SelectQuery);

				Init(false);
			}

			private static void SetTableAlias(AssociationDescriptor association, SqlTableSource table)
			{
				if (table != null)
				{
					var alias = association.GenerateAlias();
					if (!alias.IsNullOrEmpty())
						table.Alias = alias;
				}
			}

			private static void SetTableAlias(AssociationDescriptor association, SelectQuery selectQuery)
			{
				if (selectQuery.From.Tables.Count > 0)
				{
					var table = selectQuery.From.Tables[0];
					if (table.Joins.Count > 0)
					{
						table = table.Joins[0].Table;
						SetTableAlias(association, table);
					}
				}
			}

			public LambdaExpression GetSelectManyQueryMethod(Type parentType, Type objectType, bool isList, bool enforceDefault, out bool isLeft)
			{
				var definedQueryMethod = Association.GetQueryMethod(parentType, objectType);

				var shouldAddDefaultIfEmpty = enforceDefault;

				if (definedQueryMethod == null)
				{
					var parentParam = Expression.Parameter(parentType, "parent");
					var childParam  = Expression.Parameter(objectType, "child");

					var parentAccessor = TypeAccessor.GetAccessor(parentType);
					var childAccessor  = TypeAccessor.GetAccessor(objectType);

					Expression predicate = null;
					for (var i = 0; i < Association.ThisKey.Length; i++)
					{
						var parentName   = Association.ThisKey[i];
						var parentMember = parentAccessor.Members.Find(m => m.MemberInfo.Name == parentName);

						if (parentMember == null)
							throw new LinqException("Association key '{0}' not found for type '{1}.", parentName,
								parentType);

						var childName = Association.OtherKey[i];
						var childMember = childAccessor.Members.Find(m => m.MemberInfo.Name == childName);

						if (childMember == null)
							throw new LinqException("Association key '{0}' not found for type '{1}.", childName,
								ObjectType);

						var current = ExpressionBuilder.Equal(Builder.MappingSchema,
							Expression.MakeMemberAccess(parentParam, parentMember.MemberInfo),
							Expression.MakeMemberAccess(childParam, childMember.MemberInfo));

						predicate = predicate == null ? current : Expression.AndAlso(predicate, current);
					}

					ExpressionPredicate = Association.GetPredicate(parentType, objectType);

					if (ExpressionPredicate != null)
					{
						shouldAddDefaultIfEmpty = true;
						// ExpressionPredicate = (LambdaExpression)Builder.ConvertExpressionTree(ExpressionPredicate);
						//
						// ExpressionPredicate = (LambdaExpression)Builder.ConvertExpression(ExpressionPredicate);

						var replacedBody = ExpressionPredicate.GetBody(parentParam, childParam);

						predicate = predicate == null ? replacedBody : Expression.AndAlso(predicate, replacedBody);
					}

					if (predicate == null)
						throw new LinqException("Can not generate Association predicate");

					if (!isList && !shouldAddDefaultIfEmpty)
					{
						var ed = Builder.MappingSchema.GetEntityDescriptor(objectType);
						if (ed.QueryFilterFunc != null)
							shouldAddDefaultIfEmpty = true;
					}

					var dcParam    = Expression.Parameter(Builder.DataContext.GetType(), "dc");
					var queryParam = Expression.Call(Methods.LinqToDB.GetTable.MakeGenericMethod(objectType), dcParam);

					var filterLambda = Expression.Lambda(predicate, childParam);
					Expression body  = Expression.Call(Methods.Queryable.Where.MakeGenericMethod(objectType), queryParam,
						filterLambda);

					//TODO: Move EnsureEnumerable to appropriate place.
					body = EagerLoading.EnsureEnumerable(body, Builder.MappingSchema);
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
				if (!isList && shouldAddDefaultIfEmpty)
				{
					var body = definedQueryMethod.Body.Unwrap();
					body = Expression.Call(Methods.Queryable.DefaultIfEmpty.MakeGenericMethod(OriginalType), body);
					body = EagerLoading.EnsureEnumerable(body, Builder.MappingSchema);
					definedQueryMethod = Expression.Lambda(body, definedQueryMethod.Parameters);
				}

				definedQueryMethod = (LambdaExpression)Builder.ConvertExpressionTree(definedQueryMethod);
				definedQueryMethod = (LambdaExpression)Builder.ConvertExpression(definedQueryMethod);
				definedQueryMethod = (LambdaExpression)definedQueryMethod.OptimizeExpression()!;

				isLeft = shouldAddDefaultIfEmpty;
				return definedQueryMethod;
			}

			private void BuildAssociationCondition(ExpressionBuilder builder, TableContext parent, AssociationDescriptor association, SqlSearchCondition condition)
			{
				for (var i = 0; i < association.ThisKey.Length; i++)
				{
					if (!parent.SqlTable.Fields.TryGetValue(association.ThisKey[i], out var field1))
						throw new LinqException("Association key '{0}' not found for type '{1}.", association.ThisKey[i], parent.ObjectType);

					if (!SqlTable.Fields.TryGetValue(association.OtherKey[i], out var field2))
						throw new LinqException("Association key '{0}' not found for type '{1}.", association.OtherKey[i], ObjectType);

					ISqlPredicate predicate = new SqlPredicate.ExprExpr(
						field1, SqlPredicate.Operator.Equal, field2);

					predicate = builder.Convert(parent, predicate);

					condition.Conditions.Add(new SqlCondition(false, predicate));
				}

				if (ObjectType != OriginalType)
				{
					var predicate = Builder.MakeIsPredicate(this, OriginalType);

					if (predicate.GetType() != typeof(SqlPredicate.Expr))
						condition.Conditions.Add(new SqlCondition(false, predicate));
				}

				RegularConditionCount = condition.Conditions.Count;
				ExpressionPredicate = Association.GetPredicate(parent.ObjectType, ObjectType);

				if (ExpressionPredicate != null)
				{
					ExpressionPredicate = (LambdaExpression)Builder.ConvertExpressionTree(ExpressionPredicate);

					var expr = Builder.ConvertExpression(ExpressionPredicate.Body.Unwrap());

					Builder.BuildSearchCondition(
						new ExpressionContext(parent.Parent, new IBuildContext[] { parent, this }, ExpressionPredicate),
						expr,
						condition.Conditions,
						false);
				}
			}

			public LambdaExpression GetAssociationQueryExpressionAsSubQuery(Expression dataContextExpr, LambdaExpression queryMethod)
			{
				var parentParameter = queryMethod.Parameters[0];

				var body = queryMethod.GetBody(parentParameter, dataContextExpr);
				body = Expression.Convert(body, typeof(IEnumerable<>).MakeGenericType(ObjectType));

				return Expression.Lambda(body, parentParameter);
			}

			private void BuildSubQuery(ExpressionBuilder builder, TableContext parent, AssociationDescriptor association, bool left)
			{
				var queryMethod = Association.GetQueryMethod(parent.ObjectType, ObjectType);
				if (queryMethod != null)
				{
					var subquery = GetAssociationQueryExpressionAsSubQuery(Expression.Constant(builder.DataContext), queryMethod);

					var ownerTableSource = parent.SelectQuery.From.Tables[0];

					// TODO: need to build without join here
					_innerContext = builder.BuildSequence(
						new BuildInfo(
							new ExpressionContext(null, new[] { parent }, subquery),
							subquery.Body,
							new SelectQuery() { ParentSelect = SelectQuery.ParentSelect }
							)
					{ IsAssociationBuilt = false });

					var associationQuery = _innerContext.SelectQuery;

					if (associationQuery.Select.From.Tables.Count < 1)
						throw new LinqToDBException("Invalid association query. It is not possible to inline query.");

					SelectQuery = associationQuery;
				}
				else
				{
					SelectQuery.From.Table(SqlTable);

					BuildAssociationCondition(builder, parent, association, SelectQuery.Where.SearchCondition);
				}

				SetTableAlias(association, SelectQuery.From.Tables[0]);
			}

			public Expression GetAssociationQueryExpression(Expression dataContextExpr, Expression parentObjExpression, Type parentType, Expression parentTableExpression,
				LambdaExpression queryMethod)
			{
				var resultParam = Expression.Parameter(ObjectType);

				var parentObjType = EagerLoading.GetEnumerableElementType(parentTableExpression.Type, Builder.MappingSchema);
				if (parentObjType != parentType)
				{
					parentTableExpression = Expression.Call(Methods.Queryable.OfType.MakeGenericMethod(parentType), parentTableExpression);
				}

				var body    = queryMethod.GetBody(parentObjExpression ?? queryMethod.Parameters[0], dataContextExpr).Unwrap();
				body        = EagerLoading.EnsureEnumerable(body, Builder.MappingSchema);
				queryMethod = Expression.Lambda(body, queryMethod.Parameters[0]);

				var selectManyMethodInfo = Methods.Queryable.SelectManyProjection.MakeGenericMethod(parentType, ObjectType, ObjectType);
				var resultLambda         = Expression.Lambda(resultParam, Expression.Parameter(parentType), resultParam);
				var selectManyMethod     = Expression.Call(null, selectManyMethodInfo, parentTableExpression, queryMethod, resultLambda);

				return selectManyMethod;
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				if (_innerContext != null)
					_innerContext.BuildQuery(query, queryParameter);
				else
					base.BuildQuery(query, queryParameter);
			}

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				if (_innerContext != null)
					return _innerContext.ConvertToSql(expression, level, flags);

				return base.ConvertToSql(expression, level, flags);
			}

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				if (_innerContext != null)
				{
					return _innerContext.ConvertToIndex(expression, level, flags);
				}

				return base.ConvertToIndex(expression, level, flags);
			}

			ISqlExpression CorrectExpression(ISqlExpression expression)
			{
				return expression;
				// return new QueryVisitor().Convert(expression, e =>
				// {
				// 	if (e.ElementType == QueryElementType.SqlField && _replaceMap!.TryGetValue((SqlField)e, out var newField))
				// 		return newField;
				// 	return e;
				//
				// }); 
			}

			public override int ConvertToParentIndex(int index, IBuildContext? context)
			{
				// if (context == _innerContext)
				// {
				// 	var column = context.SelectQuery.Select.Columns[index];
				// 	index = SelectQuery.Select.Add(column);
				// }	
				//
				return base.ConvertToParentIndex(index, context);
			}

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				if (_innerContext != null)
					return _innerContext.BuildExpression(expression, level, enforceServerSide);

				return base.BuildExpression(expression, level, enforceServerSide);
			}

			protected override Expression ProcessExpression(Expression expression)
			{
				var isLeft = false;

				for (
					AssociatedTableContext? association = this;
					isLeft == false && association != null;
					association = association.ParentAssociation as AssociatedTableContext)
				{
					isLeft =
						association.ParentAssociationJoin!.JoinType == JoinType.Left ||
						association.ParentAssociationJoin.JoinType == JoinType.OuterApply;
				}

				if (isLeft)
				{
					Expression? cond = null;

					var keys = ConvertToIndex(null, 0, ConvertFlags.Key);

					foreach (var key in keys)
					{
						var index2  = ConvertToParentIndex(key.Index, null);

						Expression e = Expression.Call(
							ExpressionBuilder.DataReaderParam,
							ReflectionHelper.DataReader.IsDBNull,
							Expression.Constant(index2));

						cond = cond == null ? e : Expression.AndAlso(cond, e);
					}

					expression = Expression.Condition(cond, Expression.Constant(null, expression.Type), expression);
				}

				return expression;
			}

			protected internal override List<Tuple<MemberInfo, Expression?>[]>? GetLoadWith()
			{
				if (LoadWith == null)
				{
					var loadWith = ParentAssociation.GetLoadWith();

					if (loadWith != null)
					{
						foreach (var item in GetLoadWith(loadWith))
						{
							if (Association.MemberInfo.EqualsTo(item.MemberInfo))
							{
								LoadWith = item.NextLoadWith;
								break;
							}
						}
					}
				}

				return LoadWith;
			}

			interface ISubQueryHelper
			{
				Expression GetSubquery(
					ExpressionBuilder      builder,
					AssociatedTableContext tableContext,
					ParameterExpression    parentObject);
			}

			class SubQueryHelper<T> : ISubQueryHelper
				where T : class
			{
				public Expression GetSubquery(
					ExpressionBuilder      builder,
					AssociatedTableContext tableContext,
					ParameterExpression    parentObject)
				{
					var lContext = Expression.Parameter(typeof(IDataContext), "ctx");
					var lParent  = Expression.Parameter(typeof(object), "parentObject");

					Expression expression;

					var queryMethod = tableContext.Association.GetQueryMethod(parentObject.Type, typeof(T));
					if (queryMethod != null)
					{
						var ownerParam = queryMethod.Parameters[0];
						var dcParam    = queryMethod.Parameters[1];
						var ownerExpr  = Expression.Convert(lParent, parentObject.Type);

						expression = queryMethod.Body.Transform(e =>
							e == ownerParam ? ownerExpr : (e == dcParam ? lContext : e));
					}
					else
					{
						IQueryable<T> tableExpression = builder.DataContext.GetTable<T>();

						var loadWith = tableContext.GetLoadWith();

						if (loadWith != null)
						{
							foreach (var members in loadWith)
							{
								var pLoadWith  = Expression.Parameter(typeof(T), "t");
								var isPrevList = false;

								Expression obj = pLoadWith;

								foreach (var member in members)
								{
									if (isPrevList)
										obj = new GetItemExpression(obj, builder.MappingSchema);

									obj = Expression.MakeMemberAccess(obj, member.Item1);

									isPrevList = typeof(IEnumerable).IsSameOrParentOf(obj.Type);
								}

								var method = Methods.LinqToDB.LoadWith.MakeGenericMethod(typeof(T), obj.Type);

								var loadLambda = Expression.Lambda(obj, pLoadWith);
								tableExpression = (IQueryable<T>)method.Invoke(null, new object[] {tableExpression, loadLambda });
							}
						}
						
						// Where
						var pWhere = Expression.Parameter(typeof(T), "t");

						Expression? expr = null;

						for (var i = 0; i < tableContext.Association.ThisKey.Length; i++)
						{
							var thisProp  = ExpressionHelper.PropertyOrField(Expression.Convert(lParent, parentObject.Type), tableContext.Association.ThisKey[i]);
							var otherProp = ExpressionHelper.PropertyOrField(pWhere, tableContext.Association.OtherKey[i]);

							var ex = ExpressionBuilder.Equal(tableContext.Builder.MappingSchema, otherProp, thisProp);

							expr = expr == null ? ex : Expression.AndAlso(expr, ex);
						}

						var predicate = tableContext.Association.GetPredicate(parentObject.Type, typeof(T));
						if (predicate != null)
						{
							var ownerParam = predicate.Parameters[0];
							var childParam = predicate.Parameters[1];
							var ownerExpr  = Expression.Convert(lParent, parentObject.Type);

							var body = predicate.Body.Transform(e =>
								e == ownerParam ? ownerExpr : (e == childParam ? pWhere : e));

							expr = expr == null ? body : Expression.AndAlso(expr, body);
						}

						expression = tableExpression.Where(Expression.Lambda<Func<T,bool>>(expr, pWhere)).Expression;
					}

					var lambda      = Expression.Lambda<Func<IDataContext,object,IEnumerable<T>>>(expression, lContext, lParent);
					var queryReader = CompiledQuery.Compile(lambda);

					expression = Expression.Call(
						null,
						MemberHelper.MethodOf(() => ExecuteSubQuery(null!, null!, null!)),
							ExpressionBuilder.QueryRunnerParam,
							Expression.Convert(parentObject, typeof(object)),
							Expression.Constant(queryReader));

					var memberType = tableContext.Association.MemberInfo.GetMemberType();

					if (memberType == typeof(T[]))
						return Expression.Call(null, MemberHelper.MethodOf(() => Enumerable.ToArray<T>(null)), expression);

					if (memberType.IsSameOrParentOf(typeof(List<T>)))
						return Expression.Call(null, MemberHelper.MethodOf(() => Enumerable.ToList<T>(null)), expression);

					var ctor = memberType.GetConstructor(new[] { typeof(IEnumerable<T>) });

					if (ctor != null)
						return Expression.New(ctor, expression);

					var l = builder.MappingSchema.GetConvertExpression(expression.Type, memberType, false, false);

					if (l != null)
						return l.GetBody(expression);

					throw new LinqToDBException($"Expected constructor '{memberType.Name}(IEnumerable<{tableContext.ObjectType}>)'");
				}

				static IEnumerable<T> ExecuteSubQuery(
					IQueryRunner                             queryRunner,
					object                                   parentObject,
					Func<IDataContext,object,IEnumerable<T>> queryReader)
				{
					using (var db = queryRunner.DataContext.Clone(true))
						foreach (var item in queryReader(db, parentObject))
							yield return item;
				}
			}

			protected override ISqlExpression? GetField(Expression expression, int level, bool throwException)
			{
				if (_innerContext != null)
				{
					var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);

					if (levelExpression.NodeType == ExpressionType.MemberAccess)
					{
						var infos = _innerContext.ConvertToSql(expression, level, ConvertFlags.Field);
						return infos.FirstOrDefault()?.Sql;
					}
				}

				return base.GetField(expression, level, throwException);
			}

			protected override Expression BuildQuery(Type tableType, TableContext tableContext, ParameterExpression? parentObject)
			{
				if (IsList == false)
				{
					if (_innerContext != null)
						return _innerContext.BuildExpression(null, 0, false);
					return base.BuildQuery(tableType, tableContext, parentObject);
				}

				if (Configuration.Linq.AllowMultipleQuery == false)
					throw new LinqException("Multiple queries are not allowed. Set the 'LinqToDB.Common.Configuration.Linq.AllowMultipleQuery' flag to 'true' to allow multiple queries.");

				var detailExpression = EagerLoading.GenerateAssociationExpression(Builder, ParentAssociation, Association)!;

				return detailExpression;
			}

			public void MarkIsWeak()
			{
				if (_innerContext.SelectQuery.From.Tables.Count > 0)
				{
					var table = _innerContext.SelectQuery.From.Tables[0];
					foreach (var join in table.Joins)
					{
						join.IsWeak = true;
					}
				}
			}
		}
	}
}
