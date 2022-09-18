using System.Reflection;

namespace LinqToDB.DataModel
{
	using Metadata;
	using SqlQuery;

	/// <summary>
	/// Table function model.
	/// </summary>
	public sealed class TableFunctionModel : TableFunctionModelBase
	{
		public TableFunctionModel(
			SqlObjectName         name,
			MethodModel           method,
			TableFunctionMetadata metadata,
			string                methodInfoFieldName)
			: base(name, method)
		{
			Metadata            = metadata;
			MethodInfoFieldName = methodInfoFieldName;
		}

		/// <summary>
		/// Gets or sets name of private field to store <see cref="MethodInfo"/> instance of generated function mapping method.
		/// </summary>
		public string                MethodInfoFieldName { get; set; }
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
