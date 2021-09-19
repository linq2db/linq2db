using LinqToDB.CodeGen.Metadata;
using LinqToDB.CodeGen.Model;
using LinqToDB.CodeGen.Schema;

namespace LinqToDB.CodeGen.DataModel
{
	public class ScalarFunctionModel : ScalarFunctionModelBase
	{
		public ScalarFunctionModel(ObjectName name, MethodModel method, FunctionMetadata metadata)
			: base(name, method, metadata)
		{
		}

		public IType? Return { get; set; }

		public TupleModel? ReturnTuple { get; set; }
	}
}
