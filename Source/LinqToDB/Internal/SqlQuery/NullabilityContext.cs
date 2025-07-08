using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// Provides information about expression nullability in current (sub)query context based on nullability annotations on expressions and outer joins.
	/// </summary>
	public sealed class NullabilityContext
	{
		/// <summary>
		/// Context for non-select queries of places where we don't know select query.
		/// </summary>
		public static NullabilityContext NonQuery { get; } = new([], null, null, null);

		/// <summary>
		/// Creates nullability context for provided query or empty context if query is <c>null</c>.
		/// </summary>
		public static NullabilityContext GetContext(SelectQuery? selectQuery) =>
			selectQuery == null ? NonQuery : new NullabilityContext([selectQuery], null, null, null);

		/// <summary>
		/// Creates nullability context for provided query.
		/// </summary>
		public NullabilityContext(SelectQuery inQuery) : this([inQuery], null, null, null)
		{
		}

		NullabilityContext(SelectQuery[] queries, NullabilityCache? nullabilityCache, ISqlTableSource? joinSource, SqlQueryVisitor.IVisitorTransformationInfo? transformationInfo)
		{
			Queries             = queries;
			_nullabilityCache   = nullabilityCache;
			_transformationInfo = transformationInfo;
			JoinSource          = joinSource;
		}

		public NullabilityContext WithTransformationInfo(SqlQueryVisitor.IVisitorTransformationInfo? transformationInfo)
		{
			if (ReferenceEquals(transformationInfo, _transformationInfo))
				return this;

			return new NullabilityContext(Queries, _nullabilityCache, JoinSource, transformationInfo);
		}

		public NullabilityContext WithJoinSource(ISqlTableSource? joinSource)
		{
			if (ReferenceEquals(JoinSource, joinSource))
				return this;

			return new NullabilityContext(Queries, _nullabilityCache, joinSource, _transformationInfo);
		}

		public NullabilityContext WithQuery(SelectQuery inQuery)
		{
			if (Queries.Contains(inQuery))
				return this;

			return new NullabilityContext([..Queries, inQuery], _nullabilityCache, JoinSource, _transformationInfo);
		}

		public NullabilityContext(NullabilityContext parentContext, Dictionary<ISqlExpression, bool> nullabilityOverrides)
		{
			_parentContext        = parentContext;
			_nullabilityOverrides = nullabilityOverrides;
			Queries               = parentContext.Queries;
		}

		/// <summary>
		/// Current context query.
		/// </summary>
		public SelectQuery[]     Queries     { get; }

		/// <summary>
		/// Current Join table source. Used for excluding source from nullable sources check
		/// </summary>
		public ISqlTableSource? JoinSource { get; }

		[MemberNotNullWhen(false, nameof(Queries))]
		public bool             IsEmpty     => Queries == null;

		NullabilityCache?                                    _nullabilityCache;
		readonly SqlQueryVisitor.IVisitorTransformationInfo? _transformationInfo;

		public bool? CanBeNullSource(ISqlTableSource source)
		{
			if (ReferenceEquals(JoinSource, source))
				return null;

			for (var index = Queries.Length - 1; index >= 0; index--)
			{
				var q     = Queries[index];
				var local = CanBeNullInternal(q, source);
				if (local != null)
					return local;
			}

			return null;
		}

		readonly NullabilityContext?               _parentContext;
		readonly Dictionary<ISqlExpression, bool>? _nullabilityOverrides;

		bool? CanBeNullInternal(SelectQuery? query, ISqlTableSource source)
		{
			// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
			if (query == null)
			{
				return null;
			}

			_nullabilityCache ??= new();
			return _nullabilityCache.IsNullableSource(query, source, JoinSource, _transformationInfo);
		}

		/// <summary>
		/// Returns wether expression could contain null values or not.
		/// </summary>
		public bool CanBeNull(ISqlExpression expression)
		{
			if (_nullabilityOverrides?.TryGetValue(expression, out var nullabilityOverride) == true)
			{
				return nullabilityOverride;
			}

			if (_parentContext != null)
			{
				return _parentContext.CanBeNull(expression);
			}

			if (expression is SqlColumn column)
			{
				// if column comes from nullable subquery - column is always nullable
				if (column.Parent != null)
				{
					if (CanBeNullSource(column.Parent) == true)
						return true;

					if (column.Parent.HasSetOperators)
					{
						var index = column.Parent.Select.Columns.IndexOf(column);
						if (index < 0) return true;

						foreach (var set in column.Parent.SetOperators)
						{
							if (index >= set.SelectQuery.Select.Columns.Count)
								return true;

							if (set.SelectQuery.Select.Columns[index].CanBeNullable(this))
								return true;
						}
					}
				}

				// otherwise check column expression nullability
				return CanBeNull(column.Expression);
			}

			if (expression is SqlField field)
			{
				// field is nullable itself or otherwise check if field's source nullable

				if (field.CanBeNull || field.Table == null)
					return true;

				if (CanBeNullSource(field.Table) == true)
					return true;

				return false;
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
			Dictionary<(SelectQuery inQuery, ISqlTableSource source, ISqlTableSource? joindeSource), bool?>? _nullabilityInfo;

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
			public bool? IsNullableSource(SelectQuery inQuery, ISqlTableSource source, ISqlTableSource? joinedTable, SqlQueryVisitor.IVisitorTransformationInfo? transformationInfo)
			{
				_nullabilityInfo ??= new();

				var key = (inQuery, source, joinedTable);

				if (_nullabilityInfo.TryGetValue((inQuery, source, joinedTable), out var result))
				{
					return result;
				}

				result = IsNullableSourceCalculator(inQuery, source, joinedTable);

				if (result == null && transformationInfo != null)
				{
					var oldSource  = transformationInfo.GetOriginal(source) as ISqlTableSource;
					var oldInQuery = transformationInfo.GetOriginal(inQuery) as ISqlTableSource; 

					if ((!ReferenceEquals(oldSource, source) || !ReferenceEquals(oldInQuery, inQuery)) && oldInQuery is SelectQuery oldInQuerySelect && oldSource != null)
					{
						result = IsNullableSource(oldInQuerySelect, oldSource, joinedTable, transformationInfo);
					}
				}

				_nullabilityInfo[key] = result;

				return result;
			}

			bool? IsNullableSourceCalculator(SelectQuery inQuery, ISqlTableSource source, ISqlTableSource? joinedTable)
			{
				if (inQuery == source)
					return false;

				var stack = new Stack<(ISqlTableSource tableSource, bool isNullable)>();

				stack.Push((inQuery, false));

				while (stack.Count > 0)
				{
					var (currentSource, currentNullable) = stack.Pop();

					if (currentSource == source)
					{
						return currentNullable;
					}

					if (currentSource is SelectQuery query)
					{
						foreach(var ts in query.From.Tables)
						{
							stack.Push((ts, currentNullable));
						}
					}
					else if (currentSource is SqlTableSource tableSource)
					{
						var applyNullable = currentNullable;
						for (var index = tableSource.Joins.Count - 1; index >= 0; index--)
						{
							var join = tableSource.Joins[index];

							if (join.Table.Source == joinedTable)
							{
								applyNullable = currentNullable;
							}
							else if (join.JoinType is JoinType.Right or JoinType.RightApply)
							{
								stack.Push((join.Table, applyNullable));
								applyNullable = true;
								continue;
							}
							else if (join.JoinType is JoinType.Full or JoinType.FullApply)
							{
								applyNullable = true;
							}
							else if (join.JoinType is JoinType.Left or JoinType.OuterApply)
							{
								stack.Push((join.Table, true));
								continue;
							}

							stack.Push((join.Table, applyNullable));
						}

						stack.Push((tableSource.Source, applyNullable));

					}
				}

				return null;
			}
		}
	}
}
