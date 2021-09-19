using LinqToDB.CodeGen.Metadata;
using LinqToDB.CodeGen.Model;
using LinqToDB.CodeGen.Schema;

namespace LinqToDB.CodeGen.DataModel
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
