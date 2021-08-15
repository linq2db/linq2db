using System.Collections.Generic;
using LinqToDB.CodeGen.ContextModel;
using LinqToDB.CodeGen.Schema;

namespace LinqToDB.CodeGen.Model
{
	public abstract class FunctionModelBase
	{
		protected FunctionModelBase(ObjectName name, MethodModel method)
		{
			Name = name;
			Method = method;
		}

		public ObjectName Name { get; set; }
		public MethodModel Method { get; set; }

		public List<FunctionParameterModel> Parameters { get; } = new();
	}
}
