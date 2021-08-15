namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Member access expression.
	/// </summary>
	public class CodeMember : ICodeExpression, ILValue
	{
		/// <summary>
		/// Create instance member access expression.
		/// </summary>
		/// <param name="instance">Member owner instance.</param>
		/// <param name="member">Member name.</param>
		public CodeMember(ICodeExpression instance, CodeIdentifier member)
		{
			Instance = instance;
			Member   = member;
		}

		/// <summary>
		/// Create static member access expression.
		/// </summary>
		/// <param name="type">Member owner type.</param>
		/// <param name="member">Member name.</param>
		public CodeMember(IType type, CodeIdentifier member)
		{
			Type   = new (type);
			Member = member;
		}

		/// <summary>
		/// Instance of member owner.
		/// </summary>
		public ICodeExpression?   Instance { get; }
		/// <summary>
		/// Type of member owner.
		/// </summary>
		public CodeTypeReference? Type     { get; }
		/// <summary>
		/// Member to access.
		/// </summary>
		public CodeIdentifier     Member   { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.MemberAccess;
	}
}
