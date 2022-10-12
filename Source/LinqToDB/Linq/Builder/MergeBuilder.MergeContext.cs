using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	internal partial class MergeBuilder
	{
		private class MergeContext : SequenceContextBase
		{
			private readonly ISet<Expression> _sourceParameters = new HashSet<Expression>();
			private readonly ISet<Expression> _targetParameters = new HashSet<Expression>();

			public void AddSourceParameter(Expression param)
			{
				_sourceParameters.Add(param);
			}

			public void AddTargetParameter(Expression param)
			{
				_targetParameters.Add(param);
			}

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

			public IBuildContext         TargetContext => Sequence;
			public TableLikeQueryContext SourceContext => (TableLikeQueryContext)Sequences[1];

			public override SqlStatement GetResultStatement()
			{
				return Merge;
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				QueryRunner.SetNonQueryQuery(query);
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				QueryRunner.SetNonQueryQuery(query);
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
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
