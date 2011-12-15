using System;
using System.Linq.Expressions;
using LinqToDB.Extensions;

namespace LinqToDB.Data.Linq.Builder
{
	using Data.Sql;

	class InsertBuilder : MethodCallBuilder
	{
		#region InsertBuilder

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("Insert", "InsertWithIdentity");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var isSubQuery = sequence.SqlQuery.Select.IsDistinct;

			if (isSubQuery)
				sequence = new SubQueryContext(sequence);

			switch (methodCall.Arguments.Count)
			{
				case 1 : 
					// static int Insert<T>              (this IValueInsertable<T> source)
					// static int Insert<TSource,TTarget>(this ISelectInsertable<TSource,TTarget> source)
					{
						foreach (var item in sequence.SqlQuery.Insert.Items)
							sequence.SqlQuery.Select.Expr(item.Expression);
						break;
					}

				case 2 : // static int Insert<T>(this Table<T> target, Expression<Func<T>> setter)
					{
						UpdateBuilder.BuildSetter(
							builder,
							buildInfo,
							(LambdaExpression)methodCall.Arguments[1].Unwrap(),
							sequence,
							sequence.SqlQuery.Insert.Items,
							sequence);

						sequence.SqlQuery.Insert.Into  = ((TableBuilder.TableContext)sequence).SqlTable;
						sequence.SqlQuery.From.Tables.Clear();

						break;
					}

				case 3 : // static int Insert<TSource,TTarget>(this IQueryable<TSource> source, Table<TTarget> target, Expression<Func<TSource,TTarget>> setter)
					{
						var into = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SqlQuery()));

						UpdateBuilder.BuildSetter(
							builder,
							buildInfo,
							(LambdaExpression)methodCall.Arguments[2].Unwrap(),
							into,
							sequence.SqlQuery.Insert.Items,
							sequence);

						sequence.SqlQuery.Select.Columns.Clear();

						foreach (var item in sequence.SqlQuery.Insert.Items)
							sequence.SqlQuery.Select.Columns.Add(new SqlQuery.Column(sequence.SqlQuery, item.Expression));

						sequence.SqlQuery.Insert.Into = ((TableBuilder.TableContext)into).SqlTable;

						break;
					}
			}

			sequence.SqlQuery.QueryType           = QueryType.Insert;
			sequence.SqlQuery.Insert.WithIdentity = methodCall.Method.Name == "InsertWithIdentity";

			return new InsertContext(buildInfo.Parent, sequence, sequence.SqlQuery.Insert.WithIdentity);
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		#endregion

		#region InsertContext

		class InsertContext : SequenceContextBase
		{
			public InsertContext(IBuildContext parent, IBuildContext sequence, bool insertWithIdentity)
				: base(parent, sequence, null)
			{
				_insertWithIdentity = insertWithIdentity;
			}

			readonly bool _insertWithIdentity;

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				if (_insertWithIdentity) query.SetScalarQuery<object>();
				else                     query.SetNonQueryQuery();
			}

			public override Expression BuildExpression(Expression expression, int level)
			{
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override IsExpressionResult IsExpression(Expression expression, int level, RequestFor requestFlag)
			{
				throw new NotImplementedException();
			}

			public override IBuildContext GetContext(Expression expression, int level, BuildInfo buildInfo)
			{
				throw new NotImplementedException();
			}
		}

		#endregion

		#region Into

		internal class Into : MethodCallBuilder
		{
			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				return methodCall.IsQueryable("Into");
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				var source = methodCall.Arguments[0].Unwrap();
				var into   = methodCall.Arguments[1].Unwrap();

				IBuildContext sequence;

				// static IValueInsertable<T> Into<T>(this IDataContext dataContext, Table<T> target)
				//
				if (source.NodeType == ExpressionType.Constant && ((ConstantExpression)source).Value == null)
				{
					sequence = builder.BuildSequence(new BuildInfo((IBuildContext)null, into, new SqlQuery()));
					sequence.SqlQuery.Insert.Into = ((TableBuilder.TableContext)sequence).SqlTable;
					sequence.SqlQuery.From.Tables.Clear();
				}
				// static ISelectInsertable<TSource,TTarget> Into<TSource,TTarget>(this IQueryable<TSource> source, Table<TTarget> target)
				//
				else
				{
					sequence = builder.BuildSequence(new BuildInfo(buildInfo, source));
					var tbl = builder.BuildSequence(new BuildInfo((IBuildContext)null, into, new SqlQuery()));
					sequence.SqlQuery.Insert.Into = ((TableBuilder.TableContext)tbl).SqlTable;
				}

				sequence.SqlQuery.Select.Columns.Clear();

				return sequence;
			}

			protected override SequenceConvertInfo Convert(
				ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
			{
				return null;
			}
		}

		#endregion

		#region Value

		internal class Value : MethodCallBuilder
		{
			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				return methodCall.IsQueryable("Value");
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
				var extract  = (LambdaExpression)methodCall.Arguments[1].Unwrap();
				var update   =                   methodCall.Arguments[2].Unwrap();

				if (sequence.SqlQuery.Insert.Into == null)
				{
					sequence.SqlQuery.Insert.Into = (SqlTable)sequence.SqlQuery.From.Tables[0].Source;
					sequence.SqlQuery.From.Tables.Clear();
				}

				if (update.NodeType == ExpressionType.Lambda)
					UpdateBuilder.ParseSet(
						builder,
						buildInfo,
						extract,
						(LambdaExpression)update,
						sequence,
						sequence.SqlQuery.Insert.Into,
						sequence.SqlQuery.Insert.Items);
				else
					UpdateBuilder.ParseSet(
						builder,
						buildInfo,
						extract,
						update,
						sequence,
						sequence.SqlQuery.Insert.Items);

				return sequence;
			}

			protected override SequenceConvertInfo Convert(
				ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
			{
				return null;
			}
		}

		#endregion
	}
}
