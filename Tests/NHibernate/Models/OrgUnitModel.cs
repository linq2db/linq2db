using FluentNHibernate.Mapping;

namespace LinqToDB.NHibernate.Tests.Models.Org
{
	/// <summary>
	/// A self-referencing tree (scalar <see cref="ParentId"/>) used to demonstrate recursive CTEs through the
	/// bridge — something NHibernate's own LINQ provider cannot express.
	/// </summary>
	public class OrgUnit
	{
		public virtual int    Id       { get; set; }
		public virtual int?   ParentId { get; set; }
		public virtual string Name     { get; set; } = null!;
	}

	public class OrgUnitMap : ClassMap<OrgUnit>
	{
		public OrgUnitMap()
		{
			Table("OrgUnits");
			Id(x => x.Id).GeneratedBy.Assigned().Column("Id"); // assigned so the test controls the tree shape
			Map(x => x.ParentId).Column("ParentId");
			Map(x => x.Name).Column("Name").Not.Nullable();
		}
	}
}
