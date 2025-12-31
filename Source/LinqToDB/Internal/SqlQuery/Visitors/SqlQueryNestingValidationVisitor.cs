using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Internal.SqlQuery.Visitors
{
	public class SqlQueryNestingValidationVisitor : QueryElementVisitor
	{
		readonly bool                         _isSubQuery;
		readonly IQueryElement                _forStatement;

		List<ISqlTableSource>? _outerSources;
		List<ISqlTableSource>? _querySources;

		readonly HashSet<ISqlTableSource>     _spotted        = new ();
		SelectQuery?                          _currentQuery;

		public SqlQueryNestingValidationVisitor(bool isSubQuery, IQueryElement forStatement) : base(VisitMode.ReadOnly)
		{
			_isSubQuery   = isSubQuery;
			_forStatement = forStatement;
		}

		protected internal override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
		{
			var saveQuery   = _currentQuery;
			var saveOuter   = _outerSources;
			var saveSources = _querySources;

			_querySources = new List<ISqlTableSource>();

			_currentQuery = selectQuery;

			base.VisitSqlQuery(selectQuery);

			_currentQuery = saveQuery;
			_outerSources = saveOuter;
			_querySources = saveSources;

			return selectQuery;
		}

		Exception CreateErrorMessage(bool sourceInQuery, IQueryElement element)
		{
			var messageString = $"Element: '{element.ToDebugString()}' ";

			if (!sourceInQuery)
			{
				messageString += "has unknown source.\n";
				messageString += "-----------------------------------------\n";
				messageString += $"In Statement:\n";
				messageString += "-----------------------------------------\n";
				messageString += $"{_forStatement.ToDebugString()}\n";
			}
			else
			{
				messageString += "has wrong nesting.\n";

				messageString += "-----------------------------------------\n";
				messageString += $"SubQuery:\n";
				messageString += "-----------------------------------------\n";
				messageString += $"{_currentQuery!.ToDebugString()}\n";
				messageString += "-----------------------------------------\n";
				messageString += $"In Statement:\n";
				messageString += "-----------------------------------------\n";
				messageString += $"{_forStatement.ToDebugString()}\n";
			}

			return new InvalidOperationException(messageString);
		}

		protected internal override IQueryElement VisitSqlFieldReference(SqlField element)
		{
			if (element.Table != null)
			{
				var sourceInQuery = _spotted.Contains(element.Table);
				if (!_isSubQuery || sourceInQuery)
				{
					var contains = _querySources?.Contains(element.Table) == true || _outerSources?.Contains(element.Table) == true;
					if (!contains)
					{
						throw CreateErrorMessage(sourceInQuery, element);
					}
				}
			}

			return base.VisitSqlFieldReference(element);
		}

		protected internal override IQueryElement VisitSqlOrderByItem(SqlOrderByItem element)
		{
			var saveOuter = _outerSources;

			_outerSources = (_outerSources ?? Enumerable.Empty<ISqlTableSource>())
				.Concat(_querySources ?? Enumerable.Empty<ISqlTableSource>())
				.ToList();

			base.VisitSqlOrderByItem(element);

			_outerSources = saveOuter;

			return element;
		}

		protected internal override IQueryElement VisitSqlColumnReference(SqlColumn element)
		{
			if (element.Parent != null)
			{
				var sourceInQuery = _spotted.Contains(element.Parent);
				if (!_isSubQuery || sourceInQuery)
				{
					var contains = _querySources?.Contains(element.Parent) == true || _outerSources?.Contains(element.Parent) == true;
					if (!contains)
					{
						throw CreateErrorMessage(sourceInQuery, element);
					}
				}
			}

			return base.VisitSqlColumnReference(element);
		}

		protected override ISqlExpression VisitSqlColumnExpression(SqlColumn column, ISqlExpression expression)
		{
			var saveOuter = _outerSources;

			_outerSources = (_outerSources ?? Enumerable.Empty<ISqlTableSource>())
				.Concat(_querySources ?? Enumerable.Empty<ISqlTableSource>())
			.ToList();

			base.VisitSqlColumnExpression(column, expression);

			_outerSources = saveOuter;

			return expression;
		}

		protected internal override IQueryElement VisitSqlWhereClause(SqlWhereClause element)
		{
			var saveOuter = _outerSources;

			_outerSources = (_outerSources ?? Enumerable.Empty<ISqlTableSource>())
				.Concat(_querySources ?? Enumerable.Empty<ISqlTableSource>())
				.ToList();

			base.VisitSqlWhereClause(element);

			_outerSources = saveOuter;

			return element;
		}

		protected internal override IQueryElement VisitSqlMergeStatement(SqlMergeStatement element)
		{
			var saveSources = _querySources;

			_querySources = new();

			_querySources.Add(element.Source);
			_querySources.Add(element.Target);

			base.VisitSqlMergeStatement(element);

			_querySources = saveSources;

			return element;
		}

		protected internal override IQueryElement VisitSqlMultiInsertStatement(SqlMultiInsertStatement element)
		{
			var saveOuter = _outerSources;

			_outerSources = new();
			_outerSources.Add(element.Source);

			base.VisitSqlMultiInsertStatement(element);

			_outerSources = saveOuter;

			return element;
		}

		protected internal override IQueryElement VisitSqlConditionalInsertClause(SqlConditionalInsertClause element)
		{
			var saveOuter = _outerSources;

			_outerSources = new();

			if (element.Insert.Into != null)
				_outerSources.Add(element.Insert.Into);

			base.VisitSqlConditionalInsertClause(element);

			_outerSources = saveOuter;

			return element;
		}

		protected internal override IQueryElement VisitSqlInsertOrUpdateStatement(SqlInsertOrUpdateStatement element)
		{
			var saveOuter = _outerSources;

			_outerSources = new();

			if (element.Insert.Into != null)
				_outerSources.Add(element.Insert.Into);

			if (element.Update.Table != null)
				_outerSources.Add(element.Update.Table);

			base.VisitSqlInsertOrUpdateStatement(element);

			_outerSources = saveOuter;

			return element;
		}

		protected internal override IQueryElement VisitSqlUpdateStatement(SqlUpdateStatement element)
		{
			var saveOuter = _outerSources;

			_outerSources = new();
			_outerSources.Add(element.SelectQuery);

			if (element.Update.Table != null)
			{
				_outerSources.Add(element.Update.Table);
			}

			if (element.Update.TableSource != null)
			{
				_outerSources.Add(element.Update.TableSource.Source);
			}

			Visit(element.Tag);
			Visit(element.With);
			Visit(element.SelectQuery);

			_outerSources.AddRange(element.SelectQuery.From.Tables.Select(t => t.Source));

			Visit(element.Update);
			Visit(element.Output);

			VisitElements(element.SqlQueryExtensions, VisitMode.ReadOnly);

			_outerSources = saveOuter;

			return element;
		}

		protected internal override IQueryElement VisitSqlOutputClause(SqlOutputClause element)
		{
			var saveOuter = _outerSources;

			_outerSources = (_outerSources ?? Enumerable.Empty<ISqlTableSource>()).ToList();

			if (element.OutputTable != null)
				_outerSources.Add(element.OutputTable);
			
			base.VisitSqlOutputClause(element);

			_outerSources = saveOuter;

			return element;
		}

		protected internal override IQueryElement VisitSqlDeleteStatement(SqlDeleteStatement element)
		{
			var saveOuter = _outerSources;

			_outerSources = new();

			if (element.Table != null)
			{
				_outerSources.Add(element.Table);
			}

			base.VisitSqlDeleteStatement(element);

			_outerSources = saveOuter;

			return element;
		}

		protected internal override IQueryElement VisitSqlInsertStatement(SqlInsertStatement element)
		{
			var saveOuter = _outerSources;

			_outerSources = new();

			if (element.Insert.Into != null)
				_outerSources.Add(element.Insert.Into);

			_outerSources.Add(element.SelectQuery);

			base.VisitSqlInsertStatement(element);

			_outerSources = saveOuter;

			return element;
		}

		protected internal override IQueryElement VisitSqlTableSource(SqlTableSource element)
		{
			if (_querySources != null)
				_querySources.Add(element.Source);

			base.VisitSqlTableSource(element);

			return element;
		}

		protected internal override IQueryElement VisitSqlJoinedTable(SqlJoinedTable element)
		{
			if (element.JoinType == JoinType.CrossApply || element.JoinType == JoinType.OuterApply ||
			    element.JoinType == JoinType.RightApply || element.JoinType == JoinType.FullApply)
			{
				var saveOuterApply   = _outerSources;

				_outerSources = (_outerSources ?? Enumerable.Empty<ISqlTableSource>())
					.Concat(_querySources ?? Enumerable.Empty<ISqlTableSource>())
					.ToList();

				base.VisitSqlJoinedTable(element);

				_outerSources = saveOuterApply;

				return element;
			}

			Visit(element.Table);

			var saveOuter = _outerSources;

			_outerSources = (_outerSources ?? Enumerable.Empty<ISqlTableSource>())
				.Concat(_querySources ?? Enumerable.Empty<ISqlTableSource>())
				.ToList();

			Visit(element.Condition);

			_outerSources = saveOuter;

			VisitElements(element.SqlQueryExtensions, VisitMode.ReadOnly);

			return element;
		}
	}
}
