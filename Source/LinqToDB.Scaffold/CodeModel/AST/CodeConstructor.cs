using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Class constructor.
	/// </summary>
	public sealed class CodeConstructor : MethodBase, IGroupElement
	{
		private readonly List<ICodeExpression> _baseParameters;

		public CodeConstructor(
			IEnumerable<CodeAttribute>?   customAttributes,
			Modifiers                     attributes,
			CodeBlock?                    body,
			CodeXmlComment?               xmlDoc,
			IEnumerable<CodeParameter>?   parameters,
			CodeClass                     @class,
			bool                          thisCall,
			IEnumerable<ICodeExpression>? baseArguments)
			: base(customAttributes, attributes, body, xmlDoc, parameters)
		{
			Class           = @class;
			ThisCall        = thisCall;
			_baseParameters = [.. baseArguments ?? []];
		}

		public CodeConstructor(CodeClass @class)
			: this(null, default, null, null, null, @class, default, null)
		{
		}

		/// <summary>
		/// Owner class.
		/// </summary>
		public CodeClass                      Class         { get; }
		/// <summary>
		/// Indicator wether constructor calls <c>this()</c> or <c>base</c> constructor.
		/// </summary>
		public bool                           ThisCall      { get; internal set; }
		/// <summary>
		/// Parameters for <c>this()</c> or <c>base</c> constructor call.
		/// </summary>
		public IReadOnlyList<ICodeExpression> BaseArguments => _baseParameters;

		public override CodeElementType ElementType => CodeElementType.Constructor;

		internal void AddBaseParameters(ICodeExpression[] parameters)
		{
			_baseParameters.AddRange(parameters);
		}
	}
}
