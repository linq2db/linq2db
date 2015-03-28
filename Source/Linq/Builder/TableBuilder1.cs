using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using Mapping;
	using SqlQuery;

	class TableBuilder1 : ExpressionBuilderBase
	{
		public TableBuilder1(Expression expression)
			: base(expression)
		{
			_originalType = Expression.Type.GetGenericArgumentsEx()[0];
		}

		readonly Type _originalType;

		internal override SqlBuilderBase GetSqlBuilder()
		{
			return new TableSqlBuilder(Query, _originalType);
		}

		public override Expression BuildQuery()
		{
			var sqlBuilder = GetSqlBuilder();
			var expression = sqlBuilder.BuildExpression();

			return expression;
		}

		// IT : # table builder.
		class TableSqlBuilder : SqlBuilderBase
		{
			public TableSqlBuilder(QueryExpression query, Type originalType)
			{
				_originalType     = originalType;
				_objectType       = GetObjectType(query);
				_sqlTable         = new SqlTable(query.Query.MappingSchema, _objectType);
				_entityDescriptor = query.Query.MappingSchema.GetEntityDescriptor(_objectType);

				CreateSelectQuery();
			}

			readonly Type             _originalType;
			readonly Type             _objectType;
			readonly SqlTable         _sqlTable;
			readonly EntityDescriptor _entityDescriptor;

			Type GetObjectType(QueryExpression query)
			{
				for (var type = _originalType.BaseTypeEx(); type != null && type != typeof(object); type = type.BaseTypeEx())
				{
					var mapping = query.Query.MappingSchema.GetEntityDescriptor(type).InheritanceMapping;

					if (mapping.Count > 0)
						return type;
				}

				return _originalType;
			}

			void CreateSelectQuery()
			{
				SelectQuery = new SelectQuery();

				SelectQuery .From.Table(_sqlTable);

//				// Original table is a parent.
//				//
//				if (ObjectType != OriginalType)
//				{
//					var predicate = Builder.MakeIsPredicate(this, OriginalType);
//
//					if (predicate.GetType() != typeof(SelectQuery.Predicate.Expr))
//						SelectQuery.Where.SearchCondition.Conditions.Add(new SelectQuery.Condition(false, predicate));
//				}
			}

			public override Expression BuildExpression()
			{
				throw new NotImplementedException();
			}
		}
	}
}
