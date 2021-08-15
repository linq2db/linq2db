using System;
using System.Linq;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Method call expression/statement.
	/// </summary>
	public class CodeCall : ICodeExpression, ICodeStatement
	{
		public CodeCall(bool extension, ICodeExpression callee, CodeIdentifier method, IType[] genericArguments, ICodeExpression[] parameters)
		{
			Extension     = extension;
			Callee        = callee;
			MethodName    = method;
			TypeArguments = genericArguments.Length > 0 ? genericArguments.Select(t => new CodeTypeToken(t)).ToArray() : Array.Empty<CodeTypeToken>();
			Parameters    = parameters;
		}

		/// <summary>
		/// Indicates that method is an extension method.
		/// Note that for <c>this</c> parameter passed in parameters and <see cref="Callee"/> property contains type
		/// where extension method declared.
		/// </summary>
		public bool              Extension     { get; }
		/// <summary>
		/// Callee object or type (for static method call).
		/// </summary>
		public ICodeExpression   Callee        { get; }
		/// <summary>
		/// Called method name.
		/// </summary>
		public CodeIdentifier    MethodName    { get; }
		/// <summary>
		/// Type arguments for generic method call.
		/// </summary>
		public CodeTypeToken[]   TypeArguments { get; }
		/// <summary>
		/// Method call parameters.
		/// </summary>
		public ICodeExpression[] Parameters    { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.Call;
	}
}
