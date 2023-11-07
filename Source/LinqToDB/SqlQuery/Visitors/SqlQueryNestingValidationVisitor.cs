using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.SqlQuery.Visitors
{
	public class SqlQueryNestingValidationVisitor : QueryElementVisitor
	{
		readonly bool                         _isSubQuery;
		readonly IQueryElement                _forStatement;
		readonly Stack<List<ISqlTableSource>> _visibleSources = new ();
		readonly HashSet<ISqlTableSource>     _spotted        = new ();
		SelectQuery?                          _currentQuery;

		public SqlQueryNestingValidationVisitor(bool isSubQuery, IQueryElement forStatement) : base(VisitMode.ReadOnly)
		{
			_isSubQuery   = isSubQuery;
			_forStatement = forStatement;
		}

		protected override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
		{
			var saveQuery = _currentQuery;
			_currentQuery = selectQuery;

			_visibleSources.Push(new List<ISqlTableSource>());

			base.VisitSqlQuery(selectQuery);

			_visibleSources.Pop();

			_currentQuery = saveQuery;

			return selectQuery;
		}

		protected Exception CreateErrorMessage(bool sourceInQuery, IQueryElement element)
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

		protected override IQueryElement VisitSqlFieldReference(SqlField element)
		{
			if (element.Table != null)
			{
				var sourceInQuery = _spotted.Contains(element.Table);
				if (!_isSubQuery || sourceInQuery)
				{
					var contains = _visibleSources.SelectMany(s => s).Contains(element.Table);
					if (!contains)
					{
						throw CreateErrorMessage(sourceInQuery, element);
					}
				}
			}

			return base.VisitSqlFieldReference(element);
		}

		protected override IQueryElement VisitSqlColumnReference(SqlColumn element)
		{
			if (element.Parent != null)
			{
				var sourceInQuery = _spotted.Contains(element.Parent);
				if (!_isSubQuery || sourceInQuery)
				{
					var contains = _visibleSources.SelectMany(s => s).Contains(element.Parent);
					if (!contains)
					{
						throw CreateErrorMessage(sourceInQuery, element);
					}
				}
			}

			return base.VisitSqlColumnReference(element);
		}

		protected override IQueryElement VisitSqlTableSource(SqlTableSource element)
		{
			_spotted.Add(element.Source);
			_visibleSources.Peek().Add(element.Source);

			return base.VisitSqlTableSource(element);
		}


		protected override IQueryElement VisitSqlUpdateStatement(SqlUpdateStatement element)
		{
			var tableSources = new List<ISqlTableSource>();
			_visibleSources.Push(tableSources);

			if (element.Update.Table != null)
			{
				tableSources.Add(element.Update.Table);
			}

			if (element.Update.TableSource != null)
			{
				tableSources.Add(element.Update.TableSource.Source);
			}

			tableSources.Add(element.SelectQuery);
			tableSources.AddRange(element.SelectQuery.From.Tables.Select(t => t.Source));

			var newElement = base.VisitSqlUpdateStatement(element);

			_visibleSources.Pop();

			return newElement;
		}

		protected override IQueryElement VisitSqlInsertStatement(SqlInsertStatement element)
		{
			var tableSources = new List<ISqlTableSource>();
			_visibleSources.Push(tableSources);

			if (element.Insert.Into != null)
				tableSources.Add(element.Insert.Into);

			tableSources.Add(element.SelectQuery);

			var newElement = base.VisitSqlInsertStatement(element);

			_visibleSources.Pop();

			return newElement;
		}
	}
}
