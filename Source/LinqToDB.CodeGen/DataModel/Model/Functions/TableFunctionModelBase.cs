using LinqToDB.CodeGen.Schema;

namespace LinqToDB.CodeGen.DataModel
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
