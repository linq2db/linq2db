using System;

using LinqToDB.Mapping;

namespace Tests.Model
{
	public interface TInheritance
	{
		int? TypeDiscriminator { get; set; }
	}

	[Table("InheritanceParent")]
	[InheritanceMapping(Code = null, Type = typeof(InheritanceParentBase), IsDefault = true)]
	[InheritanceMapping(Code = 1,    Type = typeof(InheritanceParent1))]
	[InheritanceMapping(Code = 2,    Type = typeof(InheritanceParent2))]
	public class InheritanceParentBase : TInheritance
	{
		[PrimaryKey]                     public int  InheritanceParentId { get; set; }
		[Column(IsDiscriminator = true)] public int? TypeDiscriminator   { get; set; }

		public override bool Equals(object obj)
		{
			var other = obj as InheritanceParentBase;
			if (other == null)
				return false;

			if (ReferenceEquals(this, other))
				return true;

			return
				InheritanceParentId == other.InheritanceParentId &&
				TypeDiscriminator   == other.TypeDiscriminator   &&
				GetType()           == other.GetType();
		}

		public override int GetHashCode()
		{
			return InheritanceParentId;
		}
	}

	public class InheritanceParent1 : InheritanceParentBase
	{
	}

	public class InheritanceParent2 : InheritanceParentBase
	{
		[Column] public string Name { get; set; }
	}

	[Table("InheritanceChild")]
	[InheritanceMapping(Code = null, Type = typeof(InheritanceChildBase), IsDefault = true)]
	[InheritanceMapping(Code = 1,    Type = typeof(InheritanceChild1))]
	[InheritanceMapping(Code = 2,    Type = typeof(InheritanceChild2))]
	public class InheritanceChildBase : TInheritance
	{
		[PrimaryKey]                     public int  InheritanceChildId  { get; set; }
		[Column(IsDiscriminator = true)] public int? TypeDiscriminator   { get; set; }
		[Column]                         public int  InheritanceParentId { get; set; }

		[Association(ThisKey = "InheritanceParentId", OtherKey = "InheritanceParentId")]
		public InheritanceParentBase Parent { get; set; } 

		public override bool Equals(object obj)
		{
			var other = obj as InheritanceChildBase;
			if (other == null)
				return false;

			if (ReferenceEquals(this, other))
				return true;

			return InheritanceChildId == other.InheritanceChildId
				&& TypeDiscriminator  == other.TypeDiscriminator
				&& GetType()          == other.GetType();
		}

		public override int GetHashCode()
		{
			return InheritanceChildId;
		}
	}

	public class InheritanceChild1 : InheritanceChildBase
	{
	}

	public class InheritanceChild2 : InheritanceChildBase
	{
		[Column] public string Name { get; set; }
	}
}
