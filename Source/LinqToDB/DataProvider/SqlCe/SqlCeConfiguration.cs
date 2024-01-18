using System;

namespace LinqToDB.DataProvider.SqlCe
{
	public static class SqlCeConfiguration
	{
		/// <summary>
		/// Enables force inlining of function parameters to support SQL CE 3.0.
		/// Default value: <c>false</c>.
		/// </summary>
		[Obsolete("Use SqlCeOptions.Default.BulkCopyType instead.")]
		public static bool InlineFunctionParameters
		{
			get => SqlCeOptions.Default.InlineFunctionParameters;
			set => SqlCeOptions.Default = new SqlCeOptions(SqlCeOptions.Default) { InlineFunctionParameters = value };
		}
	}
}
