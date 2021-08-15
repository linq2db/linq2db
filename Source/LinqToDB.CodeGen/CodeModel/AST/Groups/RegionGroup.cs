using System.Linq;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Group of regions.
	/// </summary>
	public class RegionGroup : MemberGroup<CodeRegion>
	{
		public RegionGroup(CodeClass @class)
		{
			OwnerType = @class;
		}

		/// <summary>
		/// Regions containing class.
		/// </summary>
		public CodeClass OwnerType { get; }

		public override CodeElementType ElementType => CodeElementType.RegionGroup;

		/// <summary>
		/// Add new region to group.
		/// </summary>
		/// <param name="name">Region name.</param>
		/// <returns>New region builder class.</returns>
		public RegionBuilder New(string name)
		{
			var region = new CodeRegion(OwnerType, name);
			Members.Add(region);
			return new RegionBuilder(region);
		}

		public override bool IsEmpty => Members.All(m => m.IsEmpty());
	}
}
