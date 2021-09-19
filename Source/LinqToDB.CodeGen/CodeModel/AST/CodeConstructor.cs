using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Class constructor.
	/// </summary>
	public sealed class CodeConstructor : MethodBase, IGroupElement
	{
		public CodeConstructor(
			List<CodeAttribute>?   customAttributes,
			Modifiers              attributes,
			CodeBlock?             body,
			CodeXmlComment?        xmlDoc,
			List<CodeParameter>?   parameters,
			CodeClass              @class,
			bool                   thisCall,
			List<ICodeExpression>? baseArguments)
			: base(customAttributes, attributes, body, xmlDoc, parameters)
		{
			Class         = @class;
			ThisCall      = thisCall;
			BaseArguments = baseArguments ?? new();
		}

		public CodeConstructor(CodeClass @class)
			: this(null, default, null, null, null, @class, default, null)
		{
		}

		/// <summary>
		/// Owner class.
		/// </summary>
		public CodeClass             Class         { get; }
		/// <summary>
		/// Indicator wether constructor calls <c>this()</c> or <c>base</c> constructor.
		/// </summary>
		public bool                  ThisCall      { get; set; }
		/// <summary>
		/// Parameters for <c>this()</c> or <c>base</c> constructor call.
		/// </summary>
		public List<ICodeExpression> BaseArguments { get; }

		public override CodeElementType ElementType => CodeElementType.Constructor;
	}
}
