using System;
using System.Linq.Expressions;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using LinqToDB.Extensions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

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

		public SqlValuesTable Table = new SqlValuesTable();
		private readonly IList<SqlValue> _records;

		public EnumerableContext(ExpressionBuilder builder, BuildInfo buildInfo, SelectQuery query, Type elementType,
			IList<SqlValue> records)
		{
			_records          = records;
			Parent            = buildInfo.Parent;
			Builder           = builder;
			Expression        = buildInfo.Expression;
			SelectQuery       = query;
			_elementType      = elementType;
			_entityDescriptor = Builder.MappingSchema.GetEntityDescriptor(elementType);
			for (var i        = 0; i < _records.Count; i++)
			{
				Table.Rows.Add(new List<ISqlExpression>());
			}
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
									if (!Table.Fields.TryGetValue(column.MemberInfo.Name, out var newField))
									{
										newField = new SqlField(column);

										Table.Add(newField);

										for (var i = 0; i < _records.Count; i++)
										{
											object? value;
											if (column.MemberInfo.IsPropertyEx())
											{
												value = ((PropertyInfo)column.MemberInfo).GetValue(_records[i].Value);
											}
											else if (column.MemberInfo.IsFieldEx())
											{
												value = ((FieldInfo)column.MemberInfo).GetValue(_records[i].Value);
											}
											else
											{
												throw new InvalidOperationException();
											}

											var valueExpr = Expression.Constant(value, column.MemberType);
											var expr = Builder.ConvertToSqlExpression(Parent!, valueExpr, column);

											if (expr is SqlParameter p)
											{
												// avoid parameters is source, because their number is limited
												p.IsQueryParameter = !Builder.MappingSchema.ValueToSqlConverter.CanConvert(p.Type.SystemType);
												p.Type             = p.Type.WithoutSystemType(column);
											}
											else if (expr is SqlValue val)
												val.ValueType = val.ValueType.WithoutSystemType(column);

											Table.Rows[i].Add(expr);
										}
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
