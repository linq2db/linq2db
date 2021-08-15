using LinqToDB.CodeGen.Model;
using LinqToDB.CodeGen.Schema;

namespace LinqToDB.CodeGen.ContextModel
{
	public class TableFunctionModelBase : FunctionModelBase
	{
		protected TableFunctionModelBase(ObjectName name, MethodModel method)
			: base(name, method)
		{
		}

		public string? Error { get; set; }
	}
}
