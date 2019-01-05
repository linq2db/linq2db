using LinqToDB.Expressions;
using LinqToDB.SqlQuery;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
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

			public MergeContext(SqlMergeStatement merge, IBuildContext target, IBuildContext source)
				: base(null, new[] { target, source }, null)
			{
				Statement = merge;
			}

			public SqlMergeStatement Merge => (SqlMergeStatement)Statement;

			public IBuildContext           TargetContext => Sequence;
			public MergeSourceQueryContext SourceContext => (MergeSourceQueryContext)Sequences[1];

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				QueryRunner.SetNonQueryQuery(query);
			}

			public override Expression BuildExpression(Expression expression, int level, bool enforceServerSide)
			{
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
			{
				switch (flags)
				{
					case ConvertFlags.Field:
					//case ConvertFlags.Key:
					//case ConvertFlags.All:
						{
							var root = expression.GetRootObject(Builder.MappingSchema);

							if (root.NodeType == ExpressionType.Parameter)
							{
								if (_sourceParameters.Contains(root))
									return SourceContext.ConvertToSql(expression, level, flags);

								if (_targetParameters.Contains(root))
									return TargetContext.ConvertToSql(expression, level, flags);

								return TargetContext.ConvertToSql(expression, level, flags);
							}

							break;
						}
				}

				throw new LinqException("'{0}' cannot be converted to SQL.", expression);
			}

			public override IBuildContext GetContext(Expression expression, int level, BuildInfo buildInfo)
			{
				throw new NotImplementedException();
			}

			public override IsExpressionResult IsExpression(Expression expression, int level, RequestFor requestFlag)
			{
				throw new NotImplementedException();
			}
		}
	}
}
