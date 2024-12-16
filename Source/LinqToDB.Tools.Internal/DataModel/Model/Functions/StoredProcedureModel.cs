using System.Collections.Generic;
using LinqToDB.Schema;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataModel
{
	/// <summary>
	/// Stored procedure descriptor.
	/// </summary>
	public sealed class StoredProcedureModel : TableFunctionModelBase
	{
		public StoredProcedureModel(SqlObjectName name, MethodModel method)
			: base(name, method)
		{
		}

		/// <summary>
		/// Gets or sets return parameter descriptor.
		/// </summary>
		public FunctionParameterModel? Return  { get; set; }
		/// <summary>
		/// Gets or sets record types for returned result set(s).
		/// </summary>
		public List<FunctionResult>    Results { get; set; } = new();
	}
}
