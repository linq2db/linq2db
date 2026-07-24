using System.Collections.Generic;

using FluentNHibernate.Mapping;

namespace LinqToDB.NHibernate.Tests.Models.Associations
{
	// Models that exercise association shapes the Northwind model doesn't cover:
	//  * Widget -> Gadget is a many-to-one whose foreign-key member (Widget.Gid) is named differently from the
	//    target's primary-key member (Gadget.GadgetId). The source also exposes the FK column as a scalar member.
	//  * Bin -> Slots is a unidirectional one-to-many whose child (Slot) exposes no scalar foreign-key member.

	public class Gadget
	{
		public virtual int    GadgetId { get; set; }
		public virtual string Name     { get; set; } = null!;
	}

	public class Widget
	{
		public virtual int     Id     { get; set; }
		public virtual string  Name   { get; set; } = null!;
		// Scalar foreign key, mapped to the same column as the Gadget reference but named differently from
		// Gadget's primary-key member.
		public virtual int     Gid    { get; set; }
		// Many-to-one to Gadget over the same column; read-only so the column is written through Gid.
		public virtual Gadget? Gadget { get; set; }
	}

	public class Bin
	{
		public Bin()
		{
			Slots = new HashSet<Slot>();
		}

		public virtual int              Id    { get; set; }
		public virtual string           Name  { get; set; } = null!;
		public virtual ICollection<Slot> Slots { get; set; }
	}

	// The child of the unidirectional one-to-many: no back-reference and no scalar for the BinId foreign key.
	public class Slot
	{
		public virtual int    Id   { get; set; }
		public virtual string Name { get; set; } = null!;
	}

	public class GadgetMap : ClassMap<Gadget>
	{
		public GadgetMap()
		{
			Table("Gadget");
			// Assigned ids: the association tests build a Widget.Id != Gadget.GadgetId scenario deterministically,
			// so the differently-named foreign key can be told apart from a same-value coincidence.
			Id(x => x.GadgetId).GeneratedBy.Assigned().Column("GadgetId");
			Map(x => x.Name).Column("Name").Not.Nullable();
		}
	}

	public class WidgetMap : ClassMap<Widget>
	{
		public WidgetMap()
		{
			Table("Widget");
			Id(x => x.Id).GeneratedBy.Assigned().Column("WidgetId");
			Map(x => x.Name).Column("Name").Not.Nullable();
			Map(x => x.Gid).Column("GadgetId");
			References(x => x.Gadget).Column("GadgetId").ReadOnly();
		}
	}

	public class BinMap : ClassMap<Bin>
	{
		public BinMap()
		{
			Table("Bin");
			Id(x => x.Id).GeneratedBy.Assigned().Column("BinId");
			Map(x => x.Name).Column("Name").Not.Nullable();
			HasMany(x => x.Slots).KeyColumn("BinId").Cascade.All();
		}
	}

	public class SlotMap : ClassMap<Slot>
	{
		public SlotMap()
		{
			Table("Slot");
			Id(x => x.Id).GeneratedBy.Assigned().Column("SlotId");
			Map(x => x.Name).Column("Name").Not.Nullable();
		}
	}
}
