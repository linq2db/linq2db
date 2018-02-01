using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using SqlQuery;
	using LinqToDB.Expressions;

	class InsertBuilder : MethodCallBuilder
	{
		#region InsertBuilder

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("Insert", "InsertWithIdentity", "InsertWithOutput", "InsertWithOutputInto");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var isSubQuery = sequence.SelectQuery.Select.IsDistinct;

			if (isSubQuery)
				sequence = new SubQueryContext(sequence);

			if (!(sequence.Statement is SqlInsertStatement insertStatement))
			{
				insertStatement    = new SqlInsertStatement(sequence.SelectQuery);
				sequence.Statement = insertStatement;
			}

			IBuildContext into = null;

			var insertType = InsertContext.InsertType.Insert;

			switch (methodCall.Method.Name)
			{
				case "Insert"               : insertType = InsertContext.InsertType.Insert;             break;
				case "InsertWithIdentity"   : insertType = InsertContext.InsertType.InsertWithIdentity; break;
				case "InsertWithOutput"     : insertType = InsertContext.InsertType.InsertOutput;       break;
				case "InsertWithOutputInto" : insertType = InsertContext.InsertType.InsertOutputInto;   break;
			}

			var indexedParameters
				= methodCall.Method.GetParameters().Select((p, i) => Tuple.Create(p, i)).ToDictionary(t => t.Item1.Name, t => t.Item2);

			Expression GetArgumentByName(string name)
			{
				return methodCall.Arguments[indexedParameters[name]];
			}

			LambdaExpression GetOutputExpression(Type outputType)
			{
				int index;
				if (!indexedParameters.TryGetValue("outputExpression", out index))
				{
					var param = Expression.Parameter(methodCall.Method.GetGenericArguments()[1]);
					return Expression.Lambda(param, param);
				}

				return (LambdaExpression)methodCall.Arguments[index].Unwrap();
			}

			LambdaExpression outputExpression = null;

			if (methodCall.Arguments.Count > 0)
			{
				var argument = methodCall.Arguments[0];
				if (typeof(IValueInsertable<>).IsSameOrParentOf(argument.Type) ||
				    typeof(ISelectInsertable<,>).IsSameOrParentOf(argument.Type))
				{
					// static int Insert<T>              (this IValueInsertable<T> source)
					// static int Insert<TSource,TTarget>(this ISelectInsertable<TSource,TTarget> source)

					foreach (var item in insertStatement.Insert.Items)
						sequence.SelectQuery.Select.Expr(item.Expression);
				}
				else if (typeof(ITable<>).IsSameOrParentOf(argument.Type))
				{
					// static int Insert<T>(this Table<T> target, Expression<Func<T>> setter)
					// static TTarget InsertWithOutput<TTarget>(this ITable<TTarget> target, Expression<Func<TTarget>> setter)
					// static TTarget InsertWithOutput<TTarget>(this ITable<TTarget> target, Expression<Func<TTarget>> setter, Expression<Func<TTarget,TOutput>> outputExpression)

					var setter = (LambdaExpression)methodCall.Arguments[1].Unwrap();

					UpdateBuilder.BuildSetter(
						builder,
						buildInfo,
						setter,
						sequence,
						insertStatement.Insert.Items,
						sequence);

					insertStatement.Insert.Into = ((TableBuilder.TableContext)sequence).SqlTable;
					sequence.SelectQuery.From.Tables.Clear();
				}
				else if (typeof(IQueryable<>).IsSameOrParentOf(argument.Type))
				{
					// static int Insert<TSource,TTarget>(this IQueryable<TSource> source, Table<TTarget> target, Expression<Func<TSource,TTarget>> setter)

					into = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()));

					var setter = (LambdaExpression)GetArgumentByName("setter").Unwrap();

					UpdateBuilder.BuildSetter(
						builder,
						buildInfo,
						setter,
						into,
						insertStatement.Insert.Items,
						sequence);

					sequence.SelectQuery.Select.Columns.Clear();

					foreach (var item in insertStatement.Insert.Items)
						sequence.SelectQuery.Select.Columns.Add(new SqlColumn(sequence.SelectQuery, item.Expression));

					insertStatement.Insert.Into = ((TableBuilder.TableContext)into).SqlTable;
				}

				if (insertType == InsertContext.InsertType.InsertOutput || insertType == InsertContext.InsertType.InsertOutputInto)
				{
					outputExpression = GetOutputExpression(methodCall.Method.GetGenericArguments().Last());

					insertStatement.Output = new SqlOutputClause();

					var table          = new SqlTable(outputExpression.Parameters[0].Type);
					table.Name         = "INSERTED";
					table.PhysicalName = table.Name;
					table.SqlTableType = SqlTableType.SystemTable;

					var insertedContext = sequence = new TableBuilder.TableContext(builder, buildInfo, table);

					insertStatement.Output.InsertedTable = table;

					OutputHelpers.BuildOutput(
						builder,
						buildInfo,
						outputExpression,
						into,
						insertStatement.Output.OutputItems,
						insertedContext,
						null);
				}

				if (insertType == InsertContext.InsertType.InsertOutputInto)
				{
					//TODO:
					var outputTable = GetArgumentByName("outputTable");
					into = builder.BuildSequence(new BuildInfo(buildInfo, outputTable, new SelectQuery()));
				}
			}

