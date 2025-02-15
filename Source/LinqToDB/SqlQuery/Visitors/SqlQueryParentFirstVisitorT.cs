using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using LinqToDB.Common;

namespace LinqToDB.SqlQuery.Visitors
{
	public class SqlQueryParentFirstVisitor<TContext> : QueryElementVisitor
	{
		Func<TContext, IQueryElement, bool> _action  = default!;
		TContext                            _context = default!;
		HashSet<IQueryElement>?             _visited;

		public SqlQueryParentFirstVisitor() : base(VisitMode.ReadOnly)
		{
		}

		public IQueryElement Visit(TContext context, IQueryElement root, bool visitAll, Func<TContext, IQueryElement, bool> action)
		{
			_context = context;
			_action  = action;
			_visited = visitAll ? null : new(Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);

			return Visit(root);
		}

		public void Cleanup()
		{
			_action  = null!;
			_context = default!;
		}

		[return: NotNullIfNotNull(nameof(element))]
		public override IQueryElement? Visit(IQueryElement? element)
		{
			if (element == null)
				return null;

			if (_visited != null && _visited.Contains(element))
			{
				return element;
			}

			_visited?.Add(element);

			if (!_action(_context, element))
			{
				return element;
			}

			return base.Visit(element);
		}

		protected override ISqlExpression VisitSqlColumnExpression(SqlColumn column, ISqlExpression expression)
		{
			Visit(column);
			return base.VisitSqlColumnExpression(column, column.Expression);
		}

	}
}
