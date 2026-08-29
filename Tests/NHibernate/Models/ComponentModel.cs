using FluentNHibernate.Mapping;

namespace LinqToDB.NHibernate.Tests.Models.Components
{
	// A <component>: a value object whose properties are columns of the owning entity's own table.

	public class PostalAddress
	{
		public virtual string? Street { get; set; }
		public virtual string? City   { get; set; }
	}

	public class Contact
	{
		public virtual int            Id      { get; set; }
		public virtual string         Name    { get; set; } = null!;
		public virtual PostalAddress  Address { get; set; } = new();
	}

	public class ContactMap : ClassMap<Contact>
	{
		public ContactMap()
		{
			Table("Contact");
			Id(x => x.Id).GeneratedBy.Assigned().Column("ContactId");
			Map(x => x.Name).Column("Name").Not.Nullable();
			Component(x => x.Address, m =>
			{
				m.Map(a => a.Street).Column("Street");
				m.Map(a => a.City).Column("City");
			});
		}
	}
}
