using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	internal partial class MergeBuilder
	{
		sealed class MergeContext : SequenceContextBase
		{
			public MergeContext(SqlMergeStatement merge, IBuildContext target)
				: base(null, target, null)
			{
				Merge = merge;
			}

			public MergeContext(TranslationModifier translationModifier, SqlMergeStatement merge, IBuildContext target, TableLikeQueryContext source)
				: base(translationModifier, null, [target, source], null)
			{
				Merge        = merge;
				Merge.Source = source.Source;
			}

			public SqlMergeStatement Merge { get; }

			public ITableContext         TargetContext => (ITableContext)Sequence;
			public TableLikeQueryContext SourceContext => (TableLikeQueryContext)Sequences[1];

			public MergeKind    Kind             { get; set; }
			public Expression?  OutputExpression { get; set; }
			public IBuildContext? OutputContext  { get; set; }

			public override SqlStatement GetResultStatement()
			{
				return Merge;
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				switch(Kind)
				{
					case MergeKind.Merge:
					case MergeKind.MergeWithOutputInto:
					case MergeKind.MergeWithOutputIntoSource:
					{
						QueryRunner.SetNonQueryQuery(query);
						break;
					}
					case MergeKind.MergeWithOutput:
					case MergeKind.MergeWithOutputSource:
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
				if (SequenceHelper.IsSameContext(path, this) && flags.IsExpression() &&
					(Kind == MergeKind.MergeWithOutput || Kind == MergeKind.MergeWithOutputSource))
				{
					if (OutputExpression == null || OutputContext == null)
						throw new InvalidOperationException();

					var selectContext = new SelectContext(Parent, OutputExpression, OutputContext, false);
					var outputRef     = new ContextRefExpression(OutputExpression.Type, selectContext);

					var outputExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();

					var sqlExpr = Builder.BuildSqlExpression(selectContext, outputRef);
					sqlExpr = SequenceHelper.CorrectSelectQuery(sqlExpr, OutputContext.SelectQuery);

					if (sqlExpr is SqlPlaceholderExpression)
						outputExpressions.Add(new UpdateBuilder.SetExpressionEnvelope(sqlExpr, sqlExpr, false));
					else
						UpdateBuilder.ParseSetter(Builder, outputRef, sqlExpr, outputExpressions);

					var setItems = new List<SqlSetExpression>();
					UpdateBuilder.InitializeSetExpressions(Builder, selectContext, selectContext, outputExpressions, setItems, false);

					Merge.Output!.OutputColumns = setItems.Select(c => c.Expression!).ToList();

					return sqlExpr;
				}
				return path;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				throw new NotImplementedException();
			}

			public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
			{
				return null;
			}
		}
	}
}
