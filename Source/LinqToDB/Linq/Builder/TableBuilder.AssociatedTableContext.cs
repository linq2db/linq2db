using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using LinqToDB.Expressions;
	using Mapping;
	using SqlQuery;

	partial class TableBuilder
	{
		public class AssociatedTableContext : TableContext
		{
			public readonly TableContext             ParentAssociation;
			public readonly SqlJoinedTable  ParentAssociationJoin;
			public readonly AssociationDescriptor    Association;
			public readonly bool                     IsList;
			public          int                      RegularConditionCount;
			public          LambdaExpression         ExpressionPredicate;

			public override IBuildContext Parent
			{
				get { return ParentAssociation.Parent; }
				set { }
			}

			public AssociatedTableContext(ExpressionBuilder builder, TableContext parent, AssociationDescriptor association)
				: base(builder, parent.SelectQuery)
			{
				var type = association.MemberInfo.GetMemberType();
				var left = association.CanBeNull;

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

				var psrc = parent.SelectQuery.From[parent.SqlTable];
				var join = left ? SqlTable.WeakLeftJoin() : SqlTable.WeakInnerJoin();

				Association           = association;
				ParentAssociation     = parent;
				ParentAssociationJoin = join.JoinedTable;

				psrc.Joins.Add(join.JoinedTable);

				for (var i = 0; i < association.ThisKey.Length; i++)
				{
					if (!parent.SqlTable.Fields.TryGetValue(association.ThisKey[i], out var field1))
						throw new LinqException("Association key '{0}' not found for type '{1}.", association.ThisKey[i], parent.ObjectType);

					if (!SqlTable.Fields.TryGetValue(association.OtherKey[i], out var field2))
						throw new LinqException("Association key '{0}' not found for type '{1}.", association.OtherKey[i], ObjectType);

//					join.Field(field1).Equal.Field(field2);

					ISqlPredicate predicate = new SqlPredicate.ExprExpr(
						field1, SqlPredicate.Operator.Equal, field2);

					predicate = builder.Convert(parent, predicate);

					join.JoinedTable.Condition.Conditions.Add(new SqlCondition(false, predicate));
				}

				if (ObjectType != OriginalType)
				{
					var predicate = Builder.MakeIsPredicate(this, OriginalType);
 
					if (predicate.GetType() != typeof(SqlPredicate.Expr))
						join.JoinedTable.Condition.Conditions.Add(new SqlCondition(false, predicate));
				}

				RegularConditionCount = join.JoinedTable.Condition.Conditions.Count;
				ExpressionPredicate   = Association.GetPredicate(parent.ObjectType, ObjectType);

				if (ExpressionPredicate != null)
				{
					var expr = Builder.ConvertExpression(ExpressionPredicate.Body.Unwrap());

					Builder.BuildSearchCondition(
						new ExpressionContext(null, new IBuildContext[] { parent, this }, ExpressionPredicate),
						expr,
						join.JoinedTable.Condition.Conditions);
				}

				Init(false);
			}

			protected override Expression ProcessExpression(Expression expression)
			{
				var isLeft = false;

				for (
					var association = this;
					isLeft == false && association != null;
					association = association.ParentAssociation as AssociatedTableContext)
				{
					isLeft =
						association.ParentAssociationJoin.JoinType == JoinType.Left ||
						association.ParentAssociationJoin.JoinType == JoinType.OuterApply;
				}

				if (isLeft)
				{
					Expression cond = null;

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

			protected internal override List<MemberInfo[]> GetLoadWith()
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

					var tableExpression = builder.DataContext.GetTable<T>();

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
									obj = new GetItemExpression(obj);

								obj = Expression.MakeMemberAccess(obj, member);

								isPrevList = typeof(IEnumerable).IsSameOrParentOf(obj.Type);
							}

							tableExpression = tableExpression.LoadWith(Expression.Lambda<Func<T,object>>(obj, pLoadWith));
						}
					}

					Expression expression;

					{ // Where
						var pWhere = Expression.Parameter(typeof(T), "t");

						Expression expr = null;

						for (var i = 0; i < tableContext.Association.ThisKey.Length; i++)
						{
							var thisProp  = Expression.PropertyOrField(Expression.Convert(lParent, parentObject.Type), tableContext.Association.ThisKey[i]);
							var otherProp = Expression.PropertyOrField(pWhere, tableContext.Association.OtherKey[i]);

							var ex = ExpressionBuilder.Equal(tableContext.Builder.MappingSchema, otherProp, thisProp);

							expr = expr == null ? ex : Expression.AndAlso(expr, ex);
						}

						expression = tableExpression.Where(Expression.Lambda<Func<T,bool>>(expr, pWhere)).Expression;
					}

					var lambda      = Expression.Lambda<Func<IDataContext,object,IEnumerable<T>>>(expression, lContext, lParent);
					var queryReader = CompiledQuery.Compile(lambda);

					expression = Expression.Call(
						null,
						MemberHelper.MethodOf(() => ExecuteSubQuery(null, null, null)),
							ExpressionBuilder.QueryRunnerParam,
							Expression.Convert(parentObject, typeof(object)),
							Expression.Constant(queryReader));

					var memberType = tableContext.Association.MemberInfo.GetMemberType();

					if (memberType == typeof(T[]))
						return Expression.Call(null, MemberHelper.MethodOf(() => Enumerable.ToArray<T>(null)), expression);

					if (memberType.IsSameOrParentOf(typeof(List<T>)))
						return Expression.Call(null, MemberHelper.MethodOf(() => Enumerable.ToList<T>(null)), expression);

					var ctor = memberType.GetConstructorEx(new[] { typeof(IEnumerable<T>) });

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

			protected override Expression BuildQuery(Type tableType, TableContext tableContext, ParameterExpression parentObject)
			{
				if (IsList == false)
					return base.BuildQuery(tableType, tableContext, parentObject);

				if (Common.Configuration.Linq.AllowMultipleQuery == false)
					throw new LinqException("Multiple queries are not allowed. Set the 'LinqToDB.Common.Configuration.Linq.AllowMultipleQuery' flag to 'true' to allow multiple queries.");

				var sqtype = typeof(SubQueryHelper<>).MakeGenericType(tableType);
				var helper = (ISubQueryHelper)Activator.CreateInstance(sqtype);

				return helper.GetSubquery(Builder, this, parentObject);
			}
		}
	}
}
