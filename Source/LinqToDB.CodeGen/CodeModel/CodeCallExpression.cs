using System.Linq;

namespace LinqToDB.CodeGen.CodeModel
{
	public class CodeCallExpression : ICodeExpression, ICodeStatement
	{
		public CodeCallExpression(bool extension, ICodeExpression? obj, CodeIdentifier method, IType[] genericArguments, ICodeExpression[] parameters)
		{
			Extension = extension;
			Callee = obj;
			MethodName = method;
			TypeArguments = genericArguments.Select(t => new TypeToken(t)).ToArray();
			Parameters = parameters;
		}
		public bool Extension { get; }
		public ICodeExpression? Callee { get; }
		public CodeIdentifier MethodName { get; }
		public TypeToken[] TypeArguments { get; }
		public ICodeExpression[] Parameters { get; }

		public CodeElementType ElementType => CodeElementType.Call;
	}

}
