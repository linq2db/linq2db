#if !NETFRAMEWORK
using System.Collections.Generic;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.ManyToMany
{
	// 1. Implicit many-to-many, single-column keys.
	public class MmStudent
	{
		public int                   Id      { get; set; }
		public string                Name    { get; set; } = null!;
		public ICollection<MmCourse> Courses { get; set; } = null!;
	}

	public class MmCourse
	{
		public int                    Id       { get; set; }
		public string                 Title    { get; set; } = null!;
		public ICollection<MmStudent> Students { get; set; } = null!;
	}

	// 2. Explicit join entity with payload.
	public class MmOrder
	{
		public int                    Id       { get; set; }
		public string                 Number   { get; set; } = null!;
		public ICollection<MmProduct> Products { get; set; } = null!;
	}

	public class MmProduct
	{
		public int                  Id     { get; set; }
		public string               Name   { get; set; } = null!;
		public ICollection<MmOrder> Orders { get; set; } = null!;
	}

	public class MmOrderProduct
	{
		public int       OrderId   { get; set; }
		public int       ProductId { get; set; }
		public int       Qty       { get; set; }
		public MmOrder   Order     { get; set; } = null!;
		public MmProduct Product   { get; set; } = null!;
	}

	// 3. Composite-key many-to-many via an explicit join entity.
	public class MmProject
	{
		public int                   OrgId   { get; set; }
		public int                   Code    { get; set; }
		public string                Name    { get; set; } = null!;
		public ICollection<MmMember> Members { get; set; } = null!;
	}

	public class MmMember
	{
		public int                    Id       { get; set; }
		public string                 Name     { get; set; } = null!;
		public ICollection<MmProject> Projects { get; set; } = null!;
	}

	public class MmProjectMember
	{
		public int       OrgId    { get; set; }
		public int       Code     { get; set; }
		public int       MemberId { get; set; }
		public MmProject Project  { get; set; } = null!;
		public MmMember  Member   { get; set; } = null!;
	}

	// 4. Self-referencing many-to-many via an explicit join entity.
	public class MmPerson
	{
		public int                   Id        { get; set; }
		public string                Name      { get; set; } = null!;
		public ICollection<MmPerson> Friends   { get; set; } = null!;
		public ICollection<MmPerson> FriendsOf { get; set; } = null!;
	}

	public class MmFriendship
	{
		public int      PersonId { get; set; }
		public int      FriendId { get; set; }
		public MmPerson Person   { get; set; } = null!;
		public MmPerson Friend   { get; set; } = null!;
	}

	// 5. Two distinct many-to-many relationships between the same entity pair (explicit joins).
	public class MmUser
	{
		public int                 Id       { get; set; }
		public string              Name     { get; set; } = null!;
		public ICollection<MmTeam> Teams    { get; set; } = null!;
		public ICollection<MmTeam> LedTeams { get; set; } = null!;
	}

	public class MmTeam
	{
		public int                 Id      { get; set; }
		public string              Name    { get; set; } = null!;
		public ICollection<MmUser> Members { get; set; } = null!;
		public ICollection<MmUser> Leaders { get; set; } = null!;
	}

	public class MmMembership
	{
		public int UserId { get; set; }
		public int TeamId { get; set; }
	}

	public class MmLeadership
	{
		public int UserId { get; set; }
		public int TeamId { get; set; }
	}

	// 6. Two implicit many-to-many relationships between the same entity pair (unsupported -> clear error).
	public class MmDoc
	{
		public int                  Id              { get; set; }
		public string               Title           { get; set; } = null!;
		public ICollection<MmLabel> PrimaryLabels   { get; set; } = null!;
		public ICollection<MmLabel> SecondaryLabels { get; set; } = null!;
	}

	public class MmLabel
	{
		public int                Id            { get; set; }
		public string             Name          { get; set; } = null!;
		public ICollection<MmDoc> PrimaryDocs   { get; set; } = null!;
		public ICollection<MmDoc> SecondaryDocs { get; set; } = null!;
	}

	// 7. Key mapped to a private field with no CLR property (field name must match the EF property name),
	//    with a renamed column.
	public class MmAccount
	{
#pragma warning disable CS0169, CS0649 // assigned by EF via the field-mapped "AccountId" key
		private int                AccountId;
#pragma warning restore CS0169, CS0649
		public string              Name  { get; set; } = null!;
		public ICollection<MmRole> Roles { get; set; } = null!;
	}

	public class MmRole
	{
		public int                    Id       { get; set; }
		public string                 Name     { get; set; } = null!;
		public ICollection<MmAccount> Accounts { get; set; } = null!;
	}

	// 8. Shadow primary key (no CLR member), with a renamed column.
	public class MmArticle
	{
		public int                Id    { get; set; }
		public string             Title { get; set; } = null!;
		public ICollection<MmTag> Tags  { get; set; } = null!;
	}

	public class MmTag
	{
		public string                 Label    { get; set; } = null!;
		public ICollection<MmArticle> Articles { get; set; } = null!;
	}
}
#endif
