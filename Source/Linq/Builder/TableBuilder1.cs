using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
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

		static IEnumerable<T> ExecuteQuery<T>(Query query, IDataContext dataContext, Expression expression, Func<IDataReader,T> mapper)
		{
			using (var ctx = dataContext.GetQueryContext(query))
			using (var dr  = ctx.ExecuteReader())
			{
				while (dr.Read())
				{
					yield return mapper(dr);
				}
			}
		}

		public override Expression BuildQuery<T>()
		{
			var sqlBuilder  = GetSqlBuilder();
			var expression  = sqlBuilder.BuildExpression();
			var selectQuery = sqlBuilder.GetSelectQuery();

			expression = _query.FinalizeQuery(selectQuery, expression);

			var expr = Expression.Call(
				MemberHelper.MethodOf(() => ExecuteQuery<T>(null, null, null, null)),
				Expression.Constant(_query),
				Query.DataContextParameter,
				Query.ExpressionParameter,
				Expression.Lambda<Func<IDataReader,T>>(expression, Query.DataReaderParameter));

			return expr;
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

				Expression expr = Expression.MemberInit(
					Expression.New(_objectType),
					fieldInfo
						//.Where (m => !m.field.SqlField.ColumnDescriptor.MemberAccessor.IsComplex)
						.Select(m => (MemberBinding)Expression.Bind(
							m.field.SqlField.ColumnDescriptor.Storage == null ?
								m.field.SqlField.ColumnDescriptor.MemberAccessor.MemberInfo :
								Expression.PropertyOrField(Expression.Constant(null, _objectType), m.field.SqlField.ColumnDescriptor.Storage).Member,
							m.expr)));

				return expr;
			}
		}
	}
}
