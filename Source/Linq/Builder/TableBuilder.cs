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
	using Reflection;
	using SqlQuery;

	class TableBuilder : ExpressionBuilderBase
	{
		public TableBuilder(QueryBuilder builder, Expression expression)
			: base(expression)
		{
			_builder      = builder;
			_originalType = expression.Type.GetGenericArgumentsEx()[0];
		}

		readonly QueryBuilder _builder;
		readonly Type         _originalType;

		public override SqlBuilderBase GetSqlBuilder()
		{
			var builder = new TableSqlBuilder(_builder, _originalType);
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

			expression = _builder.FinalizeQuery(selectQuery, expression);

			return Expression.Lambda<Func<IDataReader,T>>(expression, QueryBuilder.DataReaderParameter);
		}

		public override Expression BuildQueryExpression<T>()
		{
			var expr = Expression.Call(
				MemberHelper.MethodOf(() => ExecuteQuery<T>(null, null, null, null)),
				Expression.Constant(_builder.Query),
				QueryBuilder.DataContextParameter,
				QueryBuilder.ExpressionParameter,
				BuildMapper<T>());

			return expr;
		}

		public override void BuildQuery<T>(QueryBuilder<T> query)
		{
			var e = BuildMapper<T>();
			var l = e.Compile();
			var q = query.Query;

			query.Query.GetIEnumerable  = (ctx, expr)                => ExecuteQuery     (q, ctx, expr, l);
			query.Query.GetForEachAsync = (ctx, expr, action, token) => ExecuteQueryAsync(q, ctx, expr, l, action, token);
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

			public Expression GetReadExpression(QueryBuilder builder, Type type)
			{
				return new ConvertFromDataReaderExpression(
					type,
					GetFieldIndex(),
					builder.DataReaderLocalParameter,
					builder.DataContext);
			}

			public Expression GetReadExpression(QueryBuilder builder)
			{
				return new ConvertFromDataReaderExpression(
					SqlField.ColumnDescriptor.MemberType,
					GetFieldIndex(),
					builder.DataReaderLocalParameter,
					builder.DataContext);
			}
		}

		#endregion

		#region TableSqlBuilder

		class TableSqlBuilder : SqlBuilderBase
		{
			public TableSqlBuilder(QueryBuilder builder, Type type)
			{
				_builder          = builder;
				_type             = type;
				_entityDescriptor = builder.MappingSchema.GetEntityDescriptor(_type);
				_sqlTable         = new SqlTable(builder.MappingSchema, (_entityDescriptor.BaseDescriptor ?? _entityDescriptor).ObjectType);
				_selectQuery      = CreateSelectQuery();
				_fieldBuilders    = _sqlTable.Fields.Values.Select(f => new FieldSqlBuilder(_selectQuery, f)).ToList();
			}

			readonly QueryBuilder          _builder;
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
						_builder.Query,
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

				return _variable = _builder.BuildVariableExpression(expr);
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
						.First(f => f.SqlField.Name == _entityDescriptor.InheritanceMapping[0].DiscriminatorName).GetReadExpression(_builder);

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
							_builder.DataReaderLocalParameter,
							ReflectionHelper.DataReader.IsDBNull,
							Expression.Constant(discriminatorField.GetFieldIndex()));
					}
					else
					{
						var codeType = mapping.Code.GetType();

						testExpr = Expression.Equal(
							Expression.Constant(mapping.Code),
							discriminatorField.GetReadExpression(_builder, codeType));
					}

					expr = Expression.Condition(
						testExpr,
						Expression.Convert(BuildEntityExpression(mapping.EntityDescriptor), _type),
						expr);
				}

				return expr;
			}

			static bool IsRecordAttribute(Attribute attr)
			{
				return attr.GetType().FullName == "Microsoft.FSharp.Core.CompilationMappingAttribute";
			}

			class ColumnInfo
			{
				public bool       IsComplex;
				public string     Name;
				public string     Storage;
				public Expression ReadExpression;
			}

			Expression BuildEntityExpression(EntityDescriptor descriptor)
			{
				var columnInfo =
				(
					from column in descriptor.Columns
					join field  in _fieldBuilders on column.MemberAccessor.Name equals field.SqlField.ColumnDescriptor.MemberAccessor.Name
					select new ColumnInfo
					{
						IsComplex      = column.MemberAccessor.IsComplex,
						Name           = column.MemberName,
						Storage        = column.Storage,
						ReadExpression = field.GetReadExpression(_builder)
					}
				).ToList();

				return BuildEntityExpression(descriptor.TypeAccessor, columnInfo);
			}

			Expression BuildEntityExpression(TypeAccessor typeAccessor, List<ColumnInfo> columnInfo)
			{
				var isRecord = _builder.MappingSchema.GetAttributes<Attribute>(typeAccessor.Type).FirstOrDefault(IsRecordAttribute) != null;

				return isRecord ?
					BuildRecordConstructor (typeAccessor, columnInfo) :
					BuildDefaultConstructor(typeAccessor, columnInfo);
			}

			Expression BuildComplexMemberExpression(TypeAccessor parentAccessor, Expression parentExpression, List<ColumnInfo> columnInfo)
			{
				var isRecord        = _builder.MappingSchema.GetAttributes<Attribute>(parentAccessor.Type).FirstOrDefault(IsRecordAttribute) != null;
				var buildExpression = BuildEntityExpression(parentAccessor, columnInfo);

				if (isRecord)
					return Expression.Assign(parentExpression, buildExpression);

				if (parentAccessor.Type.IsClassEx() || parentAccessor.Type.IsNullable())
				{
					var vars  = new List<ParameterExpression>();
					var exprs = new List<Expression>();
					var obj   = parentExpression as ParameterExpression;

					if (obj == null)
					{
						obj = Expression.Variable(parentExpression.Type);
						vars. Add(obj);
						exprs.Add(Expression.Assign(obj, parentExpression));
					}

					exprs.AddRange(columnInfo
						.Where (c => !c.IsComplex)
						.Select(c => Expression.Assign(Expression.PropertyOrField(obj, c.Name), c.ReadExpression)));

					exprs.Add(BuildComplexMembers(parentAccessor, obj, columnInfo));

					return Expression.IfThenElse(
						Expression.Equal (parentExpression, Expression.Constant(null)),
						Expression.Assign(parentExpression, buildExpression),
						Expression.Block(vars, exprs));
				}

				return Expression.Assign(parentExpression, buildExpression);
			}

			Expression BuildComplexMembers(TypeAccessor parentAccessor, Expression parentExpression, List<ColumnInfo> columnInfo)
			{
				if (!columnInfo.Any(c => c.IsComplex))
					return parentExpression;

				var vars  = new List<ParameterExpression>();
				var exprs = new List<Expression>();
				var obj   = parentExpression as ParameterExpression;

				if (obj == null)
				{
					obj = Expression.Variable(parentExpression.Type);
					vars. Add(obj);
					exprs.Add(Expression.Assign(obj, parentExpression));
				}

				var complexMembers =
				(
					from c in columnInfo
					where c.IsComplex
					group c by c.Name.Substring(0, c.Name.IndexOf('.')) into gr
					join ma in parentAccessor.Members on gr.Key equals ma.Name
					select new
					{
						memberAccessor = ma,
						columns        = gr.ToList()
					}
				).ToList();

				foreach (var complexMember in complexMembers)
				{
					var columns = complexMember.columns.Select(c =>
					{
						var name = c.Name.Substring(complexMember.memberAccessor.Name.Length + 1);
						return new ColumnInfo
						{
							IsComplex      = name.IndexOf('.') >= 0,
							Name           = name,
							Storage        = c.Storage,
							ReadExpression = c.ReadExpression,
						};
					}).ToList();

					var memberExpression   = Expression.MakeMemberAccess(obj, complexMember.memberAccessor.MemberInfo);
					var memberTypeAccessor = TypeAccessor.GetAccessor(complexMember.memberAccessor.Type);

					exprs.Add(BuildComplexMemberExpression(memberTypeAccessor, memberExpression, columns));
				}

				exprs.Add(obj);

				return Expression.Block(vars, exprs);
			}

			Expression BuildDefaultConstructor(TypeAccessor typeAccessor, List<ColumnInfo> columnInfo)
			{
				Expression expr = Expression.MemberInit(
					Expression.New(typeAccessor.Type),
					columnInfo
						.Where (c => !c.IsComplex)
						.Select(c => (MemberBinding)Expression.Bind(
							Expression.PropertyOrField(
								Expression.Constant(null, typeAccessor.Type),
								c.Storage ?? c.Name).Member,
							c.ReadExpression)));

				return BuildComplexMembers(typeAccessor, expr, columnInfo);
			}

			Expression BuildRecordConstructor(TypeAccessor typeAccessor, List<ColumnInfo> columnInfo)
			{
				var members =
				(
					from member       in typeAccessor.Members
					from dynamic attr in _builder.MappingSchema.GetAttributes<Attribute>(member.MemberInfo).Where(IsRecordAttribute).Take(1)
					select new
					{
						member,
						attr.SequenceNumber,
					}
				).ToList();

				var complexMembers =
					from c in columnInfo
					where c.IsComplex
					let name = c.Name.Substring(0, c.Name.IndexOf('.'))
					group c by name into gr
					select new
					{
						name    = gr.Key,
						columns = gr.ToList()
					};

				var exprs =
				(
					from m in members
					join c in
						from c in columnInfo
						where !c.IsComplex
						select c
					on m.member.Name equals c.Name
					select new
					{
						m.SequenceNumber,
						Expression = c.ReadExpression,
					}
				)
				.Union
				(
					from m in members
					join c in complexMembers
					on m.member.Name equals c.name
					select new
					{
						m.SequenceNumber,
						Expression = BuildEntityExpression(
							TypeAccessor.GetAccessor(m.member.Type),
							(
								from ci in c.columns
								let name = ci.Name.Substring(c.name.Length + 1)
								select new ColumnInfo
								{
									IsComplex      = name.IndexOf('.') >= 0,
									Name           = name,
									Storage        = ci.Storage,
									ReadExpression = ci.ReadExpression,
								}
							).ToList()),
					}
				);

				var ctor  = typeAccessor.Type.GetConstructorsEx().Single();
				var parms =
				(
					from p in ctor.GetParameters().Select((p,i) => new { p, i })
					join e in exprs on p.i equals e.SequenceNumber into j
					from e in j.DefaultIfEmpty()
					select e != null ?
						e.Expression :
						Expression.Constant(p.p.DefaultValue ?? _builder.MappingSchema.GetDefaultValue(p.p.ParameterType), p.p.ParameterType)
				).ToList();

				var expr = Expression.New(ctor, parms);

				return expr;
			}

			#endregion
		}

		#endregion
	}
}
