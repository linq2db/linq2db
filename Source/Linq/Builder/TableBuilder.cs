using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;
	using Mapping;
	using SqlQuery;

	class TableBuilder : ExpressionBuilderBase
	{
		public TableBuilder(Query query, Expression expression)
			: base(expression)
		{
			_query        = query;
			_originalType = expression.Type.GetGenericArgumentsEx()[0];
		}

		readonly Query _query;
		readonly Type  _originalType;

		public override SqlBuilderBase GetSqlBuilder()
		{
			var builder = new TableSqlBuilder(_query, _originalType);

			return builder;
		}

		#region BuildQuery

		static IEnumerable<T> ExecuteQuery<T>(Query query, IDataContext dataContext, Expression expression, Func<IDataReader,T> mapper)
		{
			using (var ctx = dataContext.GetQueryContext(query, expression))
			using (var dr = ctx.ExecuteReader())
				while (dr.Read())
					yield return mapper(dr);
		}

		static async Task ExecuteQueryAsync<T>(
			Query query, IDataContext dataContext, Expression expression, Func<IDataReader,T> mapper, Action<T> action, CancellationToken cancellationToken)
		{
			using (var ctx = dataContext.GetQueryContext(query, expression))
			using (var dr = await ctx.ExecuteReaderAsync(cancellationToken))
				await dr.QueryForEachAsync(mapper, action, cancellationToken);
		}

		Expression<Func<IDataReader,T>> BuildMapper<T>()
		{
			var sqlBuilder  = GetSqlBuilder();
			var expression  = sqlBuilder.BuildExpression();
			var selectQuery = sqlBuilder.GetSelectQuery();

			expression = _query.FinalizeQuery(selectQuery, expression);

			return Expression.Lambda<Func<IDataReader,T>>(expression, Query.DataReaderParameter);
		}

		public override Expression BuildQuery<T>()
		{
			var expr = Expression.Call(
				MemberHelper.MethodOf(() => ExecuteQuery<T>(null, null, null, null)),
				Expression.Constant(_query),
				Query.DataContextParameter,
				Query.ExpressionParameter,
				BuildMapper<T>());

			return expr;
		}

		public override void BuildQuery<T>(Query<T> query)
		{
			var l = BuildMapper<T>().Compile();

			query.GetIEnumerable  = (ctx, expr)                => ExecuteQuery     (query, ctx, expr, l);
			query.GetForEachAsync = (ctx, expr, action, token) => ExecuteQueryAsync(query, ctx, expr, l, action, token);
		}

		#endregion

		#region FieldSqlBuilder

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

			public int GetFieldIndex()
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

			public Expression GetReadExpression(Query query, Type type)
			{
				return new ConvertFromDataReaderExpression(
					type,
					GetFieldIndex(),
					query.DataReaderLocalParameter,
					query.DataContext);
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

		#endregion

		#region TableSqlBuilder

		// IT : # table builder.
		class TableSqlBuilder : SqlBuilderBase
		{
			public TableSqlBuilder(Query query, Type type)
			{
				_query            = query;
				_type             = type;
				_entityDescriptor = query.MappingSchema.GetEntityDescriptor(_type);
				_sqlTable         = new SqlTable(query.MappingSchema, (_entityDescriptor.BaseDescriptor ?? _entityDescriptor).ObjectType);
				_selectQuery      = CreateSelectQuery();
				_fieldBuilders    = _sqlTable.Fields.Values.Select(f => new FieldSqlBuilder(_selectQuery, f)).ToList();
			}

			readonly Query                 _query;
			readonly Type                  _type;
			readonly SqlTable              _sqlTable;
			readonly EntityDescriptor      _entityDescriptor;
			readonly SelectQuery           _selectQuery;
			readonly List<FieldSqlBuilder> _fieldBuilders;

			SelectQuery CreateSelectQuery()
			{
				var selectQuery = new SelectQuery();

				selectQuery.From.Table(_sqlTable);

				// Build Inheritance Condition.
				//
				if (_entityDescriptor.BaseDescriptor != null)
				{
					var predicate = SqlBuilder.BuildInheritanceCondition(
						_query,
						_selectQuery,
						_entityDescriptor,
						name => _sqlTable.Fields.Values.FirstOrDefault(f => f.Name == name));

					selectQuery.Where.SearchCondition.Conditions.Add(new SelectQuery.Condition(false, predicate));
				}

				return selectQuery;
			}

			public override SelectQuery GetSelectQuery()
			{
				return _selectQuery;
			}

			#region BuildExpression

			ParameterExpression _variable;

			public override Expression BuildExpression()
			{
				if (_variable != null)
					return _variable;

				var expr = BuildExpressionInternal();

				return _variable ?? (_variable = _query.BuildVariableExpression(expr));
			}

			Expression BuildExpressionInternal()
			{
				return _entityDescriptor.InheritanceMapping.Count != 0 ?
					BuildInheritanceExpression() :
					BuildEntityExpression(_entityDescriptor);
			}

			static object DefaultInheritanceMappingException(object value, Type type)
			{
				throw new LinqException("Inheritance mapping is not defined for discriminator value '{0}' in the '{1}' hierarchy.", value, type);
			}

			Expression BuildInheritanceExpression()
			{
				Expression expr;

				var defaultMapping = _entityDescriptor.InheritanceMapping.SingleOrDefault(m => m.IsDefault);

				if (defaultMapping != null)
				{
					expr = Expression.Convert(BuildEntityExpression(defaultMapping.EntityDescriptor), _type);
				}
				else
				{
					var readDiscriminator = _fieldBuilders
						.First(f => f.SqlField.Name == _entityDescriptor.InheritanceMapping[0].DiscriminatorName).GetReadExpression(_query);

					expr = Expression.Convert(
						Expression.Call(null,
							MemberHelper.MethodOf(() => DefaultInheritanceMappingException(null, null)),
							Expression.Convert(readDiscriminator, typeof(object)),
							Expression.Constant(_type)),
						_type);
				}

				foreach (var mapping in _entityDescriptor.InheritanceMapping.Where(m => m != defaultMapping))
				{
					var discriminatorField = _fieldBuilders.First(f => f.SqlField.Name == mapping.DiscriminatorName);

					Expression testExpr;

					if (mapping.Code == null)
					{
						testExpr = Expression.Call(
							_query.DataReaderLocalParameter,
							ReflectionHelper.DataReader.IsDBNull,
							Expression.Constant(discriminatorField.GetFieldIndex()));
					}
					else
					{
						var codeType = mapping.Code.GetType();

						testExpr = Expression.Equal(
							Expression.Constant(mapping.Code),
							discriminatorField.GetReadExpression(_query, codeType));
					}

					expr = Expression.Condition(
						testExpr,
						Expression.Convert(BuildEntityExpression(mapping.EntityDescriptor), _type),
						expr);
				}

				return expr;
			}

			Expression BuildEntityExpression(EntityDescriptor descriptor)
			{
				var names = descriptor.Columns.Select(c => c.MemberAccessor.Name).ToList();

				var fieldInfo = _fieldBuilders
					.Where (f => names.Contains(f.SqlField.ColumnDescriptor.MemberAccessor.Name))
					.ToList();

				Expression expr = Expression.MemberInit(
					Expression.New(descriptor.ObjectType),
					fieldInfo
						.Select(f => (MemberBinding)Expression.Bind(
							Expression.PropertyOrField(
								Expression.Constant(null, descriptor.ObjectType),
								f.SqlField.ColumnDescriptor.Storage ?? f.SqlField.ColumnDescriptor.MemberAccessor.Name).Member,
							f.GetReadExpression(_query))));

				return expr;
			}

			#endregion
		}

		#endregion
	}
}
