using System;
using System.Collections.Generic;

using LinqToDB.SqlQuery.Visitors;

namespace LinqToDB.SqlQuery
{
	public partial class QueryHelper
	{
		sealed class WrapQueryVisitor<TContext> : SqlQueryVisitor
		{
			IQueryElement       _root = default!;

			public TContext                                         Context  { get; }
			public Func<TContext, SelectQuery, IQueryElement?, int> WrapTest { get; }
			public Action<TContext, IReadOnlyList<SelectQuery>>     OnWrap   { get; }

			public WrapQueryVisitor(
				VisitMode                                        visitMode,
				TContext                                         context,
				Func<TContext, SelectQuery, IQueryElement?, int> wrapTest,
				Action<TContext, IReadOnlyList<SelectQuery>>     onWrap
				) : base(visitMode, null)
			{
				Context  = context;
				WrapTest = wrapTest;
				OnWrap   = onWrap;
			}

			public override IQueryElement ProcessElement(IQueryElement element)
			{
				_root = element;
				var result = base.ProcessElement(element);
				_root = default!;
				return result;
			}

			protected override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
			{
				selectQuery = (SelectQuery)base.VisitSqlQuery(selectQuery);

				var ec = WrapTest(Context, selectQuery, null);
				if (ec <= 0)
					return selectQuery;

				var queries = new List<SelectQuery>(ec);
				for (int i = 0; i < ec; i++)
				{
					var newQuery = new SelectQuery
					{
						IsParameterDependent = selectQuery.IsParameterDependent,
					};
					queries.Add(newQuery);
				}

				queries.Add(selectQuery);

				for (int i = queries.Count - 2; i >= 0; i--)
				{
					queries[i].From.Table(queries[i + 1]);
				}

				for (var index = 0; index < selectQuery.Select.Columns.Count; index++)
				{
					var prevColumn = selectQuery.Select.Columns[index];
					var newColumn  = prevColumn;
					for (int ic = ec - 1; ic >= 0; ic--)
					{
						newColumn = queries[ic].Select.AddNewColumn(newColumn);
					}
				}

				var newRootQuery = queries[0];

				for (var index = 0; index < newRootQuery.Select.Columns.Count; index++)
				{
					var newColumn      = newRootQuery.Select.Columns[index];
					var originalColumn = selectQuery.Select.Columns[index];
					NotifyReplaced(newColumn, originalColumn);
				}

				NotifyReplaced(newRootQuery, selectQuery);
				// Idea that this will stop Replacer to modify newRootQuery
				NotifyReplaced(newRootQuery, newRootQuery);

				OnWrap(Context, queries);

				return newRootQuery;
			}
		}

		/// <summary>
		/// Wraps tested query in subquery(s).
		/// Keeps columns count the same. After modification statement is equivalent semantically.
		/// <code>
		/// --before
		/// SELECT c1, c2           -- QA
		/// FROM A
		/// -- after (with 2 subqueries)
		/// SELECT C.c1, C.c2       -- QC
		/// FROM (
		///   SELECT B.c1, B.c2     -- QB
		///   FROM (
		///     SELECT c1, c2       -- QA
		///     FROM A
		///        ) B
		///   FROM
		///      ) C
		/// </code>
		/// </summary>
		/// <typeparam name="TStatement"></typeparam>
		/// <typeparam name="TContext">Type of <paramref name="onWrap"/> and <paramref name="wrapTest"/> context object.</typeparam>
		/// <param name="context"><paramref name="onWrap"/> and <paramref name="wrapTest"/> context object.</param>
		/// <param name="statement">Statement which may contain tested query</param>
		/// <param name="wrapTest">Delegate for testing which query needs to be enveloped.
		/// Result of delegate call tells how many subqueries needed.
		/// 0 - no changes
		/// 1 - one subquery
		/// N - N subqueries
		/// </param>
		/// <param name="onWrap">
		/// After wrapping query this function called for prcess needed optimizations. Array of queries contains [QC, QB, QA]
		/// </param>
		/// <param name="allowMutation">Wrapped query can be not recreated for performance considerations.</param>
		/// <param name="withStack">Must be set to <c>true</c>, if <paramref name="wrapTest"/> function use 3rd parameter (containing parent element) otherwise it will be always null.</param>
		/// <returns>The same <paramref name="statement"/> or modified statement when wrapping has been performed.</returns>
		public static TStatement WrapQuery<TStatement, TContext>(
			TContext                                         context,
			TStatement                                       statement,
			Func<TContext, SelectQuery, IQueryElement?, int> wrapTest,
			Action<TContext, IReadOnlyList<SelectQuery>>     onWrap,
			bool                                             allowMutation,
			bool                                             withStack)
			where TStatement : SqlStatement
		{
			if (statement == null) throw new ArgumentNullException(nameof(statement));
			if (wrapTest  == null) throw new ArgumentNullException(nameof(wrapTest));
			if (onWrap    == null) throw new ArgumentNullException(nameof(onWrap));

			var visitor = new WrapQueryVisitor<TContext>(allowMutation ? VisitMode.Modify : VisitMode.Transform, context, wrapTest, onWrap);
			var newStatement = (TStatement)visitor.ProcessElement(statement);

			return newStatement;
		}

