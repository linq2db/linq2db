﻿using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	class CountBuilder : MethodCallBuilder
	{
		public  static readonly string[] MethodNames      = { "Count"     , "LongCount"      };
		private static readonly string[] MethodNamesAsync = { "CountAsync", "LongCountAsync" };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable(MethodNames) || methodCall.IsAsyncExtension(MethodNamesAsync);
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence   = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]) { CreateSubQuery = true });
			var returnType = methodCall.Method.ReturnType;

			if (methodCall.IsAsyncExtension())
				returnType = returnType.GetGenericArguments()[0];

			if (sequence.SelectQuery != buildInfo.SelectQuery)
			{
				if (sequence is JoinBuilder.GroupJoinSubQueryContext)
				{
					var ctx = new CountContext(buildInfo.Parent, sequence, returnType)
					{
						SelectQuery =
							sequence.SelectQuery
							//((JoinBuilder.GroupJoinSubQueryContext)sequence).GetCounter(methodCall)
					};

					ctx.Sql        = ctx.SelectQuery;
					ctx.FieldIndex = ctx.SelectQuery.Select.Add(SqlFunction.CreateCount(returnType, ctx.SelectQuery), "cnt");

					return ctx;
				}

				if (sequence is GroupByBuilder.GroupByContext)
				{
//					var ctx = new CountContext(buildInfo.Parent, sequence, returnType);
//
//					ctx.Sql        = ctx.SelectQuery;
//					ctx.FieldIndex = ctx.SelectQuery.Select.Add(SqlFunction.CreateCount(returnType, ctx.SelectQuery), "cnt");
//
//					return ctx;

//					return new CountContext(buildInfo.Parent, sequence, returnType)
//					{
//						Sql        = SqlFunction.CreateCount(returnType, sequence.SelectQuery),
//						FieldIndex = -1
//					};
				}
			}

			if (sequence.SelectQuery.Select.IsDistinct        ||
			    sequence.SelectQuery.Select.TakeValue != null ||
			    sequence.SelectQuery.Select.SkipValue != null)
			{
				sequence.ConvertToIndex(null, 0, ConvertFlags.Key);
				sequence = new SubQueryContext(sequence);
			}
			else if (!sequence.SelectQuery.GroupBy.IsEmpty)
			{
				if (!builder.DataContext.SqlProviderFlags.IsSybaseBuggyGroupBy)
					sequence.SelectQuery.Select.Add(new SqlValue(0));
				else
					foreach (var item in sequence.SelectQuery.GroupBy.Items)
						sequence.SelectQuery.Select.Add(item);

				sequence = new SubQueryContext(sequence);
			}

			if (sequence.SelectQuery.OrderBy.Items.Count > 0)
			{
				if (sequence.SelectQuery.Select.TakeValue == null && sequence.SelectQuery.Select.SkipValue == null)
					sequence.SelectQuery.OrderBy.Items.Clear();
				else
					sequence = new SubQueryContext(sequence);
			}

			var context = new CountContext(buildInfo.Parent, sequence, returnType);

			context.Sql        = context.SelectQuery;
			context.FieldIndex = context.SelectQuery.Select.Add(SqlFunction.CreateCount(returnType, context.SelectQuery), "cnt");

			return context;
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		internal class CountContext : SequenceContextBase
		{
			public CountContext(IBuildContext? parent, IBuildContext sequence, Type returnType)
				: base(parent, sequence, null)
			{
				_returnType = returnType;
			}

			readonly Type       _returnType;
			private  SqlInfo[]? _index;

			public int             FieldIndex;
			public ISqlExpression? Sql;

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var expr   = Builder.BuildSql(_returnType, FieldIndex, Sql);
				var mapper = Builder.BuildMapper<object>(expr);

				CompleteColumns();
				QueryRunner.SetRunQuery(query, mapper);
			}

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				var info  = ConvertToIndex(expression, level, ConvertFlags.Field)[0];
				var index = info.Index;
				if (Parent != null)
					index = ConvertToParentIndex(index, Parent);
				return Builder.BuildSql(_returnType, index, info.Sql);
			}

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				return flags switch
				{
					ConvertFlags.Field => new[] { new SqlInfo(Sql!, Parent!.SelectQuery) },
					_                  => throw new NotImplementedException(),
				};
			}

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				return flags switch
				{
					ConvertFlags.Field => 
						_index ??= new[]
						{
							new SqlInfo(Sql!, Parent!.SelectQuery, Parent.SelectQuery.Select.Add(Sql!))
						},
					_ => throw new NotImplementedException(),
				};
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				return requestFlag switch
				{
					RequestFor.Expression => IsExpressionResult.True,
					_                     => IsExpressionResult.False,
				};
			}

			public override IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				return Sequence.GetContext(expression, level, buildInfo);
			}

			public override ISqlExpression? GetSubQuery(IBuildContext context)
			{
				var query = context.SelectQuery;

				if (query == SelectQuery)
				{
					var col = query.Select.Columns[query.Select.Columns.Count - 1];

					query.Select.Columns.RemoveAt(query.Select.Columns.Count - 1);

					return col.Expression;
				}

				return null;
			}
		}
	}
}
