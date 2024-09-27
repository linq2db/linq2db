﻿namespace LinqToDB
{
	internal static class ErrorHelper
	{
		public const string Error_Correlated_Subqueries          = "Provider does not support correlated subqueries.";
		public const string Error_Correlated_Subqueries_Level    = "Provider does not support correlated subqueries in more than {0} level.";
		public const string Error_OUTER_Joins                    = "Provider does not support CROSS/OUTER/LATERAL joins.";
		public const string Error_Squbquery_in_Column            = "Provider does not support columns with subqueries.";
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

		// Oracle
		public const string Error_ColumnSubqueryShouldNotContainParentIsNotNull = "Column expression should not contain parent's IS NOT NULL condition.";

		// Sybase
		public const string Error_JoinToDerivedTableWithTakeInvalid = "Provider has issue with JOIN to limited recordset.";

	}
}
