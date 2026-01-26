using System.Collections.Generic;
using System.Linq;

using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.SqlQuery.Visitors
{
	/// <summary>
	/// Two-pass visitor that removes unused columns from SelectQuery.
	/// Pass 1: Collects all column references (read-only behavior)
	/// Pass 2: Removes unused columns (modify)
	/// </summary>
	public sealed class SqlQueryColumnOptimizerVisitor : QueryElementVisitor
	{
		// Maps each SelectQuery to its set of used columns
		readonly Dictionary<SelectQuery, HashSet<SqlColumn>> _usedColumnsByQuery = new();
		
		// Tracks which CTE fields are actually used
		readonly Dictionary<CteClause, HashSet<string>> _usedCteFields = new();
		
		// Current CTE being processed
		CteClause? _currentCte;
		
		// Current pass: true = collecting, false = removing
		bool _isCollecting;

		bool _inExpression;

		SqlPredicate.Exists? _currentExistsPredicate;
		SqlTableLikeSource?  _currentSqlTableLikeSource;

		public SqlQueryColumnOptimizerVisitor() : base(VisitMode.Modify)
		{
		}

		public override void Cleanup()
		{
			base.Cleanup();
			_usedColumnsByQuery.Clear();
			_usedCteFields.Clear();

			_currentCte                = null;
			_currentExistsPredicate    = null;
			_currentSqlTableLikeSource = null;
			_inExpression              = true;
			_isCollecting              = false;
		}

		/// <summary>
		/// Optimizes column usage in two passes:
		/// Pass 1: Collect all column references (no modifications)
		/// Pass 2: Remove unused columns (modify)
		/// </summary>
		public IQueryElement OptimizeColumns(IQueryElement root)
		{
			Cleanup();

			// Pass 1: Collect all column references (no modifications, just collection)
			_inExpression = true; // Start in expression context
			_isCollecting = true;
			Visit(root);

			// Pass 2: Remove unused columns (modify)
			_inExpression = true;
			_isCollecting = false;
			return Visit(root);
		}

		#region Core Visitor Methods

		protected internal override IQueryElement VisitCteClause(CteClause element)
		{
			var saveCte = _currentCte;
			_currentCte = element;

			var prevInExpression = _inExpression;
			_inExpression = false;

			List<SqlColumn>? originalColumns = null;
			
			// In modify pass, store original columns for tracking
			if (!_isCollecting && element.Body != null)
			{
				originalColumns = element.Body.Select.Columns.ToList();
			}
			
			// Visit the CTE body
			var result = (CteClause)base.VisitCteClause(element);
			
			// In modify pass, synchronize fields based on what columns remain
			if (!_isCollecting && originalColumns != null && result.Body != null)
			{
				SynchronizeCteFields(result, originalColumns, result.Body.Select.Columns);
			}
			
			_currentCte = saveCte;
			_inExpression = prevInExpression;

			return result;
		}

		protected internal override IQueryElement VisitSqlOrderByClause(SqlOrderByClause element)
		{
			var prevInExpression = _inExpression;
			_inExpression = true;
			var result = base.VisitSqlOrderByClause(element);
			_inExpression = prevInExpression;
			return result;
		}

		// It will handle predicates and their expressions
		protected internal override IQueryElement VisitSqlSearchCondition(SqlSearchCondition element)
		{
			var prevInExpression = _inExpression;
			_inExpression = true;
			var result = base.VisitSqlSearchCondition(element);
			_inExpression = prevInExpression;
			return result;
		}

		protected internal override IQueryElement VisitSqlGroupByClause(SqlGroupByClause element)
		{
			var prevInExpression = _inExpression;
			_inExpression = true;
			var result = base.VisitSqlGroupByClause(element);
			_inExpression = prevInExpression;
			return result;
		}

		protected override ISqlExpression VisitSqlColumnExpression(SqlColumn column, ISqlExpression expression)
		{
			return expression;
		}

		protected internal override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
		{
			if (_isCollecting )
			{
				if (_inExpression || selectQuery.Select.IsDistinct || selectQuery.HasSetOperators && HasNonUnionAllSetOperators(selectQuery))
				{
					// Collect columns when query is used in expression context or has non-UNION-ALL set operators
					foreach (var column in selectQuery.Select.Columns)
					{
						MarkColumnUsed(column);
					}
				}
				else if (QueryHelper.IsAggregationQuery(selectQuery))
				{
					if (selectQuery.Select.Columns.Count > 0)
					{
						// For aggregation queries, mark the first column as used to ensure at least one is kept
						MarkColumnUsed(selectQuery.Select.Columns[0]);
					}
				}
			}

			// In modify pass, process this query's columns BEFORE visiting children
			// since all usage info was already collected in Phase 1
			if (!_isCollecting)
			{
				ProcessQueryColumns(selectQuery);
			}

			var prevInExpression = _inExpression;
			_inExpression = false;

			// Visit all children
			var result = (SelectQuery)base.VisitSqlQuery(selectQuery);

			_inExpression = prevInExpression;
			
			return result;
		}

		protected internal override IQueryElement VisitSqlColumnReference(SqlColumn element)
		{
			if (_isCollecting)
			{
				// Collecting pass: track that this column is being used
				MarkColumnUsed(element);
			}
			
			// In Phase 2, don't visit column expressions - usage already collected
			return _isCollecting ? base.VisitSqlColumnReference(element) : element;
		}

		protected internal override IQueryElement VisitSqlFieldReference(SqlField element)
		{
			if (_isCollecting)
			{
				// Handle CTE field references
				if (element.Table is SqlCteTable cte)
				{
					// Mark this CTE field as used
					if (!_usedCteFields.TryGetValue(cte.Cte!, out var usedFields))
					{
						usedFields = new HashSet<string>();
						_usedCteFields[cte.Cte!] = usedFields;
					}

					usedFields.Add(element.PhysicalName);
					
					// Find and mark the corresponding column
					for (var i = 0; i < cte.Cte!.Fields.Count; i++)
					{
						if (cte.Cte.Fields[i].Name == element.PhysicalName)
						{
							if (i < cte.Cte.Body!.Select.Columns.Count)
							{
								MarkColumnUsed(cte.Cte.Body!.Select.Columns[i]);
							}

							break;
						}
					}
				}
			}
			
			// In Phase 2, don't visit field references - usage already collected
			return _isCollecting ? base.VisitSqlFieldReference(element) : element;
		}

		protected internal override IQueryElement VisitSqlTableLikeSource(SqlTableLikeSource element)
		{
			var saveTableLikeSource = _currentSqlTableLikeSource;
			_currentSqlTableLikeSource = element;

			var result = (SqlTableLikeSource)base.VisitSqlTableLikeSource(element);

			_currentSqlTableLikeSource = saveTableLikeSource;

			if (!_isCollecting && result.SourceEnumerable != null)
				SynchronizeEnumerableFields(result);
			
			return result;
		}

		protected internal override IQueryElement VisitExistsPredicate(SqlPredicate.Exists predicate)
		{
			var saveExists = _currentExistsPredicate;
			_currentExistsPredicate = predicate;
			
			var result = (SqlPredicate.Exists)base.VisitExistsPredicate(predicate);

			_currentExistsPredicate = saveExists;
			return result;
		}

		#endregion

		#region Column Processing Logic

		void ProcessQueryColumns(SelectQuery selectQuery)
		{
			// Build list of column indices to keep
			var indicesToKeep = new List<int>();
			
			for (var i = 0; i < selectQuery.Select.Columns.Count; i++)
			{
				var column = selectQuery.Select.Columns[i];
				
				if (IsColumnUsed(column))
				{
					indicesToKeep.Add(i);
				}
			}
			
			// Ensure at least one column remains if we have set operators
			if (selectQuery.HasSetOperators && indicesToKeep.Count == 0)
			{
				// Keep the first column to maintain set operator alignment
				indicesToKeep.Add(0);
			}
			
			// Remove columns not in the keep list
			if (indicesToKeep.Count < selectQuery.Select.Columns.Count)
			{
				// Build new column list
				var newColumns = new List<SqlColumn>();
				foreach (var index in indicesToKeep)
				{
					newColumns.Add(selectQuery.Select.Columns[index]);
				}
				
				// Replace columns
				selectQuery.Select.Columns.Clear();
				foreach (var column in newColumns)
				{
					selectQuery.Select.Columns.Add(column);
				}
				
				// For set operators, only remove columns if all are UNION ALL
				// For UNION/INTERSECT/EXCEPT, columns must stay aligned
				if (selectQuery.HasSetOperators)
				{
					RemoveColumnsFromSetOperators(selectQuery.SetOperators, indicesToKeep);
				}
			}
			
			// Ensure non-empty SELECT
			if (selectQuery.Select.Columns.Count == 0 && AllowEmptyColumns(selectQuery))
			{
				AddDummyColumn(selectQuery);
				
				// If we added a dummy column and have set operators, add dummy to them too
				if (selectQuery.HasSetOperators)
				{
					foreach (var setOp in selectQuery.SetOperators)
					{
						if (setOp.SelectQuery.Select.Columns.Count == 0)
						{
							AddDummyColumn(setOp.SelectQuery);
						}
					}
				}
			}
		}

		bool AllowEmptyColumns(SelectQuery selectQuery)
		{
			return _currentExistsPredicate?.SubQuery != selectQuery 
			       && _currentSqlTableLikeSource?.SourceQuery != selectQuery;
		}

		private bool HasNonUnionAllSetOperators(SelectQuery selectQuery)
		{
			if (!selectQuery.HasSetOperators)
				return false;
			
			// Check this level
			if (selectQuery.SetOperators.Any(so => so.Operation != SetOperation.UnionAll))
				return true;
			
			// Check recursively in all set operator branches
			foreach (var setOp in selectQuery.SetOperators)
			{
				if (HasNonUnionAllSetOperators(setOp.SelectQuery))
					return true;
			}
			
			return false;
		}

		private void RemoveColumnsFromSetOperators(List<SqlSetOperator> setOperators, List<int> indicesToKeep)
		{
			foreach (var setOp in setOperators)
			{
				var setQuery = setOp.SelectQuery;
				
				// Build new column list keeping only columns at the specified indices
				var newColumns = new List<SqlColumn>();
				for (var i = 0; i < setQuery.Select.Columns.Count; i++)
				{
					if (indicesToKeep.Contains(i))
					{
						newColumns.Add(setQuery.Select.Columns[i]);
					}
				}
				
				// Ensure we don't remove the last column from set operator query
				if (newColumns.Count == 0 && setQuery.Select.Columns.Count > 0)
				{
					// Keep the first column as fallback
					newColumns.Add(setQuery.Select.Columns[0]);
				}
				
				// Replace columns
				setQuery.Select.Columns.Clear();
				foreach (var column in newColumns)
				{
					setQuery.Select.Columns.Add(column);
				}
				
				// Recursively handle nested set operators
				// Only if they are also all UNION ALL
				if (setQuery.HasSetOperators && !HasNonUnionAllSetOperators(setQuery))
				{
					RemoveColumnsFromSetOperators(setQuery.SetOperators, indicesToKeep);
				}
			}
		}

		private void SynchronizeCteFields(CteClause cte, List<SqlColumn> originalColumns, IReadOnlyList<SqlColumn> currentColumns)
		{
			if (cte.Fields.Count == 0 || originalColumns.Count == 0)
				return;
			
			var hasUsedFields = _usedCteFields.TryGetValue(cte, out var usedFieldNames);
			
			// Build a set of columns that still exist
			var remainingColumns = new HashSet<SqlColumn>(
				currentColumns, 
				Utils.ObjectReferenceEqualityComparer<SqlColumn>.Default
			);
			
			// Determine which fields to keep based on their corresponding columns
			var fieldsToKeep = new List<SqlField>();
			
			for (var i = 0; i < cte.Fields.Count && i < originalColumns.Count; i++)
			{
				var field = cte.Fields[i];
				var originalColumn = originalColumns[i];
				
				// Check if the column still exists
				if (remainingColumns.Contains(originalColumn))
				{
					// Check if the field is actually used
					if (!hasUsedFields || usedFieldNames!.Contains(field.Name))
					{
						fieldsToKeep.Add(field);
					}
				}
			}
			
			// Ensure at least one field remains
			if (fieldsToKeep.Count == 0 && currentColumns.Count > 0)
			{
				// Try to keep first available field
				for (var i = 0; i < cte.Fields.Count && i < originalColumns.Count; i++)
				{
					if (remainingColumns.Contains(originalColumns[i]))
					{
						fieldsToKeep.Add(cte.Fields[i]);
						break;
					}
				}
				
				// If still no field, create a dummy one
				if (fieldsToKeep.Count == 0)
				{
					fieldsToKeep.Add(new SqlField(new DbDataType(typeof(int)), "c1", false));
				}
			}
			
			// Replace fields
			cte.Fields.Clear();
			foreach (var field in fieldsToKeep)
			{
				cte.Fields.Add(field);
			}
		}

		private void MarkColumnUsed(SqlColumn column)
		{
			if (column.Parent == null)
				return;
			
			// Get or create the set for this query
			if (!_usedColumnsByQuery.TryGetValue(column.Parent, out var usedColumns))
			{
				usedColumns = new HashSet<SqlColumn>(Utils.ObjectReferenceEqualityComparer<SqlColumn>.Default);
				_usedColumnsByQuery[column.Parent] = usedColumns;
			}
			
			if (usedColumns.Add(column))
			{
				// For set operators, mark corresponding columns by position
				if (column.Parent.HasSetOperators)
				{
					var idx = column.Parent.Select.Columns.IndexOf(column);
					if (idx >= 0)
					{
						foreach (var setOp in column.Parent.SetOperators)
						{
							if (idx < setOp.SelectQuery.Select.Columns.Count)
							{
								MarkColumnUsed(setOp.SelectQuery.Select.Columns[idx]);
							}
						}
					}
				}
				
				// Visit the column's expression to find dependent columns
				// Only during Phase 1 collection
				var saveInExpression = _inExpression;
				_inExpression = true;

				Visit(column.Expression);

				_inExpression = saveInExpression;
			}
		}

		private bool IsColumnUsed(SqlColumn column)
		{
			if (column.Parent == null)
				return false;
			
			return _usedColumnsByQuery.TryGetValue(column.Parent, out var usedColumns) 
				&& usedColumns.Contains(column);
		}

		#endregion

		#region Helper Methods

		private void AddDummyColumn(SelectQuery selectQuery)
		{
			var needsName = selectQuery.From.Tables.Count == 0 
				|| !selectQuery.GroupBy.IsEmpty;
			
			selectQuery.Select.AddNew(
				new SqlValue(1), 
				alias: needsName ? "c1" : null
			);
		}

		private void SynchronizeEnumerableFields(SqlTableLikeSource tableSource)
		{
			var enumSource = tableSource.SourceEnumerable!;
			
			// Remove unreferenced fields
			for (var i = enumSource.Fields.Count - 1; i >= 0; i--)
			{
				var field = enumSource.Fields[i];
				if (tableSource.SourceFields.All(sf => sf.BasedOn != field))
				{
					enumSource.RemoveField(i);
				}
			}
			
			// Reorder if all have BasedOn
			if (tableSource.SourceFields.All(sf => sf.BasedOn != null))
			{
				var orderedFields = tableSource.SourceFields
					.OrderBy(f => enumSource.Fields.IndexOf(f.BasedOn!))
					.ToList();
				
				tableSource.SourceFields.Clear();
				foreach (var field in orderedFields)
					tableSource.SourceFields.Add(field);
			}
		}

		#endregion
	}
}
