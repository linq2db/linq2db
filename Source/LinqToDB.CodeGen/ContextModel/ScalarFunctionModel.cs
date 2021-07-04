namespace LinqToDB.CodeGen.ContextModel
{
	public class ScalarFunctionModel : ScalarFunctionModelBase
	{
		public (TypeModel type, string? propertyName)[] Type { get; set; } = null!;

		public string? TupleTypeName { get; set; }
	}
}
