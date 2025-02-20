using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Code region.
	/// </summary>
	public sealed class CodeRegion : ITopLevelElement, IGroupElement
	{
		private readonly List<IMemberGroup> _members;

		internal CodeRegion(CodeClass ownerType, string name, IEnumerable<IMemberGroup>? members)
		{
			Type     = ownerType;
			Name     = name;
			_members = [.. members ?? []];
		}

		public CodeRegion(CodeClass ownerType, string name)
			: this(ownerType, name, null)
		{
		}

		/// <summary>
		/// Region name.
		/// </summary>
		public string                      Name    { get; }
		/// <summary>
		/// Owner class in which region is declared.
		/// </summary>
		public CodeClass                   Type    { get; }
		/// <summary>
		/// Region members (in groups).
		/// </summary>
		public IReadOnlyList<IMemberGroup> Members => _members;

		CodeElementType ICodeElement.ElementType => CodeElementType.Region;

		/// <summary>
		/// Returns true if region is empty.
		/// </summary>
		public bool IsEmpty()
		{
			foreach (var group in Members)
			{
				if (!group.IsEmpty)
					return false;
			}

			return true;
		}

		internal void Add(IMemberGroup group)
		{
			_members.Add(group);
		}
	}
}
