using LinqToDB.CodeGen.CodeGeneration;
using LinqToDB.CodeGen.ContextModel;
using LinqToDB.CodeGen.Schema;

namespace LinqToDB.CodeGen.Model
{
	public class AggregateFunctionModel : ScalarFunctionModelBase
	{
		public AggregateFunctionModel(ObjectName name, MethodModel method, FunctionMetadata metadata, IType returnType)
			: base(name, method, metadata)
		{
			ReturnType = returnType;
		}

		public IType ReturnType { get; set; }
	}
}
