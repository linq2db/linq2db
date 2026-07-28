using FluentNHibernate.Mapping;

namespace LinqToDB.NHibernate.Tests.Models.Inheritance
{
	// Table-per-hierarchy: every class shares one table, told apart by a discriminator column that is NOT
	// exposed as a mapped property (the usual NHibernate style).

	public class Voucher
	{
		public virtual int    Id    { get; set; }
		public virtual string Title { get; set; } = null!;
	}

	public class Invoice : Voucher
	{
		public virtual string? InvoiceNo { get; set; }
	}

	public class Receipt : Voucher
	{
		public virtual string? ReceiptNo { get; set; }
	}

	public class VoucherMap : ClassMap<Voucher>
	{
		public VoucherMap()
		{
			Table("Voucher");
			Id(x => x.Id).GeneratedBy.Assigned().Column("VoucherId");
			Map(x => x.Title).Column("Title").Not.Nullable();
			DiscriminateSubClassesOnColumn("VoucherType").Length(10);
		}
	}

	public class InvoiceMap : SubclassMap<Invoice>
	{
		public InvoiceMap()
		{
			DiscriminatorValue("INV");
			Map(x => x.InvoiceNo).Column("InvoiceNo");
		}
	}

	// Table-per-subclass: the base table holds the shared columns, each subclass its own table joined by key.
	public class Vehicle
	{
		public virtual int    Id   { get; set; }
		public virtual string Name { get; set; } = null!;
	}

	public class Car : Vehicle
	{
		public virtual int Doors { get; set; }
	}

	public class VehicleMap : ClassMap<Vehicle>
	{
		public VehicleMap()
		{
			Table("Vehicle");
			Id(x => x.Id).GeneratedBy.Assigned().Column("VehicleId");
			Map(x => x.Name).Column("Name").Not.Nullable();
		}
	}

	public class CarMap : SubclassMap<Car>
	{
		public CarMap()
		{
			Table("Car");
			KeyColumn("VehicleId");
			Map(x => x.Doors).Column("Doors");
		}
	}

	// Table-per-concrete-class: every concrete class carries all of its columns in its own table.
	public class Shape
	{
		public virtual int    Id   { get; set; }
		public virtual string Name { get; set; } = null!;
	}

	public class Square : Shape
	{
		public virtual int Side { get; set; }
	}

	public class ShapeMap : ClassMap<Shape>
	{
		public ShapeMap()
		{
			Table("Shape");
			Id(x => x.Id).GeneratedBy.Assigned().Column("ShapeId");
			Map(x => x.Name).Column("Name").Not.Nullable();
			UseUnionSubclassForInheritanceMapping();
		}
	}

	public class SquareMap : SubclassMap<Square>
	{
		public SquareMap()
		{
			Table("Square");
			Map(x => x.Side).Column("Side");
		}
	}

	public class ReceiptMap : SubclassMap<Receipt>
	{
		public ReceiptMap()
		{
			DiscriminatorValue("RCP");
			Map(x => x.ReceiptNo).Column("ReceiptNo");
		}
	}
}
