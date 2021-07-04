namespace LinqToDB.CodeGen.ContextModel
{
	public class TableFunctionModel : TableFunctionModelBase
	{
		public ResultTableModel? CustomResult { get; set; }
		public EntityModel? EntityResult { get; set; }
	}
}
