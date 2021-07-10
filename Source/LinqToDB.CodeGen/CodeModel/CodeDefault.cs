namespace LinqToDB.CodeGen.CodeModel
{
	public class CodeDefault : ICodeExpression
	{
		public CodeDefault(IType type, bool targetTyped)
		{
			Type = new (type);
			TargetTyped = targetTyped;
		}

		public TypeToken Type { get; }
		public bool TargetTyped { get; }

		public CodeElementType ElementType => CodeElementType.Default;
	}

}
