using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Async;
	using Common;
	using LinqToDB.Expressions;
	using SqlQuery;
	using Extensions;
	using Reflection;

	sealed class FirstSingleBuilder : MethodCallBuilder
	{
		static readonly string[] MethodNames      = { "First"     , "FirstOrDefault"     , "Single"     , "SingleOrDefault"      };
		static readonly string[] MethodNamesAsync = { "FirstAsync", "FirstOrDefaultAsync", "SingleAsync", "SingleOrDefaultAsync" };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return IsApplicable(methodCall);
		}

		private static bool IsApplicable(MethodCallExpression methodCall)
		{
			return
				methodCall.IsQueryable(MethodNames)           && methodCall.Arguments.Count == 1 ||
				methodCall.IsAsyncExtension(MethodNamesAsync) && methodCall.Arguments.Count == 2;
		}

		public enum MethodKind
		{
			First,
			FirstOrDefault,
			Single,
			SingleOrDefault,
		}

		static MethodKind GetMethodKind(string methodName)
		{
			return methodName switch
			{
				"First"                => MethodKind.First,
				"FirstAsync"           => MethodKind.First,
				"FirstOrDefault"       => MethodKind.FirstOrDefault,
				"FirstOrDefaultAsync"  => MethodKind.FirstOrDefault,
				"Single"               => MethodKind.Single,
				"SingleAsync"          => MethodKind.Single,
				"SingleOrDefault"      => MethodKind.SingleOrDefault,
				"SingleOrDefaultAsync" => MethodKind.SingleOrDefault,
				_ => throw new ArgumentOutOfRangeException(nameof(methodName), methodName, "Not supported method.")
			};
		}

		protected override IBuildContext? BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
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

			var sequence = builder.TryBuildSequence(new BuildInfo(buildInfo, argument));
			if (sequence == null)
				return null;

			sequence = new SubQueryContext(sequence);

			if (fakeJoin != null)
			{
				buildInfo.Parent!.SelectQuery.From.Tables[0].Joins.Remove(fakeJoin);
			}

			var take = 0;

			var cardinality = SourceCardinality.One;
			var methodKind  = GetMethodKind(methodCall.Method.Name);

			switch (methodKind)
			{
				case MethodKind.First          :
				{
					take        = 1;
					break;
				}
				case MethodKind.FirstOrDefault :
				{
					cardinality |= SourceCardinality.Zero;
					take        = 1;
					break;
				}
				case MethodKind.Single          :
				case MethodKind.SingleOrDefault :
				{
					if (methodKind == MethodKind.SingleOrDefault)
						cardinality |= SourceCardinality.Zero;

					if (!buildInfo.IsSubQuery)
						if (buildInfo.SelectQuery.Select.TakeValue == null ||
						    buildInfo.SelectQuery.Select.TakeValue is SqlValue takeValue && (int)takeValue.Value! >= 2)
							take = 2;

					break;
				}
			}

			if (take != 0)
			{
				var takeExpression = new SqlValue(take);
				builder.BuildTake(sequence, takeExpression, null);
			}

			var canBeWeak = false;

			if (methodCall.Method.Name.Contains("OrDefault"))
			{
				sequence = new DefaultIfEmptyBuilder.DefaultIfEmptyContext(buildInfo.Parent, sequence, null, allowNullField: true);
				canBeWeak = true;
			}

			return new FirstSingleContext(buildInfo.Parent, sequence, methodKind, buildInfo.IsSubQuery, buildInfo.IsAssociation, canBeWeak, cardinality, buildInfo.IsTest);
		}

		public sealed class FirstSingleContext : SequenceContextBase
		{
			public FirstSingleContext(IBuildContext? parent, IBuildContext sequence, MethodKind methodKind,
				bool isSubQuery, bool isAssociation, bool canBeWeak, SourceCardinality cardinality, bool isTest)
				: base(parent, sequence, null)
			{
				_methodKind   = methodKind;
				IsSubQuery    = isSubQuery;
				IsAssociation = isAssociation;
				CanBeWeak     = canBeWeak;
				Cardinality   = cardinality;
				IsTest        = isTest;
			}

			readonly MethodKind _methodKind;

			public bool              IsSubQuery    { get; }
			public bool              IsAssociation { get; }
			public bool              CanBeWeak     { get; }
			public bool              IsTest        { get; }
			public SourceCardinality Cardinality   { get; set; }

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<T>(SelectQuery, expr);

				QueryRunner.SetRunQuery(query, mapper);

				switch (_methodKind)
				{
					case MethodKind.First           : GetFirstElement          (query); break;
					case MethodKind.FirstOrDefault  : GetFirstOrDefaultElement (query); break;
					case MethodKind.Single          : GetSingleElement         (query); break;
					case MethodKind.SingleOrDefault : GetSingleOrDefaultElement(query); break;
				}
			}

			static void GetFirstElement<T>(Query<T> query)
			{
				query.GetElement = (db, expr, ps, preambles) =>
					query.GetResultEnumerable(db, expr, ps, preambles).First();

				query.GetElementAsync = query.GetElementAsync = async (db, expr, ps, preambles, token) =>
					await query.GetResultEnumerable(db, expr, ps, preambles)
						.FirstAsync(token).ConfigureAwait(Configuration.ContinueOnCapturedContext);
			}

			static void GetFirstOrDefaultElement<T>(Query<T> query)
			{
				query.GetElement = (db, expr, ps, preambles) =>
					query.GetResultEnumerable(db, expr, ps, preambles).FirstOrDefault();

				query.GetElementAsync = query.GetElementAsync = async (db, expr, ps, preambles, token) =>
					await query.GetResultEnumerable(db, expr, ps, preambles)
						.FirstOrDefaultAsync(token).ConfigureAwait(Configuration.ContinueOnCapturedContext);
			}

			static void GetSingleElement<T>(Query<T> query)
			{
				query.GetElement = (db, expr, ps, preambles) =>
					query.GetResultEnumerable(db, expr, ps, preambles).Single();

				query.GetElementAsync = async (db, expr, ps, preambles, token) =>
					await query.GetResultEnumerable(db, expr, ps, preambles)
						.SingleAsync(token).ConfigureAwait(Configuration.ContinueOnCapturedContext);
			}

			static void GetSingleOrDefaultElement<T>(Query<T> query)
			{
				query.GetElement = (db, expr, ps, preambles) =>
					query.GetResultEnumerable(db, expr, ps, preambles).SingleOrDefault();

				query.GetElementAsync = async (db, expr, ps, preambles, token) =>
					await query.GetResultEnumerable(db, expr, ps, preambles)
						.SingleOrDefaultAsync(token).ConfigureAwait(Configuration.ContinueOnCapturedContext);
			}

			bool _isJoinCreated;

			void CreateJoin()
			{
				// sequence created in test mode and there can be no tables.
				//
				if (IsTest || Parent!.SelectQuery.From.Tables.Count == 0)
					return;

				if (!_isJoinCreated)
				{
					_isJoinCreated = true;

					var join = CanBeWeak ? SelectQuery.OuterApply() : SelectQuery.CrossApply();
					join.JoinedTable.IsWeak      = Cardinality.HasFlag(SourceCardinality.Zero);
					join.JoinedTable.Cardinality = Cardinality;

					Parent!.SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);
				}
			}

			bool IsSupportedByProvider(Expression expression)
			{
				if (Builder.DataContext.SqlProviderFlags.IsApplyJoinSupported)
					return true;

				var sqlProjected = Builder.ConvertToSqlExpr(this, expression, ProjectFlags.SQL | ProjectFlags.Test);

				var placeholders = ExpressionBuilder.CollectDistinctPlaceholders(sqlProjected);

				if (!Builder.DataContext.SqlProviderFlags.IsSubQueryColumnSupported && placeholders.Count > 1)
				{
					if (!Builder.DataContext.SqlProviderFlags.IsWindowFunctionsSupported)
						return false;
				}

				if (!Builder.DataContext.SqlProviderFlags.IsWindowFunctionsSupported)
					return false;

				var parentQuery = Parent?.SelectQuery;
				if (parentQuery != null)
				{
					var cloningContext = new CloningContext();

					var clonedQuery = cloningContext.CloneElement(SelectQuery);

					var parentSources = new HashSet<ISqlTableSource>();

					foreach (var source in QueryHelper.EnumerateAccessibleSources(parentQuery))
					{
						if (source != SelectQuery)
						{
							var corrected = cloningContext.CorrectElement(source);
							parentSources.Add(corrected);
						}
					}
					
					QueryHelper.OptimizeSelectQuery(clonedQuery, clonedQuery, Builder.DataContext.SqlProviderFlags, Builder.DataOptions);

					var toIgnore = new HashSet<IQueryElement>() { clonedQuery.Where };

					if (QueryHelper.IsDependsOnSources(clonedQuery, parentSources, toIgnore))
						return false;
				}


				return true;
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if ((flags.IsAssociationRoot() || flags.IsRoot()) && SequenceHelper.IsSameContext(path, this))
				{
					return path;
				}

				var projected = base.MakeExpression(path, flags);

				if (!flags.HasFlag(ProjectFlags.Test))
				{
					if (IsSubQuery)
					{
						// Bad thing here. We expect that SelectQueryOptimizer will transfer OUTER APPLY to ROW_NUMBER query. We have to predict it here
						if (!IsSupportedByProvider(path))
						{
							if (flags.HasFlag(ProjectFlags.Expression))
							{
								// Do not generate Take. Provider do not support Outer queries. Result will be filtered on the client side.
								//
								var sequenceExpression = GetEagerLoadingExpression(false);

								var resultType = typeof(IEnumerable<>).MakeGenericType(path.Type);
								var result     = (Expression)new SqlEagerLoadExpression(sequenceExpression);

								var methodInfo = _methodKind switch
								{
									MethodKind.First => Methods.Enumerable.First,
									MethodKind.FirstOrDefault => Methods.Enumerable.FirstOrDefault,
									MethodKind.Single => Methods.Enumerable.Single,
									MethodKind.SingleOrDefault => Methods.Enumerable.SingleOrDefault,
									_ => throw new ArgumentOutOfRangeException(nameof(_methodKind), _methodKind,
										"Invalid method kind.")
								};

								methodInfo = methodInfo.MakeGenericMethod(path.Type);

								result = Expression.Call(methodInfo, result);

								return result;
							}
						}

						CreateJoin();

						return projected;
					}
				}

				return projected;
			}

			Expression GetEagerLoadingExpression(bool withTake)
			{
				var sequenceExpression = Builder.GetSequenceExpression(this);
				sequenceExpression = ((MethodCallExpression)sequenceExpression).Arguments[0];

				if (!withTake)
				{
					return sequenceExpression;
				}

				var method = typeof(IQueryable<>).IsSameOrParentOf(sequenceExpression.Type)
					? Methods.Queryable.Take
					: Methods.Enumerable.Take;

				method = method.MakeGenericMethod(ExpressionBuilder.GetEnumerableElementType(sequenceExpression.Type));

				var result = Expression.Call(method, sequenceExpression, Expression.Constant(1));

				return result;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new FirstSingleContext(null, context.CloneContext(Sequence),
					_methodKind, IsSubQuery, IsAssociation, CanBeWeak, Cardinality, false)
				{
					_isJoinCreated = _isJoinCreated
				};
			}
		}
	}
}
