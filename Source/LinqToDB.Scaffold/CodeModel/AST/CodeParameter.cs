using System;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Method parameter.
	/// </summary>
	public sealed class CodeParameter : CodeTypedName, ICodeElement
	{
		internal CodeParameter(CodeTypeToken type, CodeIdentifier name, CodeParameterDirection direction, ICodeExpression? defaultValue)
			: base(name, type)
		{
			Direction    = direction;
			DefaultValue = defaultValue;

			Name.OnChange += _ => ChangeHandler?.Invoke(this);
			Type.Type.SetNameChangeHandler(_ => ChangeHandler?.Invoke(this));
		}

		public CodeParameter(IType type, CodeIdentifier name, CodeParameterDirection direction, ICodeExpression? defaultValue)
			: this(new CodeTypeToken(type), name, direction, defaultValue)
		{
		}

		/// <summary>
		/// Parameter direction.
		/// </summary>
		public CodeParameterDirection Direction    { get; }
		/// <summary>
		/// Parameter's default value.
		/// </summary>
		public ICodeExpression?       DefaultValue { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.Parameter;

		/// <summary>
		/// Internal change-tracking infrastructure. Single action instance is enough.
		/// </summary>
		internal Action<CodeParameter>? ChangeHandler { get; set; }
	}
}
