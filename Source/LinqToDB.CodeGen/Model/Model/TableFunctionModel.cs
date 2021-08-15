using LinqToDB.CodeGen.CodeGeneration;
using LinqToDB.CodeGen.ContextModel;
using LinqToDB.CodeGen.Schema;

namespace LinqToDB.CodeGen.Model
{
	public class TableFunctionModel : TableFunctionModelBase
	{
		public TableFunctionModel(ObjectName name, MethodModel method, TableFunctionMetadata metadata, string methodInfoFieldName)
			: base(name, method)
		{
			Metadata = metadata;
			MethodInfoFieldName = methodInfoFieldName;
		}

		public string MethodInfoFieldName { get; set; }
		public TableFunctionMetadata Metadata { get; set; }

		public (ResultTableModel? customTable, EntityModel? entity) Result { get; set; }
	}
}
