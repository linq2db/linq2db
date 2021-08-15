using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Class method definition.
	/// </summary>
	public class CodeMethod : MethodBase, IGroupElement
	{
		public CodeMethod(CodeIdentifier name)
		{
			Name = name;
		}

		/// <summary>
		/// Method name.
		/// </summary>
		public CodeIdentifier      Name           { get; }
		/// <summary>
		/// Method return type.
		/// <c>null</c> for void methods.
		/// </summary>
		public CodeTypeToken?      ReturnType     { get; internal set; }
		/// <summary>
		/// Generic method type parameters.
		/// </summary>
		public List<CodeTypeToken> TypeParameters { get; } = new ();

		public override CodeElementType ElementType => CodeElementType.Method;
	}
}
