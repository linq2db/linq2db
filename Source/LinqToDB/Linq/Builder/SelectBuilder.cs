using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	[BuildsMethodCall("Select")]
	sealed class SelectBuilder : MethodCallBuilder
	{
		#region SelectBuilder

		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
		{
			if (!call.IsQueryable())
				return false;
			
			var lambda = (LambdaExpression)call.Arguments[1].Unwrap();
			return lambda.Parameters.Count is 1 or 2;
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var selector    = (LambdaExpression)methodCall.Arguments[1].Unwrap();
			var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			if (buildResult.BuildContext == null)
				return buildResult;

			var sequence = buildResult.BuildContext;

			// finalizing context
			_ = builder.BuildExtractExpression(sequence, new ContextRefExpression(sequence.ElementType, sequence));

			sequence.SetAlias(selector.Parameters[0].Name);
			sequence = new SubQueryContext(sequence) { IsSelectWrapper = true };
			sequence.SetAlias(selector.Parameters[0].Name);

			var body = selector.Parameters.Count == 1
				? SequenceHelper.PrepareBody(selector, sequence)
				: SequenceHelper.PrepareBody(selector, sequence, new CounterContext(buildResult.BuildContext));

			var context       = new SelectContext (buildInfo.Parent, body, sequence, buildInfo.IsSubQuery);
			var resultContext = (IBuildContext) context;

#if DEBUG
			context.Debug_MethodCall = methodCall;
#endif
			return BuildSequenceResult.FromContext(resultContext);
		}

		#endregion

		class CounterContext : BuildContextBase
		{
			readonly IBuildContext _sequence;

			SqlPlaceholderExpression? _rowNumberPlaceholder;

			public CounterContext(IBuildContext sequence) : base(sequence.TranslationModifier, sequence.Builder, typeof(int), sequence.SelectQuery)
			{
				_sequence = sequence;
			}

			public override MappingSchema MappingSchema => Builder.MappingSchema;

			static IBuildContext GetOrderSequence(IBuildContext context)
			{
				var prevSequence = context;
				while (true)
				{
					if (prevSequence.SelectQuery.Select.HasModifier)
					{
						break;
					}

					if (!prevSequence.SelectQuery.OrderBy.IsEmpty)
						break;

					if (prevSequence is SubQueryContext { IsSelectWrapper: true } subQuery)
					{
						prevSequence = subQuery.SubQuery;
					}
					else if (prevSequence is SelectContext { InnerContext: not null } selectContext)
					{
						prevSequence = selectContext.InnerContext;
					}
					else
						break;
				}

				return prevSequence;
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this))
				{
					if (flags.IsSqlOrExpression())
					{
						if (_rowNumberPlaceholder != null)
							return _rowNumberPlaceholder;

						if (!Builder.DataContext.SqlProviderFlags.IsWindowFunctionsSupported)
						{
							if (flags.IsExpression())
							{
								return ExpressionBuilder.RowCounterParam;
							}

							return new SqlErrorExpression(path, ErrorHelper.Error_RowNumber, path.Type);
						}

						var orderSequence = GetOrderSequence(_sequence);

						var orderQuery = orderSequence.SelectQuery;

						if (orderQuery.OrderBy.IsEmpty)
						{
							return new SqlErrorExpression(path, ErrorHelper.Error_OrderByRequiredForIndexing, path.Type);
						}

						var orderBy = string.Join(", ",
							orderQuery.OrderBy.Items.Select(static (oi, i) => oi.IsDescending ? FormattableString.Invariant($"{{{i}}} DESC") : FormattableString.Invariant($"{{{i}}}")));

						var parameters = orderQuery.OrderBy.Items.Select(static oi => oi.Expression).ToArray();

						var rn = new SqlExpression(typeof(long), $"ROW_NUMBER() OVER (ORDER BY {orderBy})", Precedence.Primary, SqlFlags.IsWindowFunction, ParametersNullabilityType.NotNullable, parameters);
						var intType = MappingSchema.GetDbDataType(typeof(int));
						var sql = new SqlBinaryExpression(intType, rn, "-", new SqlValue(intType, 1));

						_rowNumberPlaceholder = ExpressionBuilder.CreatePlaceholder(_sequence, sql, path);
						return _rowNumberPlaceholder;
					}
				}

				return path;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new CounterContext(context.CloneContext(_sequence)) { _rowNumberPlaceholder = context.CloneExpression(_rowNumberPlaceholder) };
			}

			public override SqlStatement GetResultStatement()
			{
				return new SqlSelectStatement(SelectQuery);
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<T>(SelectQuery, expr);

				QueryRunner.SetRunQuery(query, mapper);
			}
		}
	}
}
