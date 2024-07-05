using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	/// <summary>
	/// Provides information about expression nullability in current (sub)query context based on nullability annotations on expressions and outer joins.
	/// </summary>
	public sealed class NullabilityContext
	{
		/// <summary>
		/// Context for non-select queries of places where we don't know select query.
		/// </summary>
		public static NullabilityContext NonQuery { get; } = new(null, null);

		/// <summary>
		/// Creates nullability context for provided query or empty context if query is <c>null</c>.
		/// </summary>
		public static NullabilityContext GetContext(SelectQuery? selectQuery) =>
			selectQuery == null ? NonQuery : new NullabilityContext(selectQuery, null);

		/// <summary>
		/// Creates nullability context for provided query.
		/// </summary>
		public NullabilityContext(SelectQuery inQuery) : this(inQuery, null)
		{
		}

		NullabilityContext(SelectQuery? inQuery, NullabilityCache? nullabilityCache)
		{
			InQuery           = inQuery;
			_nullabilityCache = nullabilityCache;
		}

		/// <summary>
		/// Current context query.
		/// </summary>
		public SelectQuery?     InQuery     { get; }

		[MemberNotNullWhen(false, nameof(InQuery))]
		public bool             IsEmpty     => InQuery == null;

		NullabilityCache? _nullabilityCache;

		bool? CanBeNullInternal(SelectQuery? query, ISqlTableSource source)
		{
			// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
			if (query == null)
			{
				return null;
			}

			_nullabilityCache ??= new();
			return _nullabilityCache.IsNullableSource(query, source);
		}

		public void RegisterReplacement(SelectQuery oldQuery, SelectQuery newQuery)
		{
			_nullabilityCache ??= new();

			_nullabilityCache.RegisterReplacement(oldQuery, newQuery);
		}

		/// <summary>
		/// Returns wether expression could contain null values or not.
		/// </summary>
		public bool CanBeNull(ISqlExpression expression)
		{
			if (expression is SqlColumn column)
			{
				// if column comes from nullable subquery - column is always nullable
				if (column.Parent != null && CanBeNullInternal(InQuery, column.Parent) == true)
					return true;

				// otherwise check column expression nullability
				return CanBeNull(column.Expression);
			}

			if (expression is SqlField field)
			{
				// column is nullable itself or otherwise check if column source nullable
				return field.CanBeNull
					|| (field.Table != null && (CanBeNullInternal(InQuery, field.Table) ?? false));
			}

			// explicit nullability specification
			if (expression is SqlNullabilityExpression nullability)
			{
				return nullability.CanBeNull;
			}

			// allow expression to calculate it's nullability
			return expression.CanBeNullable(this);
		}

		/// <summary>
		/// Collect and cache information about nullablity of each table source in specific <see cref="SelectQuery"/>.
		/// </summary>
		sealed class NullabilityCache
		{
			[DebuggerDisplay("Q[{InQuery.SourceID}] -> TS[{Source.SourceID}]")]
			record struct NullabilityKey(SelectQuery InQuery, ISqlTableSource Source);

			Dictionary<NullabilityKey, bool>?     _nullableSources;
			HashSet<SelectQuery>?                 _processedQueries;
			Dictionary<SelectQuery, SelectQuery>? _replacements;

			/// <summary>
			/// Returns nullability status of <paramref name="source"/> in specific <paramref name="inQuery"/>.
			/// </summary>
			/// <returns>
			/// <list type="bullet">
			/// <item><c>true</c>: <paramref name="source"/> records are nullable in <paramref name="inQuery"/>;</item>
			/// <item><c>false</c>: <paramref name="source"/> records are not nullable in <paramref name="inQuery"/>;</item>
			/// <item><c>null</c>: <paramref name="source"/> is not reachable/available in <paramref name="inQuery"/>.</item>
			/// </list>
			/// </returns>
			public bool? IsNullableSource(SelectQuery inQuery, ISqlTableSource source)
			{
				EnsureInitialized(inQuery);

				if (_nullableSources!.TryGetValue(new(inQuery, source), out var isNullable))
				{
					return isNullable;
				}

				if (_replacements != null && source is SelectQuery sourceQuery)
				{
					var oldSource  = GetReplacement(sourceQuery) ?? source;
					var oldInQuery = GetReplacement(inQuery) ?? inQuery;

					if (!ReferenceEquals(oldSource, source) || !ReferenceEquals(oldInQuery, inQuery))
					{
						if (_nullableSources!.TryGetValue(new(oldInQuery, oldSource), out isNullable))
						{
							return isNullable;
						}
					}
				}

				return null;
			}

			void EnsureInitialized(SelectQuery inQuery)
			{
				_nullableSources  ??= new();
				_processedQueries ??= new HashSet<SelectQuery>();

				ProcessQuery(new Stack<SelectQuery>(), inQuery);
			}

			public void RegisterReplacement(SelectQuery oldQuery, SelectQuery newQuery)
			{
				_replacements           ??= new();

				_replacements[newQuery] = oldQuery;
			}

			public SelectQuery? GetReplacement(SelectQuery newQuery)
			{
				if (_replacements == null)
					return null;

				if (!_replacements.TryGetValue(newQuery, out var oldQuery)) 
					return null;

				while (true)
				{
					if (!_replacements.TryGetValue(oldQuery, out var foundOldQuery))
						break;

					oldQuery = foundOldQuery;
				}

				return oldQuery;
			}

			/// <summary>
			/// Goes from top to down into query and register nullability of each joined table source in current and upper queries.
			/// </summary>
			/// <param name="current">Parent queries stack.</param>
			/// <param name="selectQuery">Current query for which we inspect it's joins.</param>
			void ProcessQuery(Stack<SelectQuery> current, SelectQuery selectQuery)
			{
				void Register(ISqlTableSource source, bool canBeNullTable)
				{
					foreach (var query in current)
					{
						_nullableSources![new (query, source)] = canBeNullTable;
					}
				}

				// cache hit
				if (!_processedQueries!.Add(selectQuery))
					return;

				current.Push(selectQuery);

				foreach (var table in selectQuery.From.Tables)
				{
					if (table.Source is SelectQuery sc)
					{
						ProcessQuery(current, sc);
					}

					var canBeNullTable = table.Joins.Any(static join =>
						join.JoinType == JoinType.Right || join.JoinType == JoinType.RightApply ||
						join.JoinType == JoinType.Full  || join.JoinType == JoinType.FullApply);

					// register nullability of right side of join
					Register(table.Source, canBeNullTable);

					foreach (var join in table.Joins)
					{
						var canBeNullJoin = join.JoinType == JoinType.Full || join.JoinType == JoinType.FullApply ||
							                join.JoinType == JoinType.Left ||
							                join.JoinType == JoinType.OuterApply;

						// register nullability of left right side of join
						Register(join.Table.Source, canBeNullJoin);

						if (join.Table.Source is SelectQuery jc)
						{
							ProcessQuery(current, jc);
						}
					}
				}

				_ = current.Pop();
			}
		}
	}
}
