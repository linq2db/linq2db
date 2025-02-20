using LinqToDB.Internal.SqlQuery;
using LinqToDB.Metadata;

namespace LinqToDB.DataModel
{
	/// <summary>
	/// Table function model.
	/// </summary>
	public sealed class TableFunctionModel : TableFunctionModelBase
	{
		public TableFunctionModel(
			SqlObjectName         name,
			MethodModel           method,
			TableFunctionMetadata metadata)
			: base(name, method)
		{
			Metadata = metadata;
		}

		/// <summary>
		/// Gets or sets table function metadata descriptor.
		/// </summary>
		public TableFunctionMetadata Metadata            { get; set; }
		/// <summary>
		/// Gets or sets table function result record descriptor.
		/// </summary>
		public FunctionResult?       Result              { get; set; }
	}
}
