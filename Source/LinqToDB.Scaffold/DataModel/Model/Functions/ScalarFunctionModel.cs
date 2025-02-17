using LinqToDB.CodeModel;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Metadata;

namespace LinqToDB.DataModel
{
	/// <summary>
	/// Scalar function descriptor.
	/// </summary>
	public sealed class ScalarFunctionModel : ScalarFunctionModelBase
	{
		public ScalarFunctionModel(SqlObjectName name, MethodModel method, FunctionMetadata metadata)
			: base(name, method, metadata)
		{
		}

		/// <summary>
		/// Gets or sets return value type.
		/// Either <see cref="Return"/> or <see cref="ReturnTuple"/> must be set, but not both.
		/// </summary>
		public IType?      Return      { get; set; }
		/// <summary>
		/// Gets or sets return value descriptor, when function returns tuple/row.
		/// Either <see cref="Return"/> or <see cref="ReturnTuple"/> must be set, but not both.
		/// </summary>
		public TupleModel? ReturnTuple { get; set; }
	}
}
