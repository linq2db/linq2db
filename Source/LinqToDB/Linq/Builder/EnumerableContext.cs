using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Extensions;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
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
			Expression source)
		{
			Parent            = buildInfo.Parent;
			Builder           = builder;
			Expression        = buildInfo.Expression;
			SelectQuery       = query;
			_elementType      = elementType;
			_entityDescriptor = Builder.MappingSchema.GetEntityDescriptor(elementType);
			Table             = new SqlValuesTable(source);
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
				var query = SelectQuery;
				var sql   = SelectQuery.Select.Columns[0];

				if (Parent != null)
					query = Parent.SelectQuery;

				return new[] { new SqlInfo(sql, query) };
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
								if (!Table.FieldsLookup!.TryGetValue(column.MemberInfo.Name, out var newField))
								{
									Table.Add(newField = new SqlField(column), (record, parameters) =>
									{
										// TODO: improve this place to avoid closures
										object? value;
										if (column.MemberInfo.IsPropertyEx())
											value = ((PropertyInfo)column.MemberInfo).GetValue(record);
										else if (column.MemberInfo.IsFieldEx())
											value = ((FieldInfo)column.MemberInfo).GetValue(record);
										else
											throw new InvalidOperationException();

										var valueExpr = Expression.Constant(value, column.MemberType);
										if (parameters.TryGetValue(valueExpr, out var parameter))
											return parameter;

										// TODO: parameter accessor is overkill here for disposable parameter
										// we need method to create parameter value directly with all conversions
										var sql = Builder.ConvertToSqlExpression(Parent!, valueExpr, column);
										if (sql is SqlParameter p)
										{
											// TODO: ConvertToSqlExpression should set type using column type
											p.Type = p.Type.WithoutSystemType(column);
											p.IsQueryParameter = !Builder.MappingSchema.ValueToSqlConverter.CanConvert(p.Type.SystemType);
											foreach (var pa in Builder._parameters.Values)
											{
												if (pa.SqlParameter == p)
												{
													p.Value = pa.ValueAccessor(pa.Expression, null, null);
													break;
												}
											}
										}
										else if (sql is SqlValue val)
											// TODO: ConvertToSqlExpression should set type using column type
											val.ValueType = val.ValueType.WithoutSystemType(column);

										parameters.Add(valueExpr, sql);

										return sql;
									});
								}

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

		public SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
		{
			var sql = ConvertToSql(expression, level, flags);

			if (sql[0].Index < 0)
				sql[0] = sql[0].WithIndex(sql[0].Query!.Select.Add(sql[0].Sql));

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
	}
}
