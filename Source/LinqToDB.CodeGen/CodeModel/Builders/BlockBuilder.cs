namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// <see cref="CodeBlock"/> object builder.
	/// </summary>
	public class BlockBuilder
	{
		internal BlockBuilder(CodeBlock block)
		{
			Block = block;
		}

		/// <summary>
		/// Built code block.
		/// </summary>
		public CodeBlock Block { get; }

		/// <summary>
		/// Add statement to block.
		/// </summary>
		/// <param name="statement">Statement to add.</param>
		/// <returns>Builder instance.</returns>
		public BlockBuilder Append(ICodeStatement statement)
		{
			Block.Add(statement);
			return this;
		}
	}
}
