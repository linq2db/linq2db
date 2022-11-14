using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Async;
	using Common;
	using Extensions;
	using LinqToDB.Expressions;
	using SqlQuery;

	class FirstSingleBuilder : MethodCallBuilder
	{
		public  static readonly string[] MethodNames      = { "First"     , "FirstOrDefault"     , "Single"     , "SingleOrDefault"      };
		private static readonly string[] MethodNamesAsync = { "FirstAsync", "FirstOrDefaultAsync", "SingleAsync", "SingleOrDefaultAsync" };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return
				methodCall.IsQueryable     (MethodNames     ) && methodCall.Arguments.Count == 1 ||
				methodCall.IsAsyncExtension(MethodNamesAsync) && methodCall.Arguments.Count == 2;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var argument = methodCall.Arguments[0];

			SqlJoinedTable? fakeJoin = null;
			if (buildInfo.Parent != null)
			{
				argument = SequenceHelper.MoveAllToScopedContext(argument, buildInfo.Parent);

				if (!buildInfo.IsTest)
				{
					if (buildInfo.Parent.SelectQuery.From.Tables.Count > 0)
					{
						// introducing fake join for correct nesting update

						var join = buildInfo.SelectQuery.OuterApply();
						join.JoinedTable.IsWeak = true;

						fakeJoin = join.JoinedTable;

						buildInfo.Parent.SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);
					}
				}
			}

			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, argument));

			if (fakeJoin != null)
			{
				buildInfo.Parent!.SelectQuery.From.Tables[0].Joins.Remove(fakeJoin);
			}

			var take     = 0;

			var forceOuter = buildInfo.Parent is DefaultIfEmptyBuilder.DefaultIfEmptyContext; 

				switch (methodCall.Method.Name)
				{
					case "First"                :
					case "FirstOrDefault"       :
					case "FirstAsync"           :
					case "FirstOrDefaultAsync"  :
						take = 1;
						break;

					case "Single"               :
					case "SingleOrDefault"      :
					case "SingleAsync"          :
					case "SingleOrDefaultAsync" :
						if (!buildInfo.IsSubQuery)
							if (buildInfo.SelectQuery.Select.TakeValue == null || buildInfo.SelectQuery.Select.TakeValue is SqlValue takeValue && (int)takeValue.Value! >= 2)
								take = 2;

						break;
				}

			if (take != 0)
			{
				var takeExpression = new SqlValue(take);
				builder.BuildTake(sequence, takeExpression, null);
			}

			var isOuter = false;

			if (forceOuter || methodCall.Method.Name.Contains("OrDefault"))
			{
				sequence = new DefaultIfEmptyBuilder.DefaultIfEmptyContext(buildInfo.Parent, sequence, null);
				isOuter  = true;
			}

			return new FirstSingleContext(buildInfo.Parent, sequence, methodCall, buildInfo.IsSubQuery, buildInfo.IsAssociation, isOuter);
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		public class FirstSingleContext : SequenceContextBase
		{
			public FirstSingleContext(IBuildContext? parent, IBuildContext sequence, MethodCallExpression methodCall, bool isSubQuery, bool isAssociation, bool isOuter)
				: base(parent, sequence, null)
			{
				_methodCall   = methodCall;
				IsSubQuery    = isSubQuery;
				IsAssociation = isAssociation;
				IsOuter       = isOuter;
			}

			readonly MethodCallExpression _methodCall;

			public bool IsSubQuery    { get; }
			public bool IsAssociation { get; }
			public bool IsOuter       { get; }

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				Sequence.BuildQuery(query, queryParameter);

				switch (_methodCall.Method.Name.Replace("Async", ""))
				{
					case "First"           : GetFirstElement          (query); break;
					case "FirstOrDefault"  : GetFirstOrDefaultElement (query); break;
					case "Single"          : GetSingleElement         (query); break;
					case "SingleOrDefault" : GetSingleOrDefaultElement(query); break;
				}
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<T>(expr);

				QueryRunner.SetRunQuery(query, mapper);

				switch (_methodCall.Method.Name.Replace("Async", ""))
				{
					case "First"           : GetFirstElement          (query); break;
					case "FirstOrDefault"  : GetFirstOrDefaultElement (query); break;
					case "Single"          : GetSingleElement         (query); break;
					case "SingleOrDefault" : GetSingleOrDefaultElement(query); break;
				}
			}

			static void GetFirstElement<T>(Query<T> query)
			{
				query.GetElement      = (db, expr, ps, preambles) => query.GetResultEnumerable(db, expr, ps, preambles).First();

				query.GetElementAsync = query.GetElementAsync = async (db, expr, ps, preambles, token) =>
					await query.GetResultEnumerable(db, expr, ps, preambles)
						.FirstAsync(token).ConfigureAwait(Configuration.ContinueOnCapturedContext);
			}

			static void GetFirstOrDefaultElement<T>(Query<T> query)
			{
				query.GetElement      = (db, expr, ps, preambles) => query.GetResultEnumerable(db, expr, ps, preambles).FirstOrDefault();

				query.GetElementAsync = query.GetElementAsync = async (db, expr, ps, preambles, token) =>
					await query.GetResultEnumerable(db, expr, ps, preambles)
						.FirstOrDefaultAsync(token).ConfigureAwait(Configuration.ContinueOnCapturedContext);
			}

			static void GetSingleElement<T>(Query<T> query)
			{
				query.GetElement      = (db, expr, ps, preambles) => query.GetResultEnumerable(db, expr, ps, preambles).Single();

				query.GetElementAsync = async (db, expr, ps, preambles, token) =>
					await query.GetResultEnumerable(db, expr, ps, preambles)
						.SingleAsync(token).ConfigureAwait(Configuration.ContinueOnCapturedContext);
			}

			static void GetSingleOrDefaultElement<T>(Query<T> query)
			{
				query.GetElement      = (db, expr, ps, preambles) => query.GetResultEnumerable(db, expr, ps, preambles).SingleOrDefault();

				query.GetElementAsync = async (db, expr, ps, preambles, token) =>
					await query.GetResultEnumerable(db, expr, ps, preambles)
						.SingleOrDefaultAsync(token).ConfigureAwait(Configuration.ContinueOnCapturedContext);
			}

			bool _isJoinCreated;

			void CreateJoin()
			{
				// sequence created in test mode and there can be no tables.
				//
				if (Parent!.SelectQuery.From.Tables.Count == 0)
					return;

				if (!_isJoinCreated)
				{
					_isJoinCreated = true;

					var join = IsOuter ? SelectQuery.OuterApply() : SelectQuery.CrossApply();
					join.JoinedTable.IsWeak = true;

					Parent!.SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);
				}
			}

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				throw new NotImplementedException();
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if ((flags.HasFlag(ProjectFlags.AssociationRoot) || flags.HasFlag(ProjectFlags.Root)) && SequenceHelper.IsSameContext(path, this))
				{
					return path;
				}

				var projected = base.MakeExpression(path, flags);

				if (!flags.HasFlag(ProjectFlags.Test))
				{
					if (IsSubQuery)
					{
						// Bad thing here. We expect that SelectQueryOptimizer will transfer OUTER APPLY to ROW_NUMBER query. We have to predict it here
						if (!IsAssociation && !Builder.DataContext.SqlProviderFlags.IsApplyJoinSupported && !Builder.DataContext.SqlProviderFlags.IsWindowFunctionsSupported)
						{
							var sqlProjected = Builder.ConvertToSqlExpr(this, projected, ProjectFlags.Test);

							var placeholders = ExpressionBuilder.CollectDistinctPlaceholders(sqlProjected);

							if (placeholders.Count > 1 || !Builder.DataContext.SqlProviderFlags.IsSubQueryColumnSupported)
							{
								if (flags.HasFlag(ProjectFlags.Expression))
								{
									throw new NotImplementedException("Eager loading for FirstSingleBuilder is not implemented yet.");
									return new SqlEagerLoadExpression((ContextRefExpression)path, path, Builder.GetSequenceExpression(this));
								}
							}
						}

						CreateJoin();

						return projected;
					}
				}

				return projected;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new FirstSingleContext(null, context.CloneContext(Sequence), context.CloneExpression(_methodCall), IsSubQuery, IsAssociation, IsOuter);
			}

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				return Sequence.ConvertToSql(expression, level + 1, flags);
			}

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				return Sequence.ConvertToIndex(expression, level, flags);
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				return Sequence.IsExpression(expression, level, requestFlag);
			}

			public override IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				return null;
			}
		}
	}
}
