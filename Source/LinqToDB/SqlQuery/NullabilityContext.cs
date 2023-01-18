using System.Diagnostics;

namespace LinqToDB.SqlQuery
{
	public class NullabilityContext
	{
		public static NullabilityContext NonQuery { get; } = new(null!, null, null!);

		public static NullabilityContext GetContext(SelectQuery? selectQuery) =>
			selectQuery == null ? NonQuery : new NullabilityContext(selectQuery);


		public NullabilityContext(SelectQuery inQuery) : this(inQuery, null)
		{
		}

		public NullabilityContext(SelectQuery inQuery, ISqlTableSource? forSource)
		{
			InQuery   = inQuery;
			ForSource = forSource;
		}

		NullabilityContext(SelectQuery inQuery, ISqlTableSource? forSource, NullabilityCache nullabilityCache)
			: this(inQuery, forSource)
		{
			_nullabilityCache = nullabilityCache;
		}

		public SelectQuery InQuery { get; }
		public ISqlTableSource? ForSource { get; }

		NullabilityCache? _nullabilityCache;

		public NullabilityContext WitSource(ISqlTableSource forSource)
		{
			if (ForSource == forSource)
				return this;

			_nullabilityCache ??= new();
			return new NullabilityContext(InQuery, forSource, _nullabilityCache);
		}

		bool? CanBeNullInternal(SelectQuery query, ISqlTableSource source)
		{
			// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
			if (query == null)
			{
				return null;
			}

			_nullabilityCache ??= new();
			return _nullabilityCache.IsNullableSource(query, source, ForSource);
		}

		public bool CanBeNull(ISqlExpression expression)
		{
			if (expression is SqlColumn column)
			{
				if (column.Parent == null)
					return CanBeNull(column.Expression);
				var canBeNullQuery = CanBeNullInternal(InQuery, column.Parent);
				if (canBeNullQuery == true)
					return true;

				if (CanBeNull(column.Expression))
					return true;

				return false;
			}

			if (expression is SqlField field)
			{
				if (field.Table == null)
					return false;
				if (field.CanBeNull)
					return true;
				return CanBeNullInternal(InQuery, field.Table) ?? false;
			}

			if (expression.CanBeNullable(this))
				return true;

			return false;
		}

		public class NullabilityCache
		{
			[DebuggerDisplay("Q[{InQuery.SourceID}] -> TS[{Source.SourceID}]")]
			record struct NullabilityKey(SelectQuery InQuery, ISqlTableSource Source);

			Dictionary<NullabilityKey, bool>? _nullableSources;
			HashSet<SelectQuery>? _processedQueries;

			public bool? IsNullableSource(SelectQuery inQuery, ISqlTableSource source, ISqlTableSource? forSource)
			{
				if (source == forSource)
					return null;

				_nullableSources ??= new();
				_processedQueries ??= new HashSet<SelectQuery>();

				ProcessQuery(new Stack<SelectQuery>(), inQuery);

				if (_nullableSources.TryGetValue(new(inQuery, source), out var isNullable))
				{
					return isNullable;
				}

				return null;
			}

			void ProcessQuery(Stack<SelectQuery> current, SelectQuery selectQuery)
			{
				void Register(ISqlTableSource source, bool canBeNullTable)
				{
					foreach (var query in current)
					{
						_nullableSources![new (query, source)] = canBeNullTable;
					}
				}

				if (!_processedQueries!.Add(selectQuery))
					return;

				current.Push(selectQuery);

				foreach (var table in selectQuery.From.Tables)
				{
					if (table.Source is SelectQuery sc)
					{
						ProcessQuery(current, sc);
					}

					var canBeNullTable = table.Joins.Any(join =>
						join.JoinType == JoinType.Right || join.JoinType == JoinType.RightApply ||
						join.JoinType == JoinType.Full  || join.JoinType == JoinType.FullApply);

					Register(table.Source, canBeNullTable);

					foreach (var join in table.Joins)
					{
						{
							var canBeNullJoin = join.JoinType == JoinType.Full || join.JoinType == JoinType.FullApply ||
								                join.JoinType == JoinType.Left ||
								                join.JoinType == JoinType.OuterApply;

							Register(join.Table.Source, canBeNullJoin);

							if (join.Table.Source is SelectQuery jc)
							{
								ProcessQuery(current, jc);
							}
						}
					}
				}

				_ = current.Pop();
			}
		}

	}
}
