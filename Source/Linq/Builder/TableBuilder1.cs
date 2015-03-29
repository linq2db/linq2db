using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB.Expressions;

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

		class FieldSqlBuilder
		{
			public FieldSqlBuilder(SelectQuery selectQuery, SqlField sqlField)
			{
				_selectQuery = selectQuery;

				SqlField     = sqlField;
			}

			public readonly SqlField SqlField;

			readonly SelectQuery _selectQuery;
			readonly List<int>   _indexes = new List<int>();

			int GetFieldIndex()
			{
				if (_indexes.Count == 0)
					_indexes.Add(_selectQuery.Select.Add(SqlField));

				var level = 0;

				for (var query = _selectQuery; query.ParentSelect != null; query = query.ParentSelect)
				{
					if (_indexes.Count <= level)
						_indexes.Add(query.ParentSelect.Select.Add(query.Select.Columns[_indexes[level]]));
					level++;
				}

				return _indexes[level];
			}

			public Expression GetReadExpression(Query query)
			{
				return new ConvertFromDataReaderExpression(
					SqlField.ColumnDescriptor.MemberType,
					GetFieldIndex(),
					query.DataReaderLocalParameter,
					query.DataContext);
			}
		}

		// IT : # table builder.
		class TableSqlBuilder : SqlBuilderBase
		{
			public TableSqlBuilder(Query query, Type originalType)
			{
				_query            = query;
				_originalType     = originalType;
				_objectType       = GetObjectType(query);
				_sqlTable         = new SqlTable(query.MappingSchema, _objectType);
				_entityDescriptor = query.MappingSchema.GetEntityDescriptor(_objectType);
				_selectQuery      = CreateSelectQuery();
				_fieldBuilders    = _sqlTable.Fields.Values.Select(f => new FieldSqlBuilder(_selectQuery, f)).ToList();
			}

			readonly Query                 _query;
			readonly Type                  _originalType;
			readonly Type                  _objectType;
			readonly SqlTable              _sqlTable;
			readonly EntityDescriptor      _entityDescriptor;
			readonly SelectQuery           _selectQuery;
			readonly List<FieldSqlBuilder> _fieldBuilders;

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
				var fieldInfo = _fieldBuilders
					.Select(f => new { field = f, expr = f.GetReadExpression(_query) })
					.ToList();

				throw new NotImplementedException();
			}
		}
	}
}
