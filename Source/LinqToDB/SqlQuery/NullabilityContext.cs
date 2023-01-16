namespace LinqToDB.SqlQuery
{
	public class NullabilityContext
	{
		public NullabilityContext(SelectQuery inQuery) : this(inQuery, null)
		{
		}

		public NullabilityContext(SelectQuery inQuery, ISqlTableSource? forSource)
		{
			InQuery = inQuery;
			ForSource = forSource;
		}

		NullabilityContext(SelectQuery inQuery, ISqlTableSource? forSource, Dictionary<SelectQuery, NullabilityContext> nullabilityCache)
			: this(inQuery, forSource)
		{
			_nullabilityCache = nullabilityCache;
		}

		public static NullabilityContext NonQuery { get; } = new(null!, null, null!);

		public SelectQuery InQuery { get; }
		public ISqlTableSource? ForSource { get; }

		public NullabilityContext WitSource(ISqlTableSource forSource)
		{
			if (ForSource == forSource)
				return this;

			_nullabilityCache ??= new();
			return new NullabilityContext(InQuery, forSource, _nullabilityCache);
		}

		Dictionary<ISqlTableSource, bool>? _nullableSources;
		Dictionary<SelectQuery, NullabilityContext>? _nullabilityCache;

		bool? CanBeNullInternal(SelectQuery query, ISqlTableSource source)
		{
			if (ForSource == source)
				return null;

			// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
			// Handling fake case
			if (InQuery == null)
				return null;

			if (query != InQuery)
			{
				_nullabilityCache ??= new();

				if (!_nullabilityCache.TryGetValue(query, out var nullabilityContext))
				{
					nullabilityContext = new NullabilityContext(query, null, _nullabilityCache);
					_nullabilityCache.Add(query, nullabilityContext);
				}

				// let the cache grow
				return nullabilityContext.CanBeNullInternal(query, source);
			}

			if (_nullableSources == null)
			{
				_nullableSources = new ();

				foreach (var table in InQuery.From.Tables)
				{
					var canBeNullTable = table.Joins.Any(join =>
						join.JoinType == JoinType.Right || join.JoinType == JoinType.RightApply ||
						join.JoinType == JoinType.Full  || join.JoinType == JoinType.FullApply);

					_nullableSources[table.Source] = canBeNullTable;

					foreach (var join in table.Joins)
					{
						{
							var canBeNullJoin = join.JoinType == JoinType.Full || join.JoinType == JoinType.FullApply ||
												join.JoinType == JoinType.Left ||
												join.JoinType == JoinType.OuterApply;

							_nullableSources[join.Table.Source] = canBeNullJoin;

							if (join.Table.Source is SelectQuery sc)
							{
								_nullabilityCache ??= new();

								if (!_nullabilityCache.TryGetValue(sc, out var nullabilityContext))
								{
									nullabilityContext = new NullabilityContext(sc);
									_nullabilityCache.Add(sc, nullabilityContext);
								}
							}
						}
					}
				}
			}

			if (_nullableSources.TryGetValue(source, out var isNullable))
			{
				return isNullable;
			}

			return null;
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

			return expression.CanBeNull;
		}
	}
}