/*
			switch (methodCall.Arguments.Count)
			{
				case 1 :
					// static int Insert<T>              (this IValueInsertable<T> source)
					// static int Insert<TSource,TTarget>(this ISelectInsertable<TSource,TTarget> source)
					{
						foreach (var item in insertStatement.Insert.Items)
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
							insertStatement.Insert.Items,
							sequence);

						insertStatement.Insert.Into = ((TableBuilder.TableContext)sequence).SqlTable;
						sequence.SelectQuery.From.Tables.Clear();

						break;
					}

				case 3 : // static int Insert<TSource,TTarget>(this IQueryable<TSource> source, Table<TTarget> target, Expression<Func<TSource,TTarget>> setter)
				case 4 : // InsertWithOutput or InsertWithOutputInto
				case 5 : // InsertWithOutputInto
					{
						into = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()));

						UpdateBuilder.BuildSetter(
							builder,
							buildInfo,
							(LambdaExpression)methodCall.Arguments[2].Unwrap(),
							into,
							insertStatement.Insert.Items,
							sequence);

						sequence.SelectQuery.Select.Columns.Clear();

						foreach (var item in insertStatement.Insert.Items)
							sequence.SelectQuery.Select.Columns.Add(new SqlColumn(sequence.SelectQuery, item.Expression));

						insertStatement.Insert.Into = ((TableBuilder.TableContext)into).SqlTable;

						break;
					}
			}

			if (into != null && methodCall.Method.Name.StartsWith("InsertWithOutput"))
			{
				insertType = InsertContext.InsertType.InsertOutput;
				insertStatement.Output = new SqlOutputClause();

				if (methodCall.Arguments.Count == 4)
				{
					outputExpression = (LambdaExpression)methodCall.Arguments[3].Unwrap();
				}
				else
				{
					var param        = Expression.Parameter(methodCall.Method.GetGenericArguments()[1]);
					outputExpression = Expression.Lambda(param, param);
				}

				var table          = new SqlTable(outputExpression.Parameters[0].Type);
				table.Name         = "INSERTED";
				table.PhysicalName = table.Name;
				table.SqlTableType = SqlTableType.SystemTable;

				var insertedContext = new TableBuilder.TableContext(builder, buildInfo, table);

				insertStatement.Output.InsertedTable = table;

				UpdateBuilder.BuildOutput(
					builder,
					buildInfo,
					outputExpression,
					into,
					insertStatement.Output.OutputItems,
					insertedContext,
					null);

			}
*/

			var insert = insertStatement.Insert;

			var q = insert.Into.Fields.Values
				.Except(insert.Items.Select(e => e.Column))
				.OfType<SqlField>()
				.Where(f => f.IsIdentity);

			foreach (var field in q)
			{
				var expr = builder.DataContext.CreateSqlProvider().GetIdentityExpression(insert.Into);

				if (expr != null)
				{
					insert.Items.Insert(0, new SqlSetExpression(field, expr));

					if (methodCall.Arguments.Count == 3)
					{
						sequence.SelectQuery.Select.Columns.Insert(0, new SqlColumn(sequence.SelectQuery, insert.Items[0].Expression));
					}
				}
			}

			insertStatement.Insert.WithIdentity = insertType == InsertContext.InsertType.InsertWithIdentity;
			sequence.Statement = insertStatement;

			return new InsertContext(buildInfo.Parent, sequence, insertType, outputExpression);
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		#endregion

		#region InsertContext

		class InsertContext : SelectContext // SequenceContextBase
		{
			public enum InsertType
			{
				Insert,
				InsertWithIdentity,
				InsertOutput,
				InsertOutputInto
			}

			public InsertContext(IBuildContext parent, IBuildContext sequence, InsertType insertType, LambdaExpression outputExpression)
				: base(parent, outputExpression, sequence)
			{
				_insertType       = insertType;
				_outputExpression = outputExpression;
			}

			readonly InsertType _insertType;
			private readonly LambdaExpression _outputExpression;

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				switch (_insertType)
				{
					case InsertType.Insert:
						QueryRunner.SetNonQueryQuery(query);
						break;
					case InsertType.InsertWithIdentity:
						QueryRunner.SetScalarQuery(query);
						break;
					case InsertType.InsertOutput:
						//TODO:
						base.BuildQuery(query, queryParameter);
						//var mapper = Builder.BuildMapper<T>(_outputExpression.Body.Unwrap());
						//QueryRunner.SetRunQuery(query, mapper);
						break;
					case InsertType.InsertOutputInto:
						QueryRunner.SetNonQueryQuery(query);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			public override Expression BuildExpression(Expression expression, int level, bool enforceServerSide)
			{
				if (_insertType == InsertType.InsertOutput)
					return base.BuildExpression(expression, level, enforceServerSide);
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
			{
				if (_insertType == InsertType.InsertOutput)
					return base.ConvertToSql(expression, level, flags);
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
			{
				if (_insertType == InsertType.InsertOutput)
					return base.ConvertToIndex(expression, level, flags);
				throw new NotImplementedException();
			}

			public override IsExpressionResult IsExpression(Expression expression, int level, RequestFor requestFlag)
			{
				if (_insertType == InsertType.InsertOutput)
					return base.IsExpression(expression, level, requestFlag);
				throw new NotImplementedException();
			}

			public override IBuildContext GetContext(Expression expression, int level, BuildInfo buildInfo)
			{
				if (_insertType == InsertType.InsertOutput)
					return base.GetContext(expression, level, buildInfo);
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
				SqlInsertStatement insertStatement;

				// static IValueInsertable<T> Into<T>(this IDataContext dataContext, Table<T> target)
				//
				if (source.NodeType == ExpressionType.Constant && ((ConstantExpression)source).Value == null)
				{
					sequence = builder.BuildSequence(new BuildInfo((IBuildContext)null, into, new SelectQuery()));

					if (sequence.SelectQuery.Select.IsDistinct)
						sequence = new SubQueryContext(sequence);

					insertStatement = new SqlInsertStatement(sequence.SelectQuery);
					insertStatement.Insert.Into = ((TableBuilder.TableContext)sequence).SqlTable;
					insertStatement.SelectQuery.From.Tables.Clear();
				}
				// static ISelectInsertable<TSource,TTarget> Into<TSource,TTarget>(this IQueryable<TSource> source, Table<TTarget> target)
				//
				else
				{
					sequence = builder.BuildSequence(new BuildInfo(buildInfo, source));

					if (sequence.SelectQuery.Select.IsDistinct)
						sequence = new SubQueryContext(sequence);

					insertStatement = new SqlInsertStatement(sequence.SelectQuery);

					var tbl = builder.BuildSequence(new BuildInfo((IBuildContext)null, into, new SelectQuery()));
					insertStatement.Insert.Into = ((TableBuilder.TableContext)tbl).SqlTable;
				}

				sequence.Statement = insertStatement;
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

				if (!(sequence.Statement is SqlInsertStatement insertStatement))
				{
					insertStatement    = new SqlInsertStatement(sequence.SelectQuery);
					sequence.Statement = insertStatement;
				}

				if (insertStatement.Insert.Into == null)
				{
					insertStatement.Insert.Into = (SqlTable)sequence.SelectQuery.From.Tables[0].Source;
					insertStatement.SelectQuery.From.Tables.Clear();
				}

				if (update.NodeType == ExpressionType.Lambda)
					UpdateBuilder.ParseSet(
						builder,
						buildInfo,
						extract,
						(LambdaExpression)update,
						sequence,
						insertStatement.Insert.Into,
						insertStatement.Insert.Items);
				else
					UpdateBuilder.ParseSet(
						builder,
						buildInfo,
						extract,
						update,
						sequence,
						insertStatement.Insert.Items);

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
