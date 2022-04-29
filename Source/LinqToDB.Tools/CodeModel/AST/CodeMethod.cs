﻿using System.Collections.Generic;
using LinqToDB.Common;

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
			_typeArguments = new (typeParameters ?? Array<CodeTypeToken>.Empty);
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
		/// <c>null</c> for void methods.
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
	}
}
