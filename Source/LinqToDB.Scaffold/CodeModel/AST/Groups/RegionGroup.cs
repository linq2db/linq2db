using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Group of regions.
	/// </summary>
	public sealed class RegionGroup : MemberGroup<CodeRegion>
	{
		public RegionGroup(IEnumerable<CodeRegion>? members, CodeClass @class)
			: base(members)
		{
			OwnerType = @class;
		}

		public RegionGroup(CodeClass @class)
			: this(null, @class)
		{
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
			return new RegionBuilder(AddMember(new CodeRegion(OwnerType, name)));
		}

		public override bool IsEmpty => Members.All(static m => m.IsEmpty());
	}
}
