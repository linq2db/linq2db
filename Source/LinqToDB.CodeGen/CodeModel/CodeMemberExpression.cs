namespace LinqToDB.CodeGen.CodeModel
{
	public class CodeMemberExpression : ICodeExpression, ILValue
	{
		public CodeMemberExpression(ICodeExpression obj, CodeIdentifier member)
		{
			Object = obj;
			Member = member;
		}

		public CodeMemberExpression(IType type, CodeIdentifier member)
		{
			Type = new (type);
			Member = member;
		}

		public ICodeExpression? Object { get; }
		public TypeReference? Type { get; }
		public CodeIdentifier Member { get; }

		public CodeElementType ElementType => CodeElementType.MemberAccess;
	}
}
