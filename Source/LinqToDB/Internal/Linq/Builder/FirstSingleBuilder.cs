using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Async;
using LinqToDB.Internal.Async;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(
		nameof(Queryable.First),
		nameof(Queryable.FirstOrDefault),
		nameof(Queryable.Single),
		nameof(Queryable.SingleOrDefault),
		nameof(LinqInternalExtensions.AssociationRecord),
		nameof(LinqInternalExtensions.AssociationOptionalRecord))]
	[BuildsMethodCall(
		nameof(AsyncExtensions.FirstAsync),
		nameof(AsyncExtensions.FirstOrDefaultAsync),
		nameof(AsyncExtensions.SingleAsync),
		nameof(AsyncExtensions.SingleOrDefaultAsync),
		CanBuildName = nameof(CanBuildAsyncMethod))]
	sealed class FirstSingleBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsQueryable() && call.Arguments.Count <= 2;

		public static bool CanBuildAsyncMethod(MethodCallExpression call)
			=> call.IsAsyncExtension() && call.Arguments.Count <= 3;

		public enum MethodKind
		{
			First,
			FirstOrDefault,
			Single,
			SingleOrDefault,
			AssociationRecord,
			AssociationOptionalRecord,
		}

		static MethodKind GetMethodKind(string methodName)
		{
			return methodName switch
			{
				nameof(Queryable.First)                                  => MethodKind.First,
				nameof(AsyncExtensions.FirstAsync)                       => MethodKind.First,
				nameof(Queryable.FirstOrDefault)                         => MethodKind.FirstOrDefault,
				nameof(AsyncExtensions.FirstOrDefaultAsync)              => MethodKind.FirstOrDefault,
				nameof(Queryable.Single)                                 => MethodKind.Single,
				nameof(AsyncExtensions.SingleAsync)                      => MethodKind.Single,
				nameof(Queryable.SingleOrDefault)                        => MethodKind.SingleOrDefault,
				nameof(AsyncExtensions.SingleOrDefaultAsync)             => MethodKind.SingleOrDefault,
				nameof(LinqInternalExtensions.AssociationRecord)         => MethodKind.AssociationRecord,
				nameof(LinqInternalExtensions.AssociationOptionalRecord) => MethodKind.AssociationOptionalRecord,
				_ => throw new ArgumentOutOfRangeException(nameof(methodName), methodName, "Not supported method.")
			};
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var argument = methodCall.Arguments[0];
			var argumentCount = methodCall.Arguments.Count;

			if (methodCall.IsAsyncExtension())
				--argumentCount;

			var cardinality = buildInfo.SourceCardinality;

			if (buildInfo.SourceCardinality != SourceCardinality.Unknown)
			{
				cardinality &= ~SourceCardinality.Many;
			}

			cardinality |= SourceCardinality.One;
			var methodKind = GetMethodKind(methodCall.Method.Name);

			switch (methodKind)
			{
				case MethodKind.AssociationRecord:
				case MethodKind.First:
				case MethodKind.Single:
					break;

				case MethodKind.FirstOrDefault:
				case MethodKind.SingleOrDefault:
				case MethodKind.AssociationOptionalRecord:
				{
					cardinality |= SourceCardinality.Zero;
					break;
				}
			}

			var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, argument)
			{
				SourceCardinality = cardinality
			});

			if (buildResult.BuildContext == null)
				return buildResult;

			var sequence = buildResult.BuildContext;

			if (argumentCount > 1)
			{
				var filterLambda = methodCall.Arguments[1].UnwrapLambda();
				sequence = builder.BuildWhere(sequence, filterLambda, enforceHaving: false, out var error);

				if (sequence == null)
					return BuildSequenceResult.Error(error ?? methodCall);
			}

			sequence = new SubQueryContext(sequence);

			var take = 0;

			switch (methodKind)
			{
				case MethodKind.First          :
				{
					take        = 1;
					break;
				}
				case MethodKind.FirstOrDefault :
				{
					take        = 1;
					break;
				}
				case MethodKind.Single          :
				case MethodKind.SingleOrDefault :
				{
					if (!buildInfo.IsSubQuery)
					{
						if (buildInfo.SelectQuery.Select.TakeValue is null or SqlValue { Value: >= 2 })
						{
							take = 2;
						}
					}

					break;
				}
			}

			if (take != 0)
			{
				var takeExpression = new SqlValue(sequence.MappingSchema.GetDbDataType(typeof(int)), take);
				builder.BuildTake(sequence, takeExpression, null);
			}

			var canBeWeak = false;

			if (buildInfo.Parent != null && (cardinality & SourceCardinality.Zero) != 0)
			{
				sequence = new DefaultIfEmptyBuilder.DefaultIfEmptyContext(
					buildInfo.Parent,
					sequence,
					sequence,
					defaultValue: null,
					isNullValidationDisabled: false);

				canBeWeak = true;
			}

			var isSubqueryExpression = buildInfo.IsSubqueryExpression && methodKind is not MethodKind.AssociationRecord;

			var firstSingleContext = new FirstSingleContext(buildInfo.Parent, sequence, methodKind, 
				isSubQuery: buildInfo.IsSubQuery, 
				isAssociation: buildInfo.IsAssociation,
				isSubqueryExpression: isSubqueryExpression,
				canBeWeak: canBeWeak, cardinality);
			
			return BuildSequenceResult.FromContext(firstSingleContext);
		}

		public sealed class FirstSingleContext : SequenceContextBase
		{
			public FirstSingleContext(IBuildContext? parent, IBuildContext sequence, MethodKind methodKind,
				bool isSubQuery, bool isAssociation, bool isSubqueryExpression, bool canBeWeak, SourceCardinality cardinality)
				: base(parent, sequence, null)
			{
				_methodKind          = methodKind;
				IsSubqueryExpression = isSubqueryExpression;
				IsSubQuery           = isSubQuery;
				IsAssociation        = isAssociation;
				CanBeWeak            = canBeWeak;
				Cardinality          = cardinality;
			}

			readonly MethodKind       _methodKind;

			public bool              IsSubQuery           { get; }
			public bool              IsSubqueryExpression { get; }
			public bool              IsAssociation        { get; }
			public bool              CanBeWeak            { get; }
			public SourceCardinality Cardinality          { get; set; }

			public override bool IsOptional => (Cardinality & SourceCardinality.Zero) != 0 || Cardinality == SourceCardinality.Unknown;

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
					await AsyncEnumerableExtensions.FirstAsync(query.GetResultEnumerable(db, expr, ps, preambles), token).ConfigureAwait(false);
			}

			static void GetFirstOrDefaultElement<T>(Query<T> query)
			{
				query.GetElement = (db, expr, ps, preambles) =>
					query.GetResultEnumerable(db, expr, ps, preambles).FirstOrDefault();

				query.GetElementAsync = async (db, expr, ps, preambles, token) =>
					await AsyncEnumerableExtensions.FirstOrDefaultAsync(query.GetResultEnumerable(db, expr, ps, preambles), token).ConfigureAwait(false);
			}

			static void GetSingleElement<T>(Query<T> query)
			{
				query.GetElement = (db, expr, ps, preambles) =>
					query.GetResultEnumerable(db, expr, ps, preambles).Single();

				query.GetElementAsync = async (db, expr, ps, preambles, token) =>
					await AsyncEnumerableExtensions.SingleAsync(query.GetResultEnumerable(db, expr, ps, preambles), token).ConfigureAwait(false);
			}

			static void GetSingleOrDefaultElement<T>(Query<T> query)
			{
				query.GetElement = (db, expr, ps, preambles) =>
					query.GetResultEnumerable(db, expr, ps, preambles).SingleOrDefault();

				query.GetElementAsync = async (db, expr, ps, preambles, token) =>
					await AsyncEnumerableExtensions.SingleOrDefaultAsync(query.GetResultEnumerable(db, expr, ps, preambles), token).ConfigureAwait(false);
			}

			bool _isJoinCreated;
			bool _asSubquery;

			public void CreateJoin()
			{
				if (_isJoinCreated  || _asSubquery)
					return;

				if (Parent == null)
					return;

				// process as subquery
				if (Parent.SelectQuery.From.Tables.Count == 0)
				{
					_asSubquery = true;
					return;
				}

				if (!_isJoinCreated)
				{
					_isJoinCreated = true;

					var join = CanBeWeak ? SelectQuery.OuterApply() : SelectQuery.CrossApply();
					join.JoinedTable.IsWeak               = Cardinality.HasFlag(SourceCardinality.Zero);
					join.JoinedTable.Cardinality          = Cardinality;
					join.JoinedTable.IsSubqueryExpression = IsSubqueryExpression;

					Parent!.SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);
				}
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if ((flags.IsAssociationRoot() || flags.IsRoot()) && SequenceHelper.IsSameContext(path, this))
				{
					return path;
				}

				if (IsSubQuery)
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

					projected = Builder.BuildSqlExpression(this, projected);

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
					_methodKind, IsSubQuery, IsAssociation, IsSubqueryExpression, CanBeWeak, Cardinality)
				{
					_isJoinCreated = _isJoinCreated
				};
			}

			public override void Detach()
			{
				if (_isJoinCreated)
				{
					var joins = Parent!.SelectQuery.From.Tables[0].Joins;
					var found = joins.Find(j => j.Table.Source == SelectQuery);
					if (found != null)
					{
						joins.Remove(found);
					}
				}
			}

			public override bool IsSingleElement => true;
		}
	}
}
