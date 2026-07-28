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

	public class ReceiptMap : SubclassMap<Receipt>
	{
		public ReceiptMap()
		{
			DiscriminatorValue("RCP");
			Map(x => x.ReceiptNo).Column("ReceiptNo");
		}
	}
}
