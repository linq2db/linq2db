using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	partial class TableBuilder
	{
		static IBuildContext BuildCteContext(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var methodCall = (MethodCallExpression)buildInfo.Expression;

			Expression  bodyExpr;
			IQueryable? query = null;
			string?     name  = null;
			bool        isRecursive = false;

			switch (methodCall.Arguments.Count)
			{
				case 1 :
					bodyExpr = methodCall.Arguments[0].Unwrap();
					break;
				case 2 :
					bodyExpr = methodCall.Arguments[0].Unwrap();
					name     = methodCall.Arguments[1].EvaluateExpression() as string;
					break;
				case 3 :
					query    = methodCall.Arguments[0].EvaluateExpression() as IQueryable;
					bodyExpr = methodCall.Arguments[1].Unwrap();
					name     = methodCall.Arguments[2].EvaluateExpression() as string;
					isRecursive = true;
					break;
				default:
					throw new InvalidOperationException();
			}

			builder.RegisterCte(query, bodyExpr, () => new CteClause(null, bodyExpr.Type.GetGenericArguments()[0], isRecursive, name));

			var cte = builder.BuildCte(bodyExpr,
				cteClause =>
				{
					var info      = new BuildInfo(buildInfo, bodyExpr, new SelectQuery());
					var sequence  = builder.BuildSequence(info);

					if (cteClause == null)
						cteClause = new CteClause(sequence.SelectQuery, bodyExpr.Type.GetGenericArguments()[0], isRecursive, name);
					else
					{
						cteClause.Body = sequence.SelectQuery;
						cteClause.Name = name;
					}

					return Tuple.Create(cteClause, (IBuildContext?)sequence);
				}
			);

			var cteBuildInfo = new BuildInfo(buildInfo, bodyExpr, buildInfo.SelectQuery);
			var cteContext   = new CteTableContext(builder, cteBuildInfo, cte.Item1, bodyExpr);

			// populate all fields
			if (isRecursive)
				cteContext.ConvertToSql(null, 0, ConvertFlags.All);

			return cteContext;
		}

		static CteTableContext BuildCteContextTable(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var queryable    = (IQueryable)buildInfo.Expression.EvaluateExpression()!;
			var cteInfo      = builder.RegisterCte(queryable, null, () => new CteClause(null, queryable.ElementType, false, ""));
			var cteBuildInfo = new BuildInfo(buildInfo, cteInfo.Item3, buildInfo.SelectQuery);
			var cteContext   = new CteTableContext(builder, cteBuildInfo, cteInfo.Item1, cteInfo.Item3);

			return cteContext;
		}

		class CteTableContext : TableContext
		{
			private readonly CteClause      _cte;
			private readonly Expression     _cteExpression;
			private          IBuildContext? _cteQueryContext;

			public CteTableContext(ExpressionBuilder builder, BuildInfo buildInfo, CteClause cte, Expression cteExpression)
				: base(builder, buildInfo, new SqlCteTable(builder.MappingSchema, cte))
			{
				_cte             = cte;
				_cteExpression   = cteExpression;
			}

			IBuildContext? GetQueryContext()
			{
				return _cteQueryContext ?? (_cteQueryContext = Builder.GetCteContext(_cteExpression));
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFor)
			{
				var queryContext = GetQueryContext();
				if (queryContext == null)
					return base.IsExpression(expression, level, requestFor);
				return queryContext.IsExpression(expression, level, requestFor);
			}

			static string? GenerateAlias(Expression? expression)
			{
				string? alias = null;
				var current   = expression;
				while (current?.NodeType == ExpressionType.MemberAccess)
				{
					var ma = (MemberExpression) current;
					alias = alias == null ? ma.Member.Name : ma.Member.Name + "_"  + alias;
					current = ma.Expression;
				}

				return alias;
			}

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				var queryContext = GetQueryContext();
				if (queryContext == null)
					return base.ConvertToSql(expression, level, flags);

				var baseInfos = base.ConvertToSql(null, 0, ConvertFlags.All);

				if (flags != ConvertFlags.All && _cte.Fields!.Length == 0 && queryContext.SelectQuery.Select.Columns.Count > 0)
				{
					// it means that queryContext context already has columns and we need all of them. For example for Distinct.
					ConvertToSql(null, 0, ConvertFlags.All);
				}

				var infos  = queryContext.ConvertToIndex(expression, level, flags);

				var result = infos
					.Select(info =>
					{
						var baseInfo = baseInfos.FirstOrDefault(bi => bi.CompareMembers(info));
						var alias    = flags == ConvertFlags.Field ? GenerateAlias(expression) : null;
						if (alias == null)
						{
							alias = baseInfo?.MemberChain.LastOrDefault()?.Name ??
						                  info.MemberChain.LastOrDefault()?.Name;
						}	
						var field    = RegisterCteField(baseInfo?.Sql, info.Sql, info.Index, alias);
						return new SqlInfo(info.MemberChain)
						{
							Sql = field,
						};
					})
					.ToArray();
				return result;
			}

			static string? GetColumnFriendlyAlias(SqlColumn column)
			{
				string? alias = null;

				var visited = new HashSet<ISqlExpression>();
				ISqlExpression current = column;
				while (current is SqlColumn clmn && !visited.Contains(clmn))
				{
					if (clmn.RawAlias != null)
					{
						alias = clmn.RawAlias;
						break;
					}
					visited.Add(clmn);
					current = clmn.Expression;
				}

				if (alias == null)
				{
					var field = current as SqlField;

					alias = field?.Name;
				}

				if (alias == null)
					alias = column.Alias;

				return alias;
			}

			void UpdateMissingFields()
			{
				// Collecting missed fields which has field in query. Should never happen.

				if (_cteQueryContext != null)
				{
					for (int i = 0; i < _cte.Fields!.Length; i++)
					{
						if (_cte.Fields[i] == null)
						{
							var column = _cte.Body!.Select.Columns[i];
							_cte.Fields[i] = new SqlField(column.Alias!, column.Alias!);
						}
					}
				}
			}

			public override int ConvertToParentIndex(int index, IBuildContext? context)
			{
				if (context == _cteQueryContext)
				{
					var queryColumn = context!.SelectQuery.Select.Columns[index];
					var alias       = GetColumnFriendlyAlias(queryColumn);
					var field       = RegisterCteField(null, queryColumn, index, alias);

					index = SelectQuery.Select.Add(field);

					UpdateMissingFields();
				}

				return base.ConvertToParentIndex(index, context);
			}

			SqlField RegisterCteField(ISqlExpression? baseExpression, ISqlExpression expression, int index, string? alias)
			{
				if (expression == null) throw new ArgumentNullException(nameof(expression));

				var cteField = _cte.RegisterFieldMapping(index, () =>
				{
					var f = QueryHelper.GetUnderlyingField(baseExpression ?? expression);

					var newField = f == null
						? new SqlField(expression.SystemType!, alias, expression.CanBeNull)
						: new SqlField(f);

					if (alias != null)
						newField.Name = alias;

					newField.PhysicalName = newField.Name;
					return newField;
				});

				if (!SqlTable.Fields.TryGetValue(cteField.Name!, out var field))
				{
					field = new SqlField(cteField);
					SqlTable.Add(field);
				}

				return field;
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var queryContext = GetQueryContext();
				if (queryContext == null)
					base.BuildQuery(query, queryParameter);
				else
				{
					queryContext.Parent = this;
					queryContext.BuildQuery(query, queryParameter);
				}
			}

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				var queryContext = GetQueryContext();
				if (queryContext == null)
					return base.BuildExpression(expression, level, enforceServerSide);

				queryContext.Parent = this;
				return queryContext.BuildExpression(expression, level, true);
			}

			public override SqlStatement GetResultStatement()
			{
				if (_cte.Fields!.Length == 0)
				{
					ConvertToSql(null, 0, ConvertFlags.Key);
				}

				return base.GetResultStatement();
			}
		}
	}
}
