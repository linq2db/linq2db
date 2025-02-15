using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Namespace declaration.
	/// </summary>
	public sealed class CodeNamespace : ITopLevelElement
	{
		private readonly List<IMemberGroup> _members;

		internal CodeNamespace(IReadOnlyList<CodeIdentifier> name, IEnumerable<IMemberGroup>? members)
		{
			Name     = name;
			_members = new (members ?? []);
		}

		public CodeNamespace(IReadOnlyList<CodeIdentifier> name)
			: this(name, null)
		{
		}

		/// <summary>
		/// Namespace name.
		/// </summary>
		public IReadOnlyList<CodeIdentifier> Name    { get; }
		/// <summary>
		/// Namespace members (in groups).
		/// </summary>
		public IReadOnlyList<IMemberGroup>   Members => _members;

		CodeElementType ICodeElement.ElementType => CodeElementType.Namespace;

		internal void AddGroup(IMemberGroup group)
		{
			_members.Add(group);
		}
	}
}
