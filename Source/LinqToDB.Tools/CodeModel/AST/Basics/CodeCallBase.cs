using System.Collections.Generic;
using LinqToDB.Common;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Method call statement.
	/// </summary>
	public abstract class CodeCallBase
	{
		private readonly List<CodeTypeToken>   _genericArguments;
		private readonly List<ICodeExpression> _parameters;

		private List<SimpleTrivia>?   _wrapTrivia;

		protected CodeCallBase(
			bool                         extension,
			ICodeExpression              callee,
			CodeIdentifier               method,
			IEnumerable<CodeTypeToken>?  genericArguments,
			bool                         skipTypeArguments,
			IEnumerable<ICodeExpression> parameters,
			IEnumerable<SimpleTrivia>?   wrapTrivia)
		{
			Extension            = extension;
			Callee               = callee;
			MethodName           = method;
			CanSkipTypeArguments = skipTypeArguments;
			_genericArguments    = new (genericArguments ?? []);
			_parameters          = new (parameters       ?? []);
			_wrapTrivia          = wrapTrivia == null ? null : new(wrapTrivia);
		}

		/// <summary>
		/// Indicates, that type arguments generation could be skipped, as they could be inferred from context.
		/// </summary>
		public bool                                 CanSkipTypeArguments { get; }
		/// <summary>
		/// Indicates that method is an extension method.
		/// Note that for <c>this</c> parameter passed in parameters and <see cref="Callee"/> property contains type
		/// where extension method declared.
		/// </summary>
		public bool                                 Extension            { get; }
		/// <summary>
		/// Callee object or type (for static method call).
		/// </summary>
		public ICodeExpression                      Callee               { get; }
		/// <summary>
		/// Called method name.
		/// </summary>
		public CodeIdentifier                       MethodName           { get; }
		/// <summary>
		/// Type arguments for generic method call.
		/// </summary>
		public IReadOnlyList<CodeTypeToken>         TypeArguments        => _genericArguments;
		/// <summary>
		/// Method call parameters.
		/// </summary>
		public IReadOnlyList<ICodeExpression>       Parameters           => _parameters;

		public IReadOnlyList<SimpleTrivia>?         WrapTrivia           => _wrapTrivia;

		/// <summary>
		/// Add trivia element for method call wrap.
		/// </summary>
		/// <param name="trivia">Simple trivia element.</param>
		internal void AddWrapSimpleTrivia(SimpleTrivia trivia) => (_wrapTrivia ??= new()).Add(trivia);
	}
}
