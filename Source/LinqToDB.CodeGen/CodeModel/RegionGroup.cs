using System.Linq;

namespace LinqToDB.CodeGen.CodeModel
{
	public class RegionGroup : MemberGroup<CodeRegion>
	{
		public RegionGroup(CodeClass @class)
		{
			OwnerType = @class;
		}

		public CodeClass OwnerType { get; }

		public override CodeElementType ElementType => CodeElementType.RegionGroup;

		public CodeRegionBuilder New(string name)
		{
			var region = new CodeRegion(OwnerType, name);
			Members.Add(region);
			return new CodeRegionBuilder(region);
		}

		public override bool IsEmpty => Members.All(m => m.IsEmpty());
	}
}
