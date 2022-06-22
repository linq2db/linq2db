using System.Collections.Generic;

namespace LinqToDB.DataModel
{
	/// <summary>
	/// This model defines results wrapper class for async stored procedure mapper in cases when procedure returns multiple values:
	/// <list type="bullet">
	/// <item>result set table and one or more return, out or ref parameter</item>
	/// <item>rowcount and one or more return, out or ref parameter</item>
	/// <item>two or more return, out or ref parameters</item>
	/// </list>
	/// </summary>
	public sealed class AsyncProcedureResult
	{
		public AsyncProcedureResult(ClassModel @class, PropertyModel mainResult)
		{
			Class      = @class;
			MainResult = mainResult;
		}

		/// <summary>
		/// Gets or sets class descriptor.
		/// </summary>
		public ClassModel                                        Class               { get; set; }

		/// <summary>
		/// Property descriptor for main procedure result: rowcount or recordset.
		/// </summary>
		public PropertyModel                                     MainResult          { get; set; }

		/// <summary>
		/// Returning parameters property descriptors (out, ref or return parameters).
		/// </summary>
		public Dictionary<FunctionParameterModel, PropertyModel> ParameterProperties { get; } = new();
	}
}
