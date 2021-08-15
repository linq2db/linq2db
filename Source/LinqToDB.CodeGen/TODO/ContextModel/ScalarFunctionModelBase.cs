using LinqToDB.CodeGen.CodeGeneration;
using LinqToDB.CodeGen.Model;
using LinqToDB.CodeGen.Schema;

namespace LinqToDB.CodeGen.ContextModel
{
	public class ScalarFunctionModelBase : FunctionModelBase
	{
		protected ScalarFunctionModelBase(ObjectName name, MethodModel method, FunctionMetadata metadata)
			: base(name, method)
		{
			Metadata = metadata;
		}

		public FunctionMetadata Metadata { get; set; }

	}
}
