using LinqToDB.CodeModel;
using LinqToDB.Metadata;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataModel
{
	/// <summary>
	/// Aggregate function model.
	/// </summary>
	public sealed class AggregateFunctionModel : ScalarFunctionModelBase
	{
		public AggregateFunctionModel(SqlObjectName name, MethodModel method, FunctionMetadata metadata, IType returnType)
			: base(name, method, metadata)
		{
			ReturnType = returnType;
		}

		/// <summary>
		/// Gets or sets function return type.
		/// </summary>
		public IType ReturnType { get; set; }
	}
}
