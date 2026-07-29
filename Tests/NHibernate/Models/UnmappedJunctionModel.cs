using System.Collections.Generic;

using FluentNHibernate.Mapping;

namespace LinqToDB.NHibernate.Tests.Models.UnmappedJunction
{
	// A plain HasManyToMany: the junction table is known to NHibernate only by name, and is deliberately NOT
	// mapped as an entity of its own — the ordinary way many-to-many is written.

	public class Club
	{
		public Club()
		{
			Members = new HashSet<Member>();
		}

		public virtual int                 Id      { get; set; }
		public virtual string              Name    { get; set; } = null!;
		public virtual ICollection<Member> Members { get; set; }
	}

	public class Member
	{
		public virtual int    Id   { get; set; }
		public virtual string Name { get; set; } = null!;
	}

	public class ClubMap : ClassMap<Club>
	{
		public ClubMap()
		{
			Table("Club");
			Id(x => x.Id).GeneratedBy.Assigned().Column("ClubId");
			Map(x => x.Name).Column("Name").Not.Nullable();
			HasManyToMany(x => x.Members).Table("ClubMember").ParentKeyColumn("ClubId").ChildKeyColumn("MemberId");
		}
	}

	public class MemberMap : ClassMap<Member>
	{
		public MemberMap()
		{
			Table("Member");
			Id(x => x.Id).GeneratedBy.Assigned().Column("MemberId");
			Map(x => x.Name).Column("Name").Not.Nullable();
		}
	}

	// The same shape with COMPOSITE keys on both sides: the junction carries all four columns and is still not
	// mapped as an entity, so each side's columns must be ANDed and paired in the right order.

	public class Zone
	{
		public Zone()
		{
			Facilities = new HashSet<Facility>();
		}

		public virtual int                   ZoneId     { get; set; }
		public virtual int                   ZoneNo     { get; set; }
		public virtual string                Name       { get; set; } = null!;
		public virtual ICollection<Facility> Facilities { get; set; }

		public override bool Equals(object? obj)
			=> obj is Zone other && other.ZoneId == ZoneId && other.ZoneNo == ZoneNo;

		public override int GetHashCode()
			=> (ZoneId, ZoneNo).GetHashCode();
	}

	public class Facility
	{
		public virtual int    SiteId     { get; set; }
		public virtual int    FacilityNo { get; set; }
		public virtual string Label      { get; set; } = null!;

		public override bool Equals(object? obj)
			=> obj is Facility other && other.SiteId == SiteId && other.FacilityNo == FacilityNo;

		public override int GetHashCode()
			=> (SiteId, FacilityNo).GetHashCode();
	}

	public class ZoneMap : ClassMap<Zone>
	{
		public ZoneMap()
		{
			Table("Zone");
			CompositeId()
				.KeyProperty(x => x.ZoneId, "ZoneId")
				.KeyProperty(x => x.ZoneNo, "ZoneNo");
			Map(x => x.Name).Column("Name").Not.Nullable();

			var facilities = HasManyToMany(x => x.Facilities).Table("ZoneFacility");
			facilities.ParentKeyColumns.Add("ZoneId", "ZoneNo");      // junction -> zone (composite)
			facilities.ChildKeyColumns.Add("SiteId", "FacilityNo");   // junction -> facility (composite)
		}
	}

	public class FacilityMap : ClassMap<Facility>
	{
		public FacilityMap()
		{
			Table("Facility");
			CompositeId()
				.KeyProperty(x => x.SiteId,     "SiteId")
				.KeyProperty(x => x.FacilityNo, "FacilityNo");
			Map(x => x.Label).Column("Label").Not.Nullable();
		}
	}
}
