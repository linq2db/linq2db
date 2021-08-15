using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Code region.
	/// </summary>
	public class CodeRegion : ITopLevelElement, IGroupElement
	{
		public CodeRegion(CodeClass ownerType, string name)
		{
			Type = ownerType;
			Name = name;
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
		public List<IMemberGroup> Members { get; } = new();

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
