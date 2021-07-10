using System.Collections.Generic;

namespace LinqToDB.CodeGen.CodeModel
{
	public class CodeAttribute : ITopLevelCodeElement
	{
		public CodeAttribute(IType type)
		{
			Type = new (type);
		}

		public TypeToken Type { get; }
		public List<ICodeExpression> Parameters { get; } = new ();
		public List<(CodeIdentifier property, ICodeExpression value)> NamedParameters { get; } = new ();

		public CodeElementType ElementType => CodeElementType.Attribute;
	}

}
