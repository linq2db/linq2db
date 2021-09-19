using LinqToDB.CodeGen.Metadata;
using LinqToDB.CodeGen.Schema;

namespace LinqToDB.CodeGen.DataModel
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
