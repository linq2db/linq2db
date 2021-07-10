using System.Collections.Generic;

namespace LinqToDB.CodeGen.CodeModel
{
	public class CodeRegion : ITopLevelCodeElement, IMemberElement, IMembersOwner
	{
		public CodeRegion(CodeClass ownerType, string name)
		{
			Type = ownerType;
			Name = name;
		}

		public string Name { get; }

		public CodeClass Type { get; }

		public List<IMemberGroup> Members { get; set; } = new();

		public CodeElementType ElementType => CodeElementType.Region;

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
