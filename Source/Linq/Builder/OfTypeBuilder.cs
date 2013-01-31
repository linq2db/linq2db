using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;
	using SqlBuilder;
	using Reflection.Extension;

	class OfTypeBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("OfType");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var table    = sequence as TableBuilder.TableContext;

			if (table != null && table.InheritanceMapping.Count > 0)
			{
				var objectType = methodCall.Type.GetGenericArguments()[0];

				if (table.ObjectType.IsSameOrParentOf(objectType))
				{
					var predicate = builder.MakeIsPredicate(table, objectType);

					if (predicate.GetType() != typeof(SqlQuery.Predicate.Expr))
						sequence.SqlQuery.Where.SearchCondition.Conditions.Add(new SqlQuery.Condition(false, predicate));
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
						var extension = TypeExtension.GetTypeExtension(type, builder.MappingSchema.Extensions);
						var mapping   = builder.MappingSchema.MetadataProvider.GetInheritanceMapping(type, extension);

						if (mapping.Length > 0)
						{
							var predicate = MakeIsPredicate(builder, sequence, fromType, toType);

							sequence.SqlQuery.Where.SearchCondition.Conditions.Add(new SqlQuery.Condition(false, predicate));

							return new OfTypeContext(sequence, methodCall);
						}
					}
				}
			}

			return sequence;
		}

		ISqlPredicate MakeIsPredicate(ExpressionBuilder builder, IBuildContext context, Type fromType, Type toType)
		{
			var table          = new SqlTable(builder.MappingSchema, fromType);
			var mapper         = builder.MappingSchema.GetObjectMapper(fromType);
			var discriminators = TableBuilder.TableContext.GetInheritanceDiscriminators(
				builder, table, fromType, mapper.InheritanceMapping);

			return builder.MakeIsPredicate(context, mapper.InheritanceMapping, discriminators, toType,
				name =>
				{
					var field  = table.Fields.Values.First(f => f.Name == name);
					var member = field.MemberMapper.MemberAccessor.MemberInfo;
					var expr   = Expression.MakeMemberAccess(Expression.Parameter(member.DeclaringType, "p"), member);
					var sql    = context.ConvertToSql(expr, 1, ConvertFlags.Field)[0].Sql;

					return sql;
				});
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		#region OfTypeContext

		class OfTypeContext : PassThroughContext
		{
			public OfTypeContext(IBuildContext context, MethodCallExpression methodCall)
				: base(context)
			{
				_methodCall = methodCall;
			}

			private readonly MethodCallExpression _methodCall;

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var expr   = BuildExpression(null, 0);
				var mapper = Builder.BuildMapper<T>(expr);

				query.SetQuery(mapper);
			}

			public override Expression BuildExpression(Expression expression, int level)
			{
				var expr = base.BuildExpression(expression, level);
				var type = _methodCall.Method.GetGenericArguments()[0];

				if (expr.Type != type)
					expr = Expression.Convert(expr, type);

				return expr;
			}
		}

		#endregion
	}
}
