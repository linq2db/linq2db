namespace LinqToDB.CodeGen.CodeModel
{
	public class ArrayExpression : ICodeExpression
	{
		public ArrayExpression(IType type, bool valueTyped, ICodeExpression[] values, bool inline)
		{
			Type = new (type);
			ValueTyped = valueTyped;
			Inline = inline;
			Values = values;
		}

		public TypeToken Type { get; }
		public bool ValueTyped { get; }
		public bool Inline { get; }
		public ICodeExpression[] Values { get; }

		public CodeElementType ElementType => CodeElementType.Array;
	}
}
