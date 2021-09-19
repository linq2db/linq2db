using System;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Constant expression. E.g. literal (including <c>null</c> literal) or enumeration value.
	/// </summary>
	public sealed class CodeConstant : ICodeExpression
	{
		private readonly object?        _value;
		private readonly Func<object?>? _delayedValue;

		internal CodeConstant(CodeTypeToken type, object? value, bool targetTyped)
		{
			Type        = type;
			_value      = value;
			TargetTyped = targetTyped;
		}

		public CodeConstant(IType type, object? value, bool targetTyped)
			: this(new CodeTypeToken(type), value, targetTyped)
		{
		}

		/// <summary>
		/// Creates new constant expression with value not available when constructor called but available from callback
		/// on code-generation stage.
		/// </summary>
		/// <param name="type">Constant type.</param>
		/// <param name="delayedValue">Delayed value callback.</param>
		/// <param name="targetTyped">Type is constrained by context, where constant used.</param>
		public CodeConstant(IType type, Func<object?> delayedValue, bool targetTyped)
		{
			Type          = new(type);
			_delayedValue = delayedValue;
			TargetTyped   = targetTyped;
		}

		/// <summary>
		/// Constant type.
		/// </summary>
		public CodeTypeToken Type { get; }
		/// <summary>
		/// Constant value.
		/// </summary>
		public object?       Value => _delayedValue != null ? _delayedValue() : _value;
		/// <summary>
		/// Indicates that constant type is constrained by context (e.g. used in assignment to property of specific type)
		/// and code generator could use it to ommit type information.
		/// </summary>
		public bool          TargetTyped { get; }

		IType ICodeExpression.Type => Type.Type;

		CodeElementType ICodeElement.ElementType => CodeElementType.Constant;
	}

}
