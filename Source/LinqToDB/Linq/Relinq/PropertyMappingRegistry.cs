using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;

namespace LinqToDB.Linq.Relinq
{
	public class PropertyMappingRegistry
	{
		private Dictionary<MemberExpression, MemberTransformationInfo> _memberTransformations =
			new Dictionary<MemberExpression, MemberTransformationInfo>(new ExpressionEqualityComparer());

		public class MemberTransformationInfo
		{
			public MemberTransformationInfo(QueryModel queryModel, SelectQuery selectQuery, ISqlTableSource tableSource,
				Expression transformation)
			{
				QueryModel = queryModel;
				SelectQuery = selectQuery;
				TableSource = tableSource;
				Transformation = transformation;
			}

			public override string ToString()
			{
				var str = Transformation == null ? "object" : Transformation.ToString();
				return $" -> {str}";
			}

			public ISqlTableSource TableSource { get; }
			public QueryModel QueryModel { get; }
			public SelectQuery SelectQuery { get; }
			public Expression Transformation { get; }
		}

		public void RegisterSelector(QueryModel queryModel, IQuerySource querySource, Expression selector, SelectQuery query, ISqlTableSource tableSource, MappingSchema mappingSchema)
		{
			void RegisterLevel(Expression objExpression, Expression argument)
			{
				foreach (var mapping in GeneratorHelper.GetMemberMapping(argument, mappingSchema))
				{
					var member = objExpression.Type.GetMemberEx(mapping.MemberInfo);
					if (member != null)
					{
						var ma = Expression.MakeMemberAccess(objExpression, member);

						_memberTransformations.Add(ma,
							new MemberTransformationInfo(queryModel, query, tableSource,
								GeneratorHelper.RemoveNullPropagation(mapping.Expression)));

						if (mapping.Expression != null)
							RegisterLevel(ma, mapping.Expression);
					}
				}
			}

			var refExpression = new QuerySourceReferenceExpression(querySource);
			RegisterLevel(refExpression, selector);
		}

		public void RegisterGroupKey(QueryModel queryModel, IQuerySource querySource, Expression selector,
			SelectQuery query, ISqlTableSource tableSource, MappingSchema mappingSchema)
		{
			void RegisterLevel(Expression objExpression, Expression argument)
			{
				foreach (var mapping in GeneratorHelper.GetMemberMapping(argument, mappingSchema))
				{
					var member = objExpression.Type.GetMemberEx(mapping.MemberInfo);
					if (member != null)
					{
						var ma = Expression.MakeMemberAccess(objExpression, member);

						_memberTransformations.Add(ma,
							new MemberTransformationInfo(queryModel, query, tableSource,
								GeneratorHelper.RemoveNullPropagation(mapping.Expression)));

						if (mapping.Expression != null)
							RegisterLevel(ma, mapping.Expression);
					}
				}
			}

			Expression refExpression = new QuerySourceReferenceExpression(querySource);
			refExpression = Expression.PropertyOrField(refExpression, "Key");
			RegisterLevel(refExpression, selector);
		}

		public MemberTransformationInfo GetTransformation(MemberExpression expression, MappingSchema mappingSchema)
		{
			if (_memberTransformations.TryGetValue(expression, out var info))
				return info;
			return null;
		}
	}
}
