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
		private readonly Dictionary<SelectQuery, HashSet<SqlColumn>> _usedColumnsByQuery = new();
		
		// Tracks which CTE fields are actually used
		private readonly Dictionary<CteClause, HashSet<string>> _usedCteFields = new();
		
		// Current CTE being processed
		private CteClause? _currentCte;
		
		// Current pass: true = collecting, false = removing
		private bool _isCollecting;

		private bool _inExpression;

		private SqlPredicate.Exists? _currentExistsPredicate;

		// Tracks queries that are part of set operators to avoid double-processing
		private readonly HashSet<SelectQuery> _setOperatorQueries = new();

		public SqlQueryColumnOptimizerVisitor() : base(VisitMode.Modify)
		{
		}

		public override void Cleanup()
		{
			base.Cleanup();
			_usedColumnsByQuery.Clear();
			_usedCteFields.Clear();
			_setOperatorQueries.Clear();

			_currentCte             = null;
			_currentExistsPredicate = null;
			_inExpression           = true;
			_isCollecting           = false;
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

		protected internal override IQueryElement VisitSqlSelectClause(SqlSelectClause element)
		{
			var prevInExpression = _inExpression;
			_inExpression = true;
			var result = base.VisitSqlSelectClause(element);
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

		protected internal override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
		{
			if (_isCollecting && _inExpression)
			{
				// Collect columns when query is used in expression context
				foreach (var column in selectQuery.Select.Columns)
				{
					MarkColumnUsed(column);
				}
			}

			// In modify pass, track set operator queries before visiting children
			if (!_isCollecting && selectQuery.HasSetOperators)
			{
				foreach (var setOp in selectQuery.SetOperators)
				{
					_setOperatorQueries.Add(setOp.SelectQuery);
					MarkSetOperatorQueriesRecursive(setOp.SelectQuery);
				}
			}

			var prevInExpression = _inExpression;
			_inExpression = false;

			// Visit all children
			var result = (SelectQuery)base.VisitSqlQuery(selectQuery);

			_inExpression = prevInExpression;

			// In modify pass, process this query's columns based on collected usage
			// BUT skip queries that are part of set operators - they will be processed
			// by their parent query to maintain alignment
			if (!_isCollecting && !_setOperatorQueries.Contains(result))
			{
				ProcessQueryColumns(result);
			}
			
			return result;
		}

		private void MarkSetOperatorQueriesRecursive(SelectQuery query)
		{
			if (query.HasSetOperators)
			{
				foreach (var setOp in query.SetOperators)
				{
					_setOperatorQueries.Add(setOp.SelectQuery);
					MarkSetOperatorQueriesRecursive(setOp.SelectQuery);
				}
			}
		}

		protected internal override IQueryElement VisitSqlColumnReference(SqlColumn element)
		{
			if (_isCollecting)
			{
				// Collecting pass: track that this column is being used
				MarkColumnUsed(element);
			}
			
			return base.VisitSqlColumnReference(element);
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
			
			return base.VisitSqlFieldReference(element);
		}

		protected internal override IQueryElement VisitSqlTableLikeSource(SqlTableLikeSource element)
		{
			var result = (SqlTableLikeSource)base.VisitSqlTableLikeSource(element);
			
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

		private void ProcessQueryColumns(SelectQuery selectQuery)
		{
			// Check if this query tree has non-UNION-ALL set operators
			var hasNonUnionAllSetOperators = HasNonUnionAllSetOperators(selectQuery);
			
			// Build list of column indices to keep
			var indicesToKeep = new List<int>();
			
			for (var i = 0; i < selectQuery.Select.Columns.Count; i++)
			{
				var column = selectQuery.Select.Columns[i];
				
				if (ShouldKeepColumn(column, selectQuery))
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
				if (selectQuery.HasSetOperators && !hasNonUnionAllSetOperators)
				{
					RemoveColumnsFromSetOperators(selectQuery.SetOperators, indicesToKeep);
				}
			}
			
			// Ensure non-empty SELECT
			if (selectQuery.Select.Columns.Count == 0 && _currentExistsPredicate?.SubQuery != selectQuery)
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

		private bool ShouldKeepColumn(SqlColumn column, SelectQuery selectQuery)
		{

			// Column is actually referenced somewhere
			if (IsColumnUsed(column))
				return true;

			// DISTINCT: keep all columns (they define uniqueness)
			if (selectQuery.Select.IsDistinct)
				return true;
			
			// Set operators (non UNION ALL): keep all for proper set semantics
			// This includes the query itself and all recursive set operators
			if (HasNonUnionAllSetOperators(selectQuery))
				return true;
			
			// Special case: queries with GROUP BY need at least one column
			if (!selectQuery.GroupBy.IsEmpty && selectQuery.Select.Columns.Count == 1)
				return true;
			
			// Aggregation or window functions are typically needed
			if (QueryHelper.ContainsAggregationOrWindowFunction(column.Expression))
			{
				// But only if the query itself is used
				return IsQueryUsed(selectQuery);
			}
			
			return false;
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
				
				// Visit the column's expression to mark dependent columns
				Visit(column.Expression);
			}
		}

		private bool IsColumnUsed(SqlColumn column)
		{
			if (column.Parent == null)
				return false;
			
			return _usedColumnsByQuery.TryGetValue(column.Parent, out var usedColumns) 
				&& usedColumns.Contains(column);
		}

		private bool IsQueryUsed(SelectQuery query)
		{
			// A query is "used" if any of its columns are used
			if (_usedColumnsByQuery.TryGetValue(query, out var usedColumns))
				return usedColumns.Count > 0;
			
			return false;
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
