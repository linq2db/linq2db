namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// New object instantiation expression.
	/// </summary>
	public class CodeNew : ICodeExpression
	{
		public CodeNew(IType type, ICodeExpression[] parameters, CodeAssignment[] initializers)
		{
			Type         = new (type);
			Parameters   = parameters;
			Initializers = initializers;
		}

		/// <summary>
		/// Instantiated type.
		/// </summary>
		public CodeTypeToken     Type         { get; }
		/// <summary>
		/// Constructor parameters.
		/// </summary>
		public ICodeExpression[] Parameters   { get; }
		/// <summary>
		/// Object initializer properties.
		/// </summary>
		public CodeAssignment[]  Initializers { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.New;
	}
}
