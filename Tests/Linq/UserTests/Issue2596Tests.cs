using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

#nullable disable

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2596Tests : TestBase
	{
		public abstract class BaseEntity
		{
			[Column, PrimaryKey, Identity] public int Id { get; set; } // integer
		}

		[Table]
		public class CustomInvoice : BaseEntity
		{
			[Column, NotNull] public int ContractId { get; set; } // integer

			[Column, NotNull] public int InvoiceId { get; set; } // integer

			[Column, NotNull] public int ServicePointId { get; set; } // integer

			[Column, NotNull] public int AccessTariffId { get; set; } // integer

			[Association(ThisKey = "InvoiceId", OtherKey = "Id", CanBeNull = false)]
			public Invoice Invoice { get; set; }

			[Association(ThisKey = "ServicePointId", OtherKey = "Id", CanBeNull = false)]
			public ServicePoint ServicePoint { get; set; }

			[Association(ThisKey = "ContractId", OtherKey = "Id", CanBeNull = false)]
			public Contract Contract { get; set; }

			[Association(ThisKey = "AccessTariffId", OtherKey = "Id", CanBeNull = false)]
			public AccessTariff AccessTariff { get; set; }

			[Association(ThisKey = "Id", OtherKey = "CustomInvoiceId", CanBeNull = true)]
			public ICollection<CustomInvoiceLine> InvoiceLines { get; set; }

			[Association(ThisKey = "Id", OtherKey = "CustomInvoiceId", CanBeNull = true)]
			public ICollection<TypeAMeasures> TypeAMeasures { get; set; }

			[Association(ThisKey = "Id", OtherKey = "CustomInvoiceId", CanBeNull = true)]
			public ICollection<TypeBMeasures> TypeBMeasures { get; set; }

			[Column, NotNull] public int PriceListId { get; set; } // integer

			[Association(ThisKey = "PriceListId", OtherKey = "Id", CanBeNull = true)]
			public PriceList PriceList { get; set; }
		}

		[Table]
		public class CustomInvoiceLine : BaseEntity
		{
			[Column, NotNull] public int CustomInvoiceId { get; set; }
		}

		[Table]
		public class Contract : BaseEntity
		{
		}

		[Table]
		public class ProductUnit : BaseEntity
		{
		}

		[Table]
		public class ServicePoint : BaseEntity
		{
			[Column, NotNull] public int TownId { get; set; }

			[Column, NotNull] public int StreetTypeId { get; set; }

			[Association(ThisKey = "TownId", OtherKey = "Id", CanBeNull = true)]
			public Town Town { get; set; }

			[Association(ThisKey = "StreetTypeId", OtherKey = "Id", CanBeNull = true)]
			public StreetType StreetType { get; set; }
		}

		[Table]
		public class StreetType : BaseEntity
		{
		}

		[Table]
		public class TypeAMeasures : BaseEntity
		{
			/// <summary>
			/// Factura
			/// </summary>
			[Column, NotNull] public int CustomInvoiceId { get; set; }

			[Column, NotNull] public int MeasuresSourceId { get; set; }

			[Column, NotNull] public int PreviousSourceId { get; set; }

			[Association(ThisKey = "MeasuresSourceId", OtherKey = "Id", CanBeNull = true)]
			public MeasureSource Source { get; set; }

			[Association(ThisKey = "PreviousSourceId", OtherKey = "Id", CanBeNull = true)]
			public MeasureSource PreviousSource { get; set; }
		}

		[Table]
		public class MeasureSource : BaseEntity
		{
		}

		[Table]
		public class Invoice : BaseEntity
		{
			/// <summary>
			/// Invoice Lines
			/// </summary>
			[Association(ThisKey = "Id", OtherKey = "InvoiceId", CanBeNull = true)]
			public ICollection<InvoiceLine> Lines { get; set; }

			/// <summary>
			/// Tax Lines
			/// </summary>
			[Association(ThisKey = "Id", OtherKey = "InvoiceId", CanBeNull = true)]
			public ICollection<InvoiceTaxLine> TaxLines { get; set; }

			[Column, Nullable] public int PendingStateId { get; set; }

			[Association(ThisKey = "PendingStateId", OtherKey = "Id", CanBeNull = true)]
			public InvoicePendingState PendingState { get; set; }

			[Column, Nullable] public int? RectifyingInvoiceId { get; set; }

			[Association(ThisKey = "RectifyingInvoiceId", OtherKey = "Id", CanBeNull = true)]
			public Invoice Rectifying { get; set; }

			[Column, Nullable] public int? RefundByInvoiceId { get; set; }

			[Association(ThisKey = "RefundByInvoiceId", OtherKey = "Id", CanBeNull = true)]
			public Invoice RefundBy { get; set; }
		}

		[Table]
		public class InvoiceTaxLine : BaseEntity
		{
			[Column, Nullable] public int InvoiceId { get; set; } // integer

			[Column, Nullable] public int? TaxId { get; set; } // integer

			[Association(ThisKey = "TaxId", OtherKey = "Id", CanBeNull = false)]
			public AccountTax Tax { get; set; }
		}

		[Table]
		public class AccountTax : BaseEntity
		{
		}

		[Table]
		public class InvoiceLine : BaseEntity
		{
			[Column, Nullable] public int? ProductUnitId { get; set; } // integer
			[Column, Nullable] public int? InvoiceId { get; set; } // integer
			[Column, Nullable] public int? ProductId { get; set; } // integer

			[Association(ThisKey = "ProductUnitId", OtherKey = "Id", CanBeNull = true)]
			public ProductUnit ProductUnit { get; set; }

			[Association(ThisKey = "ProductId", OtherKey = "Id", CanBeNull = true)]
			public Product Product { get; set; }
		}

		[Table]
		public class Town : BaseEntity
		{
			[Column, Nullable] public int? StateId { get; set; } // integer

			[Association(ThisKey = "StateId", OtherKey = "Id", CanBeNull = true)]
			public CountryState State { get; set; }
		}

		[Table]
		public class CountryState : BaseEntity
		{
			[Column, NotNull] public int CountryId { get; set; } // integer
			[Column, Nullable] public int? AutonomousCommunityId { get; set; } // integer

			[Association(ThisKey = "AutonomousCommunityId", OtherKey = "Id", CanBeNull = true)]
			public AutonomousCommunity Community { get; set; }

			[Association(ThisKey = "CountryId", OtherKey = "Id", CanBeNull = true)]
			public Country Country { get; set; }
		}

		[Table]
		public class AutonomousCommunity : BaseEntity
		{
		}

		[Table]
		public class Country : BaseEntity
		{
		}

		[Table]
		public class InvoicePendingState : BaseEntity
		{
		}

		[Table]
		public class TypeBMeasures : BaseEntity
		{
			[Column, NotNull] public int CustomInvoiceId { get; set; }
		}

		[Table]
		public class AccessTariff : BaseEntity
		{
		}

		[Table]
		public class Product : BaseEntity
		{
		}

		[Table]
		public class PriceList : BaseEntity
		{
		}

		[YdbTableNotFound]
		[Test]
		public void TestLoadWithInfiniteLoop([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t01 = db.CreateLocalTable<PriceList>();
			using var t02 = db.CreateLocalTable<Product>();
			using var t03 = db.CreateLocalTable<AccessTariff>();
			using var t04 = db.CreateLocalTable<CountryState>();
			using var t05 = db.CreateLocalTable<AutonomousCommunity>();
			using var t06 = db.CreateLocalTable<Country>();
			using var t07 = db.CreateLocalTable<InvoicePendingState>();
			using var t08 = db.CreateLocalTable<TypeBMeasures>();
			using var t09 = db.CreateLocalTable<Town>();
			using var t10 = db.CreateLocalTable<InvoiceLine>();
			using var t11 = db.CreateLocalTable<AccountTax>();
			using var t12 = db.CreateLocalTable<InvoiceTaxLine>();
			using var t13 = db.CreateLocalTable<Invoice>();
			using var t14 = db.CreateLocalTable<MeasureSource>();
			using var t15 = db.CreateLocalTable<TypeAMeasures>();
			using var t16 = db.CreateLocalTable<StreetType>();
			using var t17 = db.CreateLocalTable<ServicePoint>();
			using var t18 = db.CreateLocalTable<ProductUnit>();
			using var t19 = db.CreateLocalTable<Contract>();
			using var t20 = db.CreateLocalTable<CustomInvoice>();
			using var t21 = db.CreateLocalTable<CustomInvoiceLine>();

			var query = db
					.GetTable<CustomInvoice>()
					.LoadWith(i => i.Invoice)
					.LoadWith(i => i.Invoice.Rectifying)
					.LoadWith(i => i.Invoice.RefundBy)
					.LoadWith(i => i.Invoice.PendingState)
					.LoadWith(i => i.Invoice.Lines)
					.LoadWith(i => i.Invoice.Lines.First().ProductUnit)
					.LoadWith(i => i.Invoice.Lines.First().Product)
					.LoadWith(i => i.Invoice.TaxLines)
					.LoadWith(i => i.Invoice.TaxLines.First().Tax)
					.LoadWith(i => i.Contract)
					.LoadWith(i => i.AccessTariff)
					.LoadWith(i => i.ServicePoint)
					.LoadWith(i => i.ServicePoint.Town)
					.LoadWith(i => i.ServicePoint.Town.State.Community)
					.LoadWith(i => i.ServicePoint.StreetType)
					.LoadWith(i => i.InvoiceLines)
					.LoadWith(i => i.TypeAMeasures)
					.LoadWith(i => i.TypeAMeasures.First().Source)
					.LoadWith(i => i.TypeAMeasures.First().PreviousSource)
					.LoadWith(i => i.TypeBMeasures)
					.LoadWith(i => i.PriceList)
					.Where(f => f.Id == 1);

			query.ToArray();
		}
	}
}
