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

		#region Init

		readonly Type _originalType;

		Type             _objectType;
		SqlTable         _sqlTable;
		EntityDescriptor _entityDescriptor;

		protected override void Init()
		{
			_objectType       = GetObjectType();
			_sqlTable         = new SqlTable(Query.Query.MappingSchema, _objectType);
			_entityDescriptor = Query.Query.MappingSchema.GetEntityDescriptor(_objectType);
		}

		Type GetObjectType()
		{
			for (var type = _originalType.BaseTypeEx(); type != null && type != typeof(object); type = type.BaseTypeEx())
			{
				var mapping = Query.Query.MappingSchema.GetEntityDescriptor(type).InheritanceMapping;

				if (mapping.Count > 0)
					return type;
			}

			return _originalType;
		}

		#endregion

		// IT : # table builder.
		public override SqlBuilderBase GetSqlBuilder()
		{
			return new TableSqlBuilder(this);
		}

		class TableSqlBuilder : SqlBuilderBase
		{
			public TableSqlBuilder(TableBuilder1 tableBuilder)
			{
				_tableBuilder = tableBuilder;

				CreateSelectQuery();
			}

			readonly TableBuilder1 _tableBuilder;

			void CreateSelectQuery()
			{
				SelectQuery = new SelectQuery();

				SelectQuery .From.Table(_tableBuilder._sqlTable);

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
		}
	}
}
