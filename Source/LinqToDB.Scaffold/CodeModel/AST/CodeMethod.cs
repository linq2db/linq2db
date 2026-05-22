using System;
using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Class method definition.
	/// </summary>
	public sealed class CodeMethod : MethodBase, IGroupElement
	{
		private readonly List<CodeTypeToken> _typeArguments;

		public CodeMethod(
			IEnumerable<CodeAttribute>? customAttributes,
			Modifiers                   attributes,
			CodeBlock?                  body,
			CodeXmlComment?             xmlDoc,
			IEnumerable<CodeParameter>? parameters,
			CodeIdentifier              name,
			CodeTypeToken?              returnType,
			IEnumerable<CodeTypeToken>? typeParameters)
			: base(customAttributes, attributes, body, xmlDoc, parameters)
		{
			Name           = name;
			ReturnType     = returnType;
			_typeArguments = [.. typeParameters ?? []];

			Name.OnChange += _ => ChangeHandler?.Invoke(this);
			if (ReturnType != null)
				ReturnType.Type.SetNameChangeHandler(_ => ChangeHandler?.Invoke(this));
		}

		public CodeMethod(CodeIdentifier name)
			: this(null, default, null, null, null, name, null, null)
		{
		}

		/// <summary>
		/// Method name.
		/// </summary>
		public CodeIdentifier               Name           { get; }
		/// <summary>
		/// Method return type.
		/// <see langword="null"/> for void methods.
		/// </summary>
		public CodeTypeToken?               ReturnType     { get; internal set; }
		/// <summary>
		/// Generic method type parameters.
		/// </summary>
		public IReadOnlyList<CodeTypeToken> TypeParameters => _typeArguments;

		public override CodeElementType ElementType => CodeElementType.Method;

		internal void AddGenericParameter(CodeTypeToken genericParameter)
		{
			_typeArguments.Add(genericParameter);
		}

		/// <summary>
		/// Internal change-tracking infrastructure. Single action instance is enough.
		/// </summary>
		internal Action<CodeMethod>? ChangeHandler { get; set; }
	}
}
