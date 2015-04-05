using System;
using System.Linq;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using Mapping;
	using SqlQuery;

	static class SqlBuilder
	{
		public static ISqlPredicate BuildInheritanceCondition(
			Query                       query,
			SelectQuery                 selectQuery,
			EntityDescriptor            entityDescriptor,
			Func<string,ISqlExpression> getSql)
		{
			var inheritanceMapping = entityDescriptor.BaseDescriptor.InheritanceMapping;
			var toType             = entityDescriptor.ObjectType;

			var mapping = inheritanceMapping
				.Where (m => m.Type == toType && !m.IsDefault)
				.ToList();

			switch (mapping.Count)
			{
				case 0 :
					{
						var cond = new SelectQuery.SearchCondition();

						if (inheritanceMapping.Any(m => m.Type == toType))
						{
							foreach (var m in inheritanceMapping.Where(m => !m.IsDefault))
							{
								cond.Conditions.Add(
									new SelectQuery.Condition(
										false,
										query.SqlOptimizer.ConvertPredicate(
											selectQuery,
											new SelectQuery.Predicate.ExprExpr(
												getSql(m.DiscriminatorName),
												SelectQuery.Predicate.Operator.NotEqual,
												query.MappingSchema.GetSqlValue(m.Discriminator.MemberType, m.Code)))));
							}
						}
						else
						{
							foreach (var m in inheritanceMapping.Where(m => toType.IsSameOrParentOf(m.Type)))
							{
								cond.Conditions.Add(
									new SelectQuery.Condition(
										false,
										query.SqlOptimizer.ConvertPredicate(
											selectQuery,
											new SelectQuery.Predicate.ExprExpr(
												getSql(m.DiscriminatorName),
												SelectQuery.Predicate.Operator.Equal,
												query.MappingSchema.GetSqlValue(m.Discriminator.MemberType, m.Code))),
										true));
							}
						}

						return cond;
					}

				case 1 :
					{
						return query.SqlOptimizer.ConvertPredicate(
							selectQuery,
							new SelectQuery.Predicate.ExprExpr(
								getSql(mapping[0].DiscriminatorName),
								SelectQuery.Predicate.Operator.Equal,
								query.MappingSchema.GetSqlValue(mapping[0].Discriminator.MemberType, mapping[0].Code)));
					}

				default:
					{
						var cond = new SelectQuery.SearchCondition();

						foreach (var m in mapping)
						{
							cond.Conditions.Add(
								new SelectQuery.Condition(
									false,
									query.SqlOptimizer.ConvertPredicate(
										selectQuery,
										new SelectQuery.Predicate.ExprExpr(
											getSql(m.DiscriminatorName),
											SelectQuery.Predicate.Operator.Equal,
											query.MappingSchema.GetSqlValue(m.Discriminator.MemberType, m.Code))),
									true));
						}

						return cond;
					}
			}
		}
	}
}
