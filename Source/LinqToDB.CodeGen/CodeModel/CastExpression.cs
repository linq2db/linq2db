namespace LinqToDB.CodeGen.CodeModel
{
	public class CastExpression : ICodeExpression
	{
		public CastExpression(IType type, ICodeExpression value)
		{
			Type = new (type);
			Value = value;
		}

		public TypeToken Type { get; }
		public ICodeExpression Value { get; }

		public CodeElementType ElementType => CodeElementType.Cast;
	}
}
