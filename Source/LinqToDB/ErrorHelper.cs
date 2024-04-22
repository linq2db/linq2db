namespace LinqToDB
{
	internal static class ErrorHelper
	{
		public const string Error_Correlated_Subqueries          = "Provider does not support Correlated subqueries.";
		public const string Error_OUTER_Joins                    = "Provider does not support CROSS/OUTER/LATERAL joins.";
		public const string Error_Take_in_Subquery               = "Provider does not support Take value in subquery.";
		public const string Error_Take_in_Correlated_Subquery    = "Provider does not support Take value in correlated subquery.";
		public const string Error_Join_Without_Condition         = "Provider does not support JOIN without condition.";
		public const string Error_Join_ParentReference_Condition = "Provider does not support subqueries with JOIN which has reference to parent table in condition.";
		public const string Error_Skip_in_Subquery               = "Provider does not support Skip value in subquery.";
		public const string Error_OrderBy_in_Subquery            = "Provider does not support ORDER BY in subquery.";
		public const string Error_OrderBy_in_Derived             = "Provider does not support ORDER BY in derived tabley.";
	}
}
