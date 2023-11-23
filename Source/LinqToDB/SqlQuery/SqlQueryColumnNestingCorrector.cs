using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LinqToDB.SqlQuery
{
	using Visitors;

	public class SqlQueryColumnNestingCorrector : SqlQueryVisitor
	{
		[DebuggerDisplay("QN(S:{TableSource.SourceID})")]
		class QueryNesting
		{
			public QueryNesting(QueryNesting? parent, ISqlTableSource tableSource)
			{
				TableSource = tableSource;
				Parent      = parent;
				if (parent != null)
					parent.AddSource(this);
			}

			public QueryNesting?       Parent      { get; }
			public ISqlTableSource     TableSource { get; }
			public List<QueryNesting>? Sources     { get; private set; }

			void AddSource(QueryNesting source)
			{
				Sources ??= new();

				if (!Sources.Contains(source))
					Sources.Add(source);
			}

			public QueryNesting? FindNesting(ISqlTableSource tableSource)
			{
				if (Sources != null)
				{
					foreach (var s in Sources)
					{
						if (s.TableSource == tableSource)
							return this;
						var result = s.FindNesting(tableSource);
						if (result != null) 
							return result;
					}
				}

				return null;
			}

			public static bool UpdateNesting(QueryNesting upTo, QueryNesting current, ISqlExpression element, out ISqlExpression newElement)
			{
				while (upTo != current)
				{
					newElement = current.UpdateNesting(element);

					if (current.Parent == null)
						throw new InvalidOperationException("Invalid nesting tree.");

					current = current.Parent;
					element = newElement;
				}

				newElement = element;
				return true;
			}

			public ISqlExpression UpdateNesting(ISqlExpression element)
			{
				if (TableSource is SelectQuery selectQuery)
				{
					return selectQuery.Select.AddColumn(element);
				}

				return element;
			}

		}

		QueryNesting?       _parentQuery;

		public SqlQueryColumnNestingCorrector() : base(VisitMode.Modify)
		{
		}

		public override void Cleanup()
		{
			base.Cleanup();
			_parentQuery = null;
		}

		public IQueryElement CorrectColumnNesting(IQueryElement element)
		{
			var result = Visit(element);
			return result;
		}

		IQueryElement ProcessNesting(ISqlTableSource elementSource, ISqlExpression element)
		{
			if (_parentQuery == null)
				return element;

			var current = _parentQuery;
			while (current != null)
			{
				var found = current.FindNesting(elementSource);

				if (found != null)
				{
					if (!QueryNesting.UpdateNesting(current, found, element, out var newElement))
						throw new InvalidOperationException();

#if DEBUG
					if (!ReferenceEquals(newElement, element))
					{
						Debug.WriteLine($"Corrected nesting: {element} -> {newElement}");
					}
#endif
					return newElement;
				}

				current = current.Parent;
			}

			return element;
		}

		protected override IQueryElement VisitSqlFieldReference(SqlField element)
		{
			var newElement = base.VisitSqlFieldReference(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			if (element.Table != null)
			{
				newElement = ProcessNesting(element.Table, element);
			}

			return newElement;
		}

		protected override IQueryElement VisitSqlColumnReference(SqlColumn element)
		{
			var newElement = base.VisitSqlColumnReference(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			if (element.Parent != null)
			{
				newElement = ProcessNesting(element.Parent, element);
			}

			return newElement;
		}

		protected override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
		{
			var saveQuery = _parentQuery;

			_parentQuery = new QueryNesting(saveQuery, selectQuery);

			var newQuery = base.VisitSqlQuery(selectQuery);

			_parentQuery = saveQuery;

			return newQuery;
		}

		protected override IQueryElement VisitSqlTableSource(SqlTableSource element)
		{
			_ = new QueryNesting(_parentQuery, element.Source);

			var newElement = base.VisitSqlTableSource(element);

			return newElement;
		}

	}
}
