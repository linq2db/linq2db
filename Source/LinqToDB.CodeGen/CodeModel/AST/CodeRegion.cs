using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Code region.
	/// </summary>
	public sealed class CodeRegion : ITopLevelElement, IGroupElement
	{
		internal CodeRegion(CodeClass ownerType, string name, List<IMemberGroup>? members)
		{
			Type    = ownerType;
			Name    = name;
			Members = members ?? new ();
		}

		public CodeRegion(CodeClass ownerType, string name)
			: this(ownerType, name, null)
		{
		}

		/// <summary>
		/// Region name.
		/// </summary>
		public string             Name    { get; }
		/// <summary>
		/// Owner class in which region is declared.
		/// </summary>
		public CodeClass          Type    { get; }
		/// <summary>
		/// Region members (in groups).
		/// </summary>
		public List<IMemberGroup> Members { get; }

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
	}
}
