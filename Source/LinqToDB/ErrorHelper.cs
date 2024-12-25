namespace LinqToDB
{
	internal static class ErrorHelper
	{
		public const string Error_Correlated_Subqueries          = "Provider does not support correlated subqueries.";
		public const string Error_Correlated_Subqueries_Level    = "Provider does not support correlated subqueries in more than {0} level.";
		public const string Error_OUTER_Joins                    = "Provider does not support CROSS/OUTER/LATERAL joins.";
		public const string Error_Subquery_in_Column             = "Provider does not support columns with subqueries.";
		public const string Error_Take_in_Subquery               = "Provider does not support Take value in subquery.";
		public const string Error_Take_in_Correlated_Subquery    = "Provider does not support Take value in correlated subquery.";
		public const string Error_Join_Without_Condition         = "Provider does not support JOIN without condition.";
		public const string Error_Join_ParentReference_Condition = "Provider does not support subqueries with JOIN which has reference to parent table in condition.";
		public const string Error_Skip_in_Subquery               = "Provider does not support Skip value in subquery.";
		public const string Error_OrderBy_in_Subquery            = "Provider does not support ORDER BY in subquery.";
		public const string Error_Take_in_Derived                = "Provider does not support Take value in derived table.";
		public const string Error_OrderBy_in_Derived             = "Provider does not support ORDER BY in derived table.";
		public const string Error_MutiTable_Insert               = "Provider does not support multi-table insert.";
		public const string Error_SqlRow_in_Update               = "Provider does not support SqlRow on the left-hand side of an UPDATE SET.";
		public const string Error_SqlRow_in_Update_Value         = "Provider does not support SqlRow literal on the right-hand side of an UPDATE SET.";
		public const string Error_RowNumber                      = "Provider does not support ROW_NUMBER function.";
		public const string Error_OrderByRequiredForIndexing     = "For retrieving index of row, specify OrderBy part";

		public const string Error_WindowFunctionsInSearchCondition = "Window functions cannot be used in search condition.";

		public const string Error_GroupGuard =
							"""
							You should explicitly specify selected fields for server-side GroupBy() call or add AsEnumerable() call before GroupBy() to perform client-side grouping.
							Set Configuration.Linq.GuardGrouping = false to disable this check.
							Additionally this guard exception can be disabled by extension GroupBy(...).DisableGuard().
							NOTE! By disabling this guard you accept Eager Loading for grouping query.
							""";

		internal static class Oracle
		{
			public const string Error_ColumnSubqueryShouldNotContainParentIsNotNull = "Column expression should not contain parent's IS NOT NULL condition.";
		}

		internal static class Sybase
		{
			public const string Error_JoinToDerivedTableWithTakeInvalid = "Feature not supported by database: database has issue with JOIN to limited recordset.";
			public const string Error_UpdateWithTopOrderBy              = "Feature not supported by database: UPDATE statement with the TOP + ORDER BY clause.";
			public const string Error_DeleteWithTopOrderBy              = "Feature not supported by database: DELETE statement with the TOP + ORDER BY clause.";
			public const string Error_UpdateWithSkip                    = "Feature not supported by database: UPDATE statement with the Skip.";
			public const string Error_DeleteWithSkip                    = "Feature not supported by database: DELETE statement with the Skip.";
		}

		internal static class ClickHouse
		{
			public const string Error_CorrelatedDelete = "Feature not supported by database: Correlated DELETE";
			public const string Error_CorrelatedUpdate = "Feature not supported by database: Correlated UPDATE";
		}

		internal static class MySql
		{
			public const string Error_SkipInUpdate = "Feature not supported by database: Skip in UPDATE.";
		}

	}
}