		/// <summary>
		/// Wraps <paramref name="queryToWrap"/> by another select.
		/// Keeps columns count the same. After modification statement is equivalent symantically.
		/// <code>
		/// --before
		/// SELECT c1, c2
		/// FROM A
		/// -- after
		/// SELECT B.c1, B.c2
		/// FROM (
		///   SELECT c1, c2
		///   FROM A
		///      ) B
		/// </code>
		/// </summary>
		/// <typeparam name="TStatement"></typeparam>
		/// <param name="statement">Statement which may contain tested query</param>
		/// <param name="queryToWrap">Tells which select query needs enveloping</param>
		/// <param name="allowMutation">Wrapped query can be not recreated for performance considerations.</param>
		/// <returns>The same <paramref name="statement"/> or modified statement when wrapping has been performed.</returns>
		public static TStatement WrapQuery<TStatement>(
			TStatement  statement,
			SelectQuery queryToWrap,
			bool        allowMutation)
			where TStatement : SqlStatement
		{
			if (statement == null) throw new ArgumentNullException(nameof(statement));

			return WrapQuery(queryToWrap, statement, static (queryToWrap, q, _) => q == queryToWrap, null, allowMutation, false);
		}

		/// <summary>
		/// Wraps queries by another select.
		/// Keeps columns count the same. After modification statement is equivalent symantically.
		/// </summary>
		/// <typeparam name="TStatement"></typeparam>
		/// <typeparam name="TContext">Type of <paramref name="onWrap"/> and <paramref name="wrapTest"/> context object.</typeparam>
		/// <param name="context"><paramref name="onWrap"/> and <paramref name="wrapTest"/> context object.</param>
		/// <param name="statement"></param>
		/// <param name="wrapTest">Delegate for testing when query needs to be wrapped.</param>
		/// <param name="onWrap">After enveloping query this function called for prcess needed optimizations.</param>
		/// <param name="allowMutation">Wrapped query can be not recreated for performance considerations.</param>
		/// <param name="withStack">Must be set to <c>true</c>, if <paramref name="wrapTest"/> function use 3rd parameter (containing parent element) otherwise it will be always null.</param>
		/// <returns>The same <paramref name="statement"/> or modified statement when wrapping has been performed.</returns>
		public static TStatement WrapQuery<TStatement, TContext>(
			TContext                                          context,
			TStatement                                        statement,
			Func<TContext, SelectQuery, IQueryElement?, bool> wrapTest,
			Action<TContext, SelectQuery, SelectQuery>?       onWrap,
			bool                                              allowMutation,
			bool                                              withStack)
			where TStatement : SqlStatement
		{
			if (statement == null) throw new ArgumentNullException(nameof(statement));
			if (wrapTest == null)  throw new ArgumentNullException(nameof(wrapTest));

			return WrapQuery(
				(context, wrapTest, onWrap),
				statement,
				static (context, q, pe  ) => context.wrapTest(context.context, q, pe) ? 1 : 0,
				static (context, queries) => context.onWrap?.Invoke(context.context, queries[0], queries[1]),
				allowMutation,
				withStack);
		}
	}
}
