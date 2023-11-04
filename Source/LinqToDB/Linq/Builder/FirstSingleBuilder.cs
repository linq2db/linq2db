using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Async;
	using Common;
	using LinqToDB.Expressions;
	using SqlQuery;

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
				methodCall.IsQueryable(MethodNames)           && methodCall.Arguments.Count <= 2 ||
				methodCall.IsAsyncExtension(MethodNamesAsync) && methodCall.Arguments.Count <= 3;
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
			var argumentCount = methodCall.Arguments.Count;

			if (methodCall.IsAsyncExtension(MethodNamesAsync))
				--argumentCount;

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

			if (argumentCount > 1)
			{
				var filterLambda = methodCall.Arguments[1].UnwrapLambda();
				sequence = builder.BuildWhere(buildInfo.Parent, sequence, filterLambda, false, false, buildInfo.IsTest,
					isAggregationTest : buildInfo.AggregationTest);

				if (sequence == null)
					return null;
			}

			if (fakeJoin != null)
			{
				buildInfo.Parent!.SelectQuery.From.Tables[0].Joins.Remove(fakeJoin);
			}

			if (buildInfo.IsSubQuery)
			{
				if (!SequenceHelper.IsSupportedSubqueryForModifier(sequence))
					return null;
			}

			sequence = new SubQueryContext(sequence);

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
			bool _asSubquery;

			void CreateJoin()
			{
				// sequence created in test mode and there can be no tables.
				//
				if (IsTest)
					return;

				if (_isJoinCreated  || _asSubquery)
					return;

				// process as subquery
				if (Parent!.SelectQuery.From.Tables.Count == 0)
				{
					_asSubquery = true;
					return;
				}

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
				if (_methodKind == MethodKind.Single || _methodKind == MethodKind.SingleOrDefault)
					return true;

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

				return true;
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if ((flags.IsAssociationRoot() || flags.IsRoot()) && SequenceHelper.IsSameContext(path, this))
				{
					return path;
				}

				if (!flags.IsTest() && IsSubQuery)
				{
					CreateJoin();
				}

				var projected = base.MakeExpression(path, flags);

				if (flags.IsTable())
					return projected;

				if (_asSubquery)
				{
					if (Parent == null)
						return path;

					projected = Builder.BuildSqlExpression(this, projected, ProjectFlags.SQL,
						buildFlags : ExpressionBuilder.BuildFlags.ForceAssignments);

					if (projected is SqlPlaceholderExpression placeholder)
					{
						var column = Builder.ToColumns(this, placeholder);
						if (column is SqlPlaceholderExpression)
						{
							projected = ExpressionBuilder.CreatePlaceholder(Parent, SelectQuery, path);
						}
						else
						{
							projected = path;
						}
					}
					else
					{
						projected = path;
					}
				}

				return projected;
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
