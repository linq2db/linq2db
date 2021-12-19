using System;
using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Method call statement.
	/// </summary>
	public abstract class CodeCallBase
	{
		private readonly List<CodeTypeToken>   _genericArguments;
		private readonly List<ICodeExpression> _parameters;

		protected CodeCallBase(
			bool                         extension,
			ICodeExpression              callee,
			CodeIdentifier               method,
			IEnumerable<CodeTypeToken>   genericArguments,
			IEnumerable<ICodeExpression> parameters)
		{
			Extension         = extension;
			Callee            = callee;
			MethodName        = method;
			_genericArguments = new (genericArguments ?? Array.Empty<CodeTypeToken>());
			_parameters       = new (parameters       ?? Array.Empty<ICodeExpression>());
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
		public IReadOnlyList<CodeTypeToken>         TypeArguments => _genericArguments;
		/// <summary>
		/// Method call parameters.
		/// </summary>
		public IReadOnlyList<ICodeExpression>       Parameters    => _parameters;
	}
}
