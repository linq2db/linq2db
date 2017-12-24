using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB.Extensions;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	class ArrayBuilder : ISequenceBuilder
	{
		public int BuildCounter { get; set; }

		public bool CanBuild(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return Find(buildInfo, (i, t) => i) > 0;
		}

		static T Find<T>(BuildInfo buildInfo, Func<int, Type, T> action)
		{
			var expression = buildInfo.Expression;

			switch (expression.NodeType)
			{
				case ExpressionType.Constant:
					{
						var c = (ConstantExpression)expression;

						var type = c.Value.GetType();
						if (typeof(EnumerableQuery<>).IsSameOrParentOf(type))
							return action(1, type.GetGenericArguments(typeof(EnumerableQuery<>))[0]);
						if (typeof(Array).IsSameOrParentOf(type))
							return action(2, type.GetElementType());

						break;
					}

				case ExpressionType.NewArrayInit:
				{
					var newArray = (NewArrayExpression) expression;
					if (newArray.Expressions.Count > 0)
						return action(3, newArray.Expressions[0].Type);
					break;
				}

				case ExpressionType.Parameter:
					{
						break;
					}
			}

			return action(0, null);
		}

		static IEnumerable<ISqlExpression> BuildElements(ExpressionBuilder builder, BuildInfo buildInfo, IEnumerable<Expression> elements)
		{
			foreach (var itemExpr in elements)
			{
				var ctx = builder.GetContext(buildInfo.Parent, itemExpr);
				if (ctx == null) ;
					ctx = buildInfo.Parent;

				yield return ctx.ConvertToSql(itemExpr, 0, ConvertFlags.Field)[0].Sql;
			}
		}

		static IEnumerable<ISqlExpression> BuildElements(Type type, IEnumerable elements)
		{
			return elements.OfType<object>().Select(o => new SqlValue(type, o));
		}

		public IBuildContext BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var sequence = Find(buildInfo, (index, type) =>
			{
				var query      = buildInfo.SelectQuery;
				var innerQuery = new SelectQuery();
				innerQuery.ParentSelect = query;
				query.Select.From.Table(innerQuery);

				var array = new ArrayContext(builder, buildInfo, query, type);
				IEnumerable<ISqlExpression> elements;

				switch (index)
				{
					case 1:
					case 2:
						elements = BuildElements(type, (IEnumerable)((ConstantExpression)buildInfo.Expression).Value);
						break;
					case 3:
//						buildInfo.JoinType = JoinType.CrossApply;
						elements = BuildElements(builder, buildInfo, ((NewArrayExpression)buildInfo.Expression).Expressions);
						break;
					default:
						throw new InvalidOperationException();
				}

				var isFirst = true;
				foreach (var itemSql in elements)
				{
					var currentQuery = isFirst ? innerQuery : new SelectQuery();
					currentQuery.Select.AddNew(itemSql);

					if (!isFirst)
					{
						innerQuery.AddUnion(currentQuery, true);
					}

					isFirst = false;
				}

				query.Select.Columns.Add(new SqlColumn(query, innerQuery.Select.Columns[0], "Item"));

				return array;
			});

			return sequence;
		}


		public SequenceConvertInfo Convert(ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}


		class ArrayContext : IBuildContext
		{
			private readonly Type _elementType;
#if DEBUG
			public string _sqlQueryText { get; }
#endif
			public ExpressionBuilder Builder { get; }
			public Expression Expression { get; }
			public SelectQuery SelectQuery { get; set; }
			public SqlStatement Statement { get; set; }
			public IBuildContext Parent { get; set; }

			public ArrayContext(ExpressionBuilder builder, BuildInfo buildInfo, SelectQuery query, Type elementType)
			{
				Parent       = buildInfo.Parent;
				Builder      = builder;
				Expression   = buildInfo.Expression;
				SelectQuery  = query;
				_elementType = elementType;
			}

			public void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				throw new NotImplementedException();
			}

			public Expression BuildExpression(Expression expression, int level, bool enforceServerSide)
			{
				var index = ConvertToIndex(expression, level, ConvertFlags.Field)[0].Index;
				return Builder.BuildSql(_elementType, index);
			}

			public SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
			{
				if (expression == null)
				{
					var query = SelectQuery;
					var sql   = SelectQuery.Select.Columns[0];

					if (Parent != null)
						query = Parent.SelectQuery;

					return new[] { new SqlInfo { Query = query, Sql = sql } };
				}

				throw new NotImplementedException();
			}

			public SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
			{
				var sql = ConvertToSql(expression, level, flags);

				if (sql[0].Index < 0)
					sql[0].Index = sql[0].Query.Select.Add(sql[0].Sql);

				return sql;
			}

			public IsExpressionResult IsExpression(Expression expression, int level, RequestFor requestFlag)
			{
				if (expression == null)
				{
					switch (requestFlag)
					{
						case RequestFor.Expression :
						case RequestFor.Field      : return IsExpressionResult.False;
					}
				}

				return IsExpressionResult.False;
			}

			public IBuildContext GetContext(Expression expression, int level, BuildInfo buildInfo)
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
}
