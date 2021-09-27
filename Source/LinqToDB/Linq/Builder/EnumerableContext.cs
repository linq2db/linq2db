using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using Data;
	using LinqToDB.Expressions;
	using Extensions;
	using Mapping;
	using SqlQuery;

	// based on ArrayContext
	[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
	class EnumerableContext : IBuildContext
	{
		readonly Type _elementType;

#if DEBUG
		public string?               _sqlQueryText { get; }
		public string                Path          => this.GetPath();
#endif
		public  ExpressionBuilder    Builder       { get; }
		public  Expression           Expression    { get; }
		public  SelectQuery          SelectQuery   { get; set; }
		public  SqlStatement?        Statement     { get; set; }
		public  IBuildContext?       Parent        { get; set; }

		private readonly EntityDescriptor _entityDescriptor;

		public SqlValuesTable Table { get; }

		public EnumerableContext(ExpressionBuilder builder, BuildInfo buildInfo, SelectQuery query, Type elementType,
			ISqlExpression source)
		{
			Parent            = buildInfo.Parent;
			Builder           = builder;
			Expression        = buildInfo.Expression;
			SelectQuery       = query;
			_elementType      = elementType;
			_entityDescriptor = Builder.MappingSchema.GetEntityDescriptor(elementType);
			Table             = new SqlValuesTable(source);
			SelectQuery.From.Table(Table);
		}

		public void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
		{
			throw new NotImplementedException();
		}

		public Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
		{
			var info  = ConvertToIndex(expression, level, ConvertFlags.Field)[0];
			var index = info.Index;
			if (Parent != null)
				index = ConvertToParentIndex(index, Parent);
			return Builder.BuildSql(_elementType, index, info.Sql);
		}

		public SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
		{
			if (expression == null)
			{
				SqlInfo[] sql;
				if (Builder.MappingSchema.IsScalarType(_elementType))
				{
					SqlField field;
					if (Table.Fields.Count > 0)
					{
						field = Table.Fields[0];
					}
					else
					{
						field = new SqlField(_elementType, "item", true);
						var param = Expression.Parameter(typeof(object), "record");
						var body = Expression.New(_sqlValueconstructor,
							Expression.Constant(new DbDataType(_elementType,
								ColumnDescriptor.CalculateDataType(Builder.MappingSchema, _elementType))),
							param);

						var getterLambda = Expression.Lambda<Func<object, ISqlExpression>>(body, param);
						var getterFunc   = getterLambda.Compile();
						Table.Add(field, getterFunc);
					}

					sql = new[] { new SqlInfo(field, SelectQuery) };
				}
				else
				{
					sql = _entityDescriptor.Columns
						.Select(c => new SqlInfo(c.MemberInfo, BuildField(c), SelectQuery)).ToArray();

					if (sql.Length == 0)
						throw new LinqToDBException($"Entity of type '{_elementType.Name}' as no defined columns.");
				}

				return sql;
			}

			switch (flags)
			{
				case ConvertFlags.Field:
				{
					if (expression.NodeType == ExpressionType.MemberAccess)
					{
						var memberExpression = (MemberExpression)expression;

						foreach (var column in _entityDescriptor.Columns)
						{
							if (column.MemberInfo.EqualsTo(memberExpression.Member, _elementType))
							{
								var newField = BuildField(column);

								return new[]
								{
									new SqlInfo(column.MemberInfo, newField, SelectQuery)
								};
							}
						}
					}

					break;
				}
			}

			throw new NotImplementedException();
		}

		private static ConstructorInfo _parameterConstructor =
			MemberHelper.ConstructorOf(() => new SqlParameter(new DbDataType(typeof(object)), "", null));

		private static ConstructorInfo _sqlValueconstructor =
			MemberHelper.ConstructorOf(() => new SqlValue(new DbDataType(typeof(object)), null));

		private SqlField BuildField(ColumnDescriptor column)
		{
			var memberName = column.MemberName;
			if (!Table.FieldsLookup!.TryGetValue(memberName, out var newField))
			{
				var getter = column.GetDbParamLambda();

				var generator = new ExpressionGenerator();
				if (typeof(DataParameter).IsSameOrParentOf(getter.Body.Type))
				{
					
					var variable  = generator.AssignToVariable(getter.Body);
					generator.AddExpression(Expression.New(_parameterConstructor,
						Expression.Property(variable, nameof(DataParameter.DataType),
							Expression.Constant(memberName),
							Expression.Property(variable, nameof(DataParameter.Value))
						)));
				}
				else
				{
					generator.AddExpression(Expression.New(_sqlValueconstructor,
						Expression.Constant(column.GetDbDataType(true)),
						Expression.Convert(getter.Body, typeof(object))));
				}

				var param = Expression.Parameter(typeof(object), "e");

				var body = generator.Build();
				body = body.Replace(getter.Parameters[0], Expression.Convert(param, _elementType));

				var getterLambda = Expression.Lambda<Func<object, ISqlExpression>>(body, param);
				var getterFunc   = getterLambda.Compile();

				Table.Add(newField = new SqlField(column), getterFunc);
			}

			return newField;
		}

		public SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
		{
			var sql = ConvertToSql(expression, level, flags);

			var sqlInfo = sql[0];
			if (sqlInfo.Index < 0)
			{
				var idx = sqlInfo.Query!.Select.Add(sqlInfo.Sql);
				sql[0] = sqlInfo.WithIndex(idx).WithSql(sqlInfo.Query!.Select.Columns[idx]);
			}

			return sql;
		}

		public IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
		{
			if (expression == null)
			{
				switch (requestFlag)
				{
					case RequestFor.Expression:
					case RequestFor.Field: return IsExpressionResult.False;
					case RequestFor.Object:
						return new IsExpressionResult(!Builder.MappingSchema.IsScalarType(_elementType));
				}
			}

			return IsExpressionResult.False;
		}

		public IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
		{
			throw new NotImplementedException();
		}

		public int ConvertToParentIndex(int index, IBuildContext context)
		{
			throw new NotImplementedException();
		}

		public void SetAlias(string alias)
		{
			SelectQuery.Select.Columns[0].Alias = alias;
		}

		public ISqlExpression GetSubQuery(IBuildContext context)
		{
			throw new NotImplementedException();
		}

		public SqlStatement GetResultStatement()
		{
			throw new NotImplementedException();
		}

		public void CompleteColumns()
		{
		}
	}
}
