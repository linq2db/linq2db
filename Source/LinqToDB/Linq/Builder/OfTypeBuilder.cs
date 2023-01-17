using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;
	using SqlQuery;

	sealed class OfTypeBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("OfType");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			if (sequence is TableBuilder.TableContext table
				&& table.InheritanceMapping.Count > 0)
			{
				var objectType = methodCall.Type.GetGenericArguments()[0];

				if (table.ObjectType.IsSameOrParentOf(objectType))
				{
					if (!buildInfo.IsTest)
					{
						var predicate = builder.MakeIsPredicate(table, objectType);

						if (predicate.GetType() != typeof(SqlPredicate.Expr))
							sequence.SelectQuery.Where.SearchCondition.Conditions.Add(
								new SqlCondition(false, predicate));
					}

					return new OfTypeContext(sequence, objectType);
				}
			}
			else
			{
				var toType   = methodCall.Type.GetGenericArguments()[0];
				var gargs    = methodCall.Arguments[0].Type.GetGenericArguments(typeof(IQueryable<>));
				var fromType = gargs == null ? typeof(object) : gargs[0];

				if (toType.IsSubclassOf(fromType))
				{
					for (var type = toType.BaseType; type != null && type != typeof(object); type = type.BaseType)
					{
						var mapping = builder.MappingSchema.GetEntityDescriptor(type).InheritanceMapping;

						if (mapping.Count > 0)
						{
							if (!buildInfo.IsTest)
							{
								var predicate = MakeIsPredicate(builder, sequence, fromType, toType);

								sequence.SelectQuery.Where.SearchCondition.Conditions.Add(new SqlCondition(false, predicate));
							}

							return new OfTypeContext(sequence, toType);
						}
					}
				}
			}

			return sequence;
		}

		static ISqlPredicate MakeIsPredicate(ExpressionBuilder builder, IBuildContext context, Type fromType, Type toType)
		{
			var table          = new SqlTable(builder.MappingSchema, fromType);
			var mapper         = builder.MappingSchema.GetEntityDescriptor(fromType);
			var discriminators = mapper.InheritanceMapping;

			return builder.MakeIsPredicate((context, table), context, discriminators, toType,
				static (context, name) =>
				{
					var field  = context.table.FindFieldByMemberName(name) ?? throw new LinqException($"Field {name} not found in table {context.table}");
					var member = field.ColumnDescriptor.MemberInfo;

					var contextRef = new ContextRefExpression(member.DeclaringType!, context.context);
					var expr       = Expression.MakeMemberAccess(contextRef, member);
					var sql        = context.context.Builder.ConvertToSql(contextRef.BuildContext, expr);

					return sql;
				});
		}

		#region OfTypeContext

		sealed class OfTypeContext : PassThroughContext
		{
			public Type EntityType { get; }

			public OfTypeContext(IBuildContext context, Type entityType)
				: base(context)
			{
				EntityType = entityType;
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				var corrected = base.MakeExpression(path, flags);

				var noConvert = corrected.UnwrapConvert();

				if (EntityType != noConvert.Type
				    && noConvert is SqlGenericConstructorExpression { ConstructType: SqlGenericConstructorExpression.CreateType.Full })
				{
					corrected = Builder.BuildFullEntityExpression(Context, EntityType, flags);
				}

				return corrected;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new OfTypeContext(context.CloneContext(Context), EntityType);
			}
		}

		#endregion
	}
}
