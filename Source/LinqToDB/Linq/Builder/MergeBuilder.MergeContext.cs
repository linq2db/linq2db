using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;
	using LinqToDB.Expressions;

	internal partial class MergeBuilder
	{
        sealed class MergeContext : SequenceContextBase
		{
			public MergeContext(SqlMergeStatement merge, IBuildContext target)
				: base(null, target, null)
			{
				Statement = merge;
			}

			public MergeContext(SqlMergeStatement merge, IBuildContext target, TableLikeQueryContext source)
				: base(null, new[] { target, source }, null)
			{
				Statement    = merge;
				merge.Source = source.Source;
			}

			public SqlMergeStatement Merge => (SqlMergeStatement)Statement!;

			public ITableContext         TargetContext => (ITableContext)Sequence;
			public TableLikeQueryContext SourceContext => (TableLikeQueryContext)Sequences[1];

			public MergeKind    Kind             { get; set; }
			public Expression?  OutputExpression { get; set; }
			public IBuildContext? OutputContext  { get; set; }

			public override SqlStatement GetResultStatement()
			{
				return Merge;
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				throw new NotImplementedException();
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				switch(Kind)
				{
					case MergeKind.Merge:
					case MergeKind.MergeWithOutputInto:
					{
						QueryRunner.SetNonQueryQuery(query);
						break;
					}	
					case MergeKind.MergeWithOutput:
					{
						var mapper = Builder.BuildMapper<T>(SelectQuery, expr);
						QueryRunner.SetRunQuery(query, mapper);
						break;
					}	
					default:
						throw new ArgumentOutOfRangeException(Kind.ToString());
				}
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this) && flags.IsExpression() && Kind == MergeKind.MergeWithOutput)
				{
					if (OutputExpression == null || OutputContext == null)
						throw new InvalidOperationException();

					var selectContext = new SelectContext(Parent, OutputExpression, OutputContext, false);
					var outputRef     = new ContextRefExpression(path.Type, selectContext);

					var outputExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();

					var sqlExpr = Builder.ConvertToSqlExpr(selectContext, outputRef);
					if (sqlExpr is SqlPlaceholderExpression)
						outputExpressions.Add(new UpdateBuilder.SetExpressionEnvelope(sqlExpr, sqlExpr));
					else
						UpdateBuilder.ParseSetter(Builder, outputRef, sqlExpr, outputExpressions);

					var setItems = new List<SqlSetExpression>();
					UpdateBuilder.InitializeSetExpressions(Builder, selectContext, selectContext, outputExpressions, setItems, false);

					Merge.Output!.OutputColumns = setItems.Select(c => c.Expression!).ToList();

					return sqlExpr;
				}
				return path;
			}

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override IBuildContext Clone(CloningContext context)
			{
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				return null;
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				throw new NotImplementedException();
			}

			
		}
	}
}
