using LinqToDB.CodeGen.CodeGeneration;
using LinqToDB.CodeGen.ContextModel;
using LinqToDB.CodeGen.Schema;

namespace LinqToDB.CodeGen.Model
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
