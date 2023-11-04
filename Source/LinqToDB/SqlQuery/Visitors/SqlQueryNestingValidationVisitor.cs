using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.SqlQuery.Visitors
{
	public class SqlQueryNestingValidationVisitor : QueryElementVisitor
	{
		readonly bool                         _isSubQuery;
		readonly SelectQuery                  _forQuery;
		readonly Stack<List<ISqlTableSource>> _visibleSources = new ();
		readonly HashSet<ISqlTableSource>     _spotted        = new ();
		SelectQuery?                          _currentQuery;

		public SqlQueryNestingValidationVisitor(bool isSubQuery, SelectQuery forQuery) : base(VisitMode.ReadOnly)
		{
			_isSubQuery    = isSubQuery;
			_forQuery = forQuery;
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
				messageString += $"\nQuery:\n{_forQuery.ToDebugString()}";
			}
			else
			{
				messageString += "has wrong nesting.\n";

				if (_forQuery == _currentQuery)
				{
					messageString += "-----------------------------------------\n";
					messageString += $"Query:\n{_forQuery.ToDebugString()}";
				}
				else
				{
					messageString += "-----------------------------------------\n";

					messageString += $"SubQuery:\n{_currentQuery!.ToDebugString()}\n";
					messageString += "-----------------------------------------\n";
					messageString += $"In Query:\n{_forQuery.ToDebugString()}";
				}
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
	}
}
