namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Member access expression.
	/// </summary>
	public sealed class CodeMember : ICodeExpression, ILValue
	{
		/// <summary>
		/// Create instance member access expression.
		/// </summary>
		/// <param name="instance">Member owner instance.</param>
		/// <param name="member">Member name.</param>
		/// <param name="memberType">Member type.</param>
		public CodeMember(ICodeExpression instance, CodeIdentifier member, IType memberType)
		{
			Instance   = instance;
			Member     = member;
			MemberType = memberType;
		}

		/// <summary>
		/// Create static member access expression.
		/// </summary>
		/// <param name="type">Member owner type.</param>
		/// <param name="member">Member name.</param>
		/// <param name="memberType">Member type.</param>
		public CodeMember(IType type, CodeIdentifier member, IType memberType)
		{
			Type       = new (type);
			Member     = member;
			MemberType = memberType;
		}

		/// <summary>
		/// Instance of member owner.
		/// </summary>
		public ICodeExpression?   Instance   { get; }
		/// <summary>
		/// Type of member owner.
		/// </summary>
		public CodeTypeReference? Type       { get; }
		/// <summary>
		/// Member to access.
		/// </summary>
		public CodeIdentifier     Member     { get; }

		public IType              MemberType { get; }

		IType ICodeExpression.Type => MemberType;

		CodeElementType ICodeElement.ElementType => CodeElementType.MemberAccess;
	}
}
