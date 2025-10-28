using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall("Select")]
	sealed class SelectBuilder : MethodCallBuilder
	{
		#region SelectBuilder

		public static bool CanBuildMethod(MethodCallExpression call)
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

		sealed class CounterContext : BuildContextBase
		{
			readonly IBuildContext _sequence;

			SqlPlaceholderExpression? _rowNumberPlaceholder;

			public CounterContext(IBuildContext sequence) : base(sequence.TranslationModifier, sequence.Builder, typeof(int), sequence.SelectQuery)
			{
				_sequence = sequence;
			}

			public override MappingSchema MappingSchema => Builder.MappingSchema;

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

						var orderSequence = SequenceHelper.GetOrderSequence(_sequence);

						if (orderSequence == null)
						{
							return new SqlErrorExpression(path, ErrorHelper.Error_OrderByRequiredForIndexing, path.Type);
						}

						var orderQuery = orderSequence.SelectQuery;

						var orderBy  = orderQuery.OrderBy.Items.Select(o => new SqlWindowOrderItem(o.Expression, o.IsDescending, Sql.NullsPosition.None));
						var longType = MappingSchema.GetDbDataType(typeof(long));
						var rn       = new SqlExtendedFunction(longType, "ROW_NUMBER", [], [], orderBy : orderBy);
						var sql      = new SqlBinaryExpression(longType, rn, "-", new SqlValue(longType, 1));

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
