using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using LinqToDB.SqlQuery.Visitors;

namespace LinqToDB.SqlQuery
{
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

			protected bool Equals(QueryNesting other)
			{
				return TableSource == other.TableSource;
			}

			public ISqlExpression UpdateNesting(ISqlExpression element)
			{
				if (Parent is { TableSource: SelectQuery parentSelectQuery })
				{
					return UpdateNestingInQuery(parentSelectQuery, element);
				}

				if (Parent is { TableSource: SqlTableLikeSource { SourceQuery: not null } tableLike})
				{
					var columnCount = tableLike.SourceQuery.Select.Columns.Count;
					var newElement  = UpdateNestingInQuery(tableLike.SourceQuery, element);
						
					if (newElement is not SqlColumn column)
						throw new InvalidOperationException("Invalid AST");

					if (columnCount == tableLike.SourceQuery.Select.Columns.Count)
					{
						var columnIndex = tableLike.SourceQuery.Select.Columns.IndexOf(column);
						if (columnIndex < 0)
							throw new InvalidOperationException("Invalid AST");
						return tableLike.SourceFields[columnIndex];
					}

					SqlField? field            = null;
					var       columnDescriptor = QueryHelper.GetColumnDescriptor(newElement);
					if (columnDescriptor != null)
					{
						field = new SqlField(columnDescriptor) { Name = "field" };
					}
					else
					{
						var dbDataType = QueryHelper.GetDbDataTypeWithoutSchema(newElement);
						field = new SqlField(dbDataType, "field", true);
					}

					var nullability = NullabilityContext.GetContext(tableLike.SourceQuery);
					var canBeNull   = nullability.CanBeNull(newElement);
					field.CanBeNull = canBeNull;

					tableLike.AddField(field);

					return field;
				}

				return element;
			}

			public ISqlExpression UpdateNestingInQuery(SelectQuery selectQuery, ISqlExpression element)
			{
				if (Parent is { TableSource: SelectQuery { HasSetOperators: true } parentSelectQuery })
				{
					if (parentSelectQuery.SetOperators.Any(so => so.SelectQuery == selectQuery))
					{
						var saveCount      = selectQuery.Select.Columns.Count;
						var setColumnIndex = selectQuery.Select.Add(element);

						// Column found, just return column from parent query.
						if (saveCount == selectQuery.Select.Columns.Count)
							return parentSelectQuery.Select.Columns[setColumnIndex];

						var dbDataType   = QueryHelper.GetDbDataTypeWithoutSchema(element);
						var resultColumn = parentSelectQuery.Select.AddNewColumn(new SqlValue(dbDataType, null));

						foreach (var so in parentSelectQuery.SetOperators)
						{
							if (so.SelectQuery != selectQuery)
							{
								so.SelectQuery.Select.AddNew(new SqlValue(dbDataType, null));
							}
						}

						return resultColumn.Expression;
					}
				}

				return selectQuery.Select.AddColumn(element);
			}

			public override bool Equals(object? obj)
			{
				if (obj is null)
				{
					return false;
				}

				if (ReferenceEquals(this, obj))
				{
					return true;
				}

				if (obj.GetType() != GetType())
				{
					return false;
				}

				return Equals((QueryNesting)obj);
			}

			public override int GetHashCode()
			{
				return TableSource.GetHashCode();
			}
		}

		QueryNesting? _parentQueryNesting;

		public bool HasSelectQuery { get; private set; }

		public SqlQueryColumnNestingCorrector() : base(VisitMode.Modify, null)
		{
		}

		public override void Cleanup()
		{
			base.Cleanup();

			_parentQueryNesting = null;
		}

		public IQueryElement CorrectColumnNesting(IQueryElement element)
		{
			Cleanup();
			HasSelectQuery = false;

			#if DEBUG
			var beforeText = element.ToDebugString();
			#endif

			var result = Visit(element);
			return result;
		}

		ISqlExpression ProcessNesting(ISqlTableSource elementSource, ISqlExpression element)
		{
			if (_parentQueryNesting == null)
				return element;

			var current = _parentQueryNesting;
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
				if (element.Table is SqlTableLikeSource tableLikeSource && tableLikeSource.SourceQuery != null)
				{
					var current = _parentQueryNesting?.Parent;
					while (current != null)
					{
						if (current.TableSource == element.Table)
						{
							var fieldIndex = tableLikeSource.SourceFields.IndexOf(element);
							if (fieldIndex < 0)
								throw new InvalidOperationException("Invalid field field for SqlTableLikeSource");
							var fieldExpression = tableLikeSource.SourceQuery.Select.Columns[fieldIndex].Expression;
							return Visit(fieldExpression);
						}

						current = current.Parent;
					}
				}

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
			HasSelectQuery = true;

			var saveQueryNesting = _parentQueryNesting;

			_parentQueryNesting = new QueryNesting(saveQueryNesting, selectQuery);

			var newQuery = base.VisitSqlQuery(selectQuery);

			_parentQueryNesting = saveQueryNesting;

			return newQuery;
		}

		protected override IQueryElement VisitSqlTableSource(SqlTableSource element)
		{
			_ = new QueryNesting(_parentQueryNesting, element.Source);

			var newElement = base.VisitSqlTableSource(element);

			return newElement;
		}

		protected override IQueryElement VisitSqlTableLikeSource(SqlTableLikeSource element)
		{
			var saveQueryNesting = _parentQueryNesting;
			_parentQueryNesting = new QueryNesting(saveQueryNesting, element);

			var newElement = base.VisitSqlTableLikeSource(element);

			if (saveQueryNesting != null)
				_parentQueryNesting = saveQueryNesting;

			return newElement;
		}
	}
}
