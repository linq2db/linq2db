using System.Collections.Generic;
using LinqToDB.CodeGen.Metadata;

namespace LinqToDB.CodeGen.ContextModel
{
	public abstract class FunctionModelBase
	{
		public ObjectName DbName { get; set; } = null!;
		public string? Description { get; set; }
		public string MethodName { get; set; } = null!;
		public string MethodInfoName { get; set; } = null!;

		public List<ParameterModel> Parameters { get; } = new();
	}
}
