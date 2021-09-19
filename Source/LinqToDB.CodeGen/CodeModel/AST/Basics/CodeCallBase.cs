using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Method call statement.
	/// </summary>
	public abstract class CodeCallBase
	{
		protected CodeCallBase(
			bool                           extension,
			ICodeExpression                callee,
			CodeIdentifier                 method,
			IReadOnlyList<CodeTypeToken>   genericArguments,
			IReadOnlyList<ICodeExpression> parameters)
		{
			Extension     = extension;
			Callee        = callee;
			MethodName    = method;
			TypeArguments = genericArguments;
			Parameters    = parameters;
		}

		/// <summary>
		/// Indicates that method is an extension method.
		/// Note that for <c>this</c> parameter passed in parameters and <see cref="Callee"/> property contains type
		/// where extension method declared.
		/// </summary>
		public bool                                 Extension     { get; }
		/// <summary>
		/// Callee object or type (for static method call).
		/// </summary>
		public ICodeExpression                      Callee        { get; }
		/// <summary>
		/// Called method name.
		/// </summary>
		public CodeIdentifier                       MethodName    { get; }
		/// <summary>
		/// Type arguments for generic method call.
		/// </summary>
		public IReadOnlyList<CodeTypeToken>         TypeArguments { get; }
		/// <summary>
		/// Method call parameters.
		/// </summary>
		public IReadOnlyList<ICodeExpression>       Parameters    { get; }
	}
}
