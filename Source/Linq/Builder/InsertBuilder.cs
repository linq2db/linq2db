using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

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

			var isSubQuery = sequence.SelectQuery.Select.IsDistinct;

			if (isSubQuery)
				sequence = new SubQueryContext(sequence);

			switch (methodCall.Arguments.Count)
			{
				case 1 : 
					// static int Insert<T>              (this IValueInsertable<T> source)
					// static int Insert<TSource,TTarget>(this ISelectInsertable<TSource,TTarget> source)
					{
						foreach (var item in sequence.SelectQuery.Insert.Items)
							sequence.SelectQuery.Select.Expr(item.Expression);
						break;
					}

				case 2 : // static int Insert<T>(this Table<T> target, Expression<Func<T>> setter)
					{
						UpdateBuilder.BuildSetter(
							builder,
							buildInfo,
							(LambdaExpression)methodCall.Arguments[1].Unwrap(),
							sequence,
							sequence.SelectQuery.Insert.Items,
							sequence);

						sequence.SelectQuery.Insert.Into = ((TableBuilder.TableContext)sequence).SqlTable;
						sequence.SelectQuery.From.Tables.Clear();

						break;
					}

				case 3 : // static int Insert<TSource,TTarget>(this IQueryable<TSource> source, Table<TTarget> target, Expression<Func<TSource,TTarget>> setter)
					{
						var into = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()));

						UpdateBuilder.BuildSetter(
							builder,
							buildInfo,
							(LambdaExpression)methodCall.Arguments[2].Unwrap(),
							into,
							sequence.SelectQuery.Insert.Items,
							sequence);

						sequence.SelectQuery.Select.Columns.Clear();

						foreach (var item in sequence.SelectQuery.Insert.Items)
							sequence.SelectQuery.Select.Columns.Add(new SelectQuery.Column(sequence.SelectQuery, item.Expression));

						sequence.SelectQuery.Insert.Into = ((TableBuilder.TableContext)into).SqlTable;

						break;
					}
			}

			var insert = sequence.SelectQuery.Insert;

			var q = insert.Into.Fields.Values
#if SL4
				.Cast<ISqlExpression>()
#endif
				.Except(insert.Items.Select(e => e.Column))
				.OfType<SqlField>()
				.Where(f => f.IsIdentity);

			foreach (var field in q)
			{
				var expr = builder.DataContext.CreateSqlProvider().GetIdentityExpression(insert.Into);

				if (expr != null)
				{
					insert.Items.Insert(0, new SelectQuery.SetExpression(field, expr));

					if (methodCall.Arguments.Count == 3)
					{
						sequence.SelectQuery.Select.Columns.Insert(0, new SelectQuery.Column(sequence.SelectQuery, insert.Items[0].Expression));
					}
				}
			}

			sequence.SelectQuery.QueryType           = QueryType.Insert;
			sequence.SelectQuery.Insert.WithIdentity = methodCall.Method.Name == "InsertWithIdentity";

			return new InsertContext(buildInfo.Parent, sequence, sequence.SelectQuery.Insert.WithIdentity);
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
				if (_insertWithIdentity) QueryRunner.SetScalarQuery  (query);
				else                     QueryRunner.SetNonQueryQuery(query);
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
					sequence = builder.BuildSequence(new BuildInfo((IBuildContext)null, into, new SelectQuery()));

					if (sequence.SelectQuery.Select.IsDistinct)
						sequence = new SubQueryContext(sequence);

					sequence.SelectQuery.Insert.Into = ((TableBuilder.TableContext)sequence).SqlTable;
					sequence.SelectQuery.From.Tables.Clear();
				}
				// static ISelectInsertable<TSource,TTarget> Into<TSource,TTarget>(this IQueryable<TSource> source, Table<TTarget> target)
				//
				else
				{
					sequence = builder.BuildSequence(new BuildInfo(buildInfo, source));

					if (sequence.SelectQuery.Select.IsDistinct)
						sequence = new SubQueryContext(sequence);

					var tbl = builder.BuildSequence(new BuildInfo((IBuildContext)null, into, new SelectQuery()));
					sequence.SelectQuery.Insert.Into = ((TableBuilder.TableContext)tbl).SqlTable;
				}

				sequence.SelectQuery.Select.Columns.Clear();

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

				if (sequence.SelectQuery.Insert.Into == null)
				{
					sequence.SelectQuery.Insert.Into = (SqlTable)sequence.SelectQuery.From.Tables[0].Source;
					sequence.SelectQuery.From.Tables.Clear();
				}

				if (update.NodeType == ExpressionType.Lambda)
					UpdateBuilder.ParseSet(
						builder,
						buildInfo,
						extract,
						(LambdaExpression)update,
						sequence,
						sequence.SelectQuery.Insert.Into,
						sequence.SelectQuery.Insert.Items);
				else
					UpdateBuilder.ParseSet(
						builder,
						buildInfo,
						extract,
						update,
						sequence,
						sequence.SelectQuery.Insert.Items);

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
