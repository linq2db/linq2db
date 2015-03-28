using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using Mapping;
	using SqlQuery;

	class TableBuilder1 : ExpressionBuilderBase
	{
		public TableBuilder1(Query query, Expression expression)
			: base(expression)
		{
			_query        = query;
			_originalType = expression.Type.GetGenericArgumentsEx()[0];
		}

		readonly Query _query;
		readonly Type  _originalType;

		public override SqlBuilderBase GetSqlBuilder()
		{
			return new TableSqlBuilder(_query, _originalType);
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
			public TableSqlBuilder(Query query, Type originalType)
			{
				_originalType     = originalType;
				_objectType       = GetObjectType(query);
				_sqlTable         = new SqlTable(query.MappingSchema, _objectType);
				_entityDescriptor = query.MappingSchema.GetEntityDescriptor(_objectType);
				_selectQuery      = CreateSelectQuery();
			}

			readonly Type             _originalType;
			readonly Type             _objectType;
			readonly SqlTable         _sqlTable;
			readonly EntityDescriptor _entityDescriptor;
			readonly SelectQuery      _selectQuery;

			Type GetObjectType(Query query)
			{
				for (var type = _originalType.BaseTypeEx(); type != null && type != typeof(object); type = type.BaseTypeEx())
				{
					var mapping = query.MappingSchema.GetEntityDescriptor(type).InheritanceMapping;

					if (mapping.Count > 0)
						return type;
				}

				return _originalType;
			}

			SelectQuery CreateSelectQuery()
			{
				var selectQuery = new SelectQuery();

				selectQuery.From.Table(_sqlTable);

//				// Original table is a parent.
//				//
//				if (ObjectType != OriginalType)
//				{
//					var predicate = Builder.MakeIsPredicate(this, OriginalType);
//
//					if (predicate.GetType() != typeof(SelectQuery.Predicate.Expr))
//						SelectQuery.Where.SearchCondition.Conditions.Add(new SelectQuery.Condition(false, predicate));
//				}

				return selectQuery;
			}

			public override SelectQuery GetSelectQuery()
			{
				return _selectQuery;
			}

			public override Expression BuildExpression()
			{
				throw new NotImplementedException();
			}
		}
	}
}
