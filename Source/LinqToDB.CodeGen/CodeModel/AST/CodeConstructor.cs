using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Class constructor.
	/// </summary>
	public class CodeConstructor : MethodBase, IGroupElement
	{
		public CodeConstructor(CodeClass type)
		{
			Type = type;
		}

		/// <summary>
		/// Owner class.
		/// </summary>
		public CodeClass             Type          { get; }
		/// <summary>
		/// Indicator wether constructor calls <c>this()</c> or <c>base</c> constructor.
		/// </summary>
		public bool                  ThisCall      { get; set; }
		/// <summary>
		/// Parameters for <c>this()</c> or <c>base</c> constructor call.
		/// </summary>
		public List<ICodeExpression> BaseArguments { get; } = new();

		public override CodeElementType ElementType => CodeElementType.Constructor;
	}
}
