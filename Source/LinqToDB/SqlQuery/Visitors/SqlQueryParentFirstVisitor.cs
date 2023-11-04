﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.SqlQuery.Visitors
{
	using Common;

	public class SqlQueryParentFirstVisitor : QueryElementVisitor
	{
		Func<IQueryElement, bool> _action = default!;
		HashSet<IQueryElement>?   _visited;

		public SqlQueryParentFirstVisitor() : base(VisitMode.ReadOnly)
		{
		}

		public IQueryElement Visit(IQueryElement root, bool visitAll, Func<IQueryElement, bool> action)
		{
			_action  = action;
			_visited = visitAll ? null : new(Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);

			return Visit(root);
		}

		public void Cleanup()
		{
			_action  = null!;
			_visited = null;
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

			if (!_action(element))
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
