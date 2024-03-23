namespace LinqToDB
{
	internal static class ErrorHelper
	{
		public const string Error_Correlated_Subqueries = "Provider does not support Correlated subqueries.";
		public const string Error_OUTER_Joins           = "Provider does not support CROSS/OUTER/LATERAL joins.";
		public const string Error_Take_in_Subquery      = "Provider does not support Take value in subquery.";
		public const string Error_Skip_in_Subquery      = "Provider does not support Skip value in subquery.";
	}
}
