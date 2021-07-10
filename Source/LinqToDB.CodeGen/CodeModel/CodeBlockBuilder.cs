namespace LinqToDB.CodeGen.CodeModel
{
	public class CodeBlockBuilder
	{
		public CodeBlockBuilder(CodeBlock block)
		{
			Block = block;
		}

		public CodeBlock Block { get; }

		//public CodeBlockBuilder Return(ICodeExpression expression)
		//{
		//	var returnStatement = new ReturnStatement(expression);
		//	Block.Add(returnStatement);
		//	return this;
		//}

		public CodeBlockBuilder Append(ICodeStatement statement)
		{
			Block.Add(statement);
			return this;
		}
	}
}
