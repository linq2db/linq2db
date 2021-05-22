﻿using System.Collections.Generic;
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

		[Table(Schema = "public", Name = "custom_invoice")]
		public class CustomInvoice : BaseEntity
		{
			[Column, NotNull] public int ContractId { get; set; } // integer

			[Column, NotNull] public int InvoiceId { get; set; } // integer

			[Column, NotNull] public int ServicePointId { get; set; } // integer

			[Column, NotNull] public int AccessTariffId { get; set; } // integer

			[Association(ThisKey = "InvoiceId", OtherKey = "Id", CanBeNull = false,
				KeyName = "custom_invoice_InvoiceId_fkey",
				BackReferenceName = "facturaciofacturainvoiceidfkeys")]
			public Invoice Invoice { get; set; }

			[Association(ThisKey = "ServicePointId", OtherKey = "Id", CanBeNull = false,
				KeyName = "custom_invoice_ServicePointId_fkey",
				BackReferenceName = "facturaciofacturacupsidfkeys")]
			public ServicePoint ServicePoint { get; set; }

			[Association(ThisKey = "ContractId", OtherKey = "Id", CanBeNull = false,
				KeyName = "custom_invoice_ContractId_fkey",
				BackReferenceName = "facturaciofacturapolissaidfkeys")]
			public Contract Contract { get; set; }

			[Association(ThisKey = "AccessTariffId", OtherKey = "Id", CanBeNull = false,
				KeyName = "custom_invoice_AccessTariffId_fkey",
				BackReferenceName = "facturaciofacturatarifaaccesidfkeys")]
			public AccessTariff AccessTariff { get; set; }

			/// <summary>
			/// Línies d energia
			/// </summary>
			[Association(ThisKey = "Id", OtherKey = "CustomInvoiceId", CanBeNull = true)]
			public ICollection<CustomInvoiceLine> InvoiceLines { get; set; }

			/// <summary>
			/// Lecturas de energía
			/// </summary>
			[Association(ThisKey = "Id", OtherKey = "CustomInvoiceId", CanBeNull = true)]
			public ICollection<TypeAMeasures> TypeAMeasures { get; set; }

			/// <summary>
			/// Lecturas de energía
			/// </summary>
			[Association(ThisKey = "Id", OtherKey = "CustomInvoiceId", CanBeNull = true)]
			public ICollection<TypeBMeasures> TypeBMeasures { get; set; }


			[Column, NotNull] public int PriceListId { get; set; } // integer

			/// <summary>
			/// Price list used for this invoice
			/// </summary>
			[Association(ThisKey = "PriceListId", OtherKey = "Id", CanBeNull = true,
				KeyName = "custom_invoice_llista_preu_fkey",
				BackReferenceName = "facturaciofacturallista_preufkeys")]
			public PriceList PriceList { get; set; }
		}

		[Table(Schema = "public", Name = "custom_invoice_linia")]
		public class CustomInvoiceLine : BaseEntity
		{
			[Column, NotNull] public int CustomInvoiceId { get; set; }
		}

		[Table(Schema = "public", Name = "polissa")]
		public class Contract : BaseEntity
		{
		}


		[Table(Schema = "public", Name = "product_uom")]
		public class ProductUnit : BaseEntity
		{
		}


		[Table(Schema = "public", Name = "cups_ps")]
		public class ServicePoint : BaseEntity
		{
			[Column, NotNull] public int TownId { get; set; }

			[Column, NotNull] public int StreetTypeId { get; set; }

			/// <summary>
			///     Municipio
			/// </summary>
			[Association(ThisKey = "TownId", OtherKey = "Id", CanBeNull = true, KeyName = "service_point_TownId_fkey", BackReferenceName = "cupspsidmunicipifkeys")]
			public Town Town { get; set; }

			/// <summary>
			///     Tipo vía
			/// </summary>
			[Association(ThisKey = "StreetTypeId", OtherKey = "Id", CanBeNull = true, KeyName = "service_point_StreetTypeId_fkey")]
			public StreetType StreetType { get; set; }
		}

		[Table(Schema = "public", Name = "street_type")]
		public class StreetType : BaseEntity
		{
		}

		[Table(Schema = "public", Name = "type_a_measures")]
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

		[Table(Schema = "public", Name = "lectures_origen")]
		public class MeasureSource : BaseEntity
		{
		}

		[Table(Schema = "public", Name = "invoice")]
		public class Invoice : BaseEntity
		{
			/// <summary>
			/// Invoice Lines
			/// </summary>
			[Association(ThisKey = "Id", OtherKey = "InvoiceId", CanBeNull = true, IsBackReference = true)]
			public ICollection<InvoiceLine> Lines { get; set; }

			/// <summary>
			/// Tax Lines
			/// </summary>
			[Association(ThisKey = "Id", OtherKey = "InvoiceId", CanBeNull = true)]
			public ICollection<InvoiceTaxLine> TaxLines { get; set; }

			[Column, Nullable] public int PendingStateId { get; set; }

			[Association(ThisKey = "PendingStateId", OtherKey = "Id", CanBeNull = true, KeyName = "invoice_pending_state_fkey", BackReferenceName = "accountinvoicependingstatefkeys")]
			public InvoicePendingState PendingState { get; set; }

			[Column, Nullable] public int? RectifyingInvoiceId { get; set; }

			[Association(ThisKey = "RectifyingInvoiceId", OtherKey = "Id", CanBeNull = true, KeyName = "invoice_rectifying_id_fkey", BackReferenceName = "accountinvoicerectifyingidfkeys")]
			public Invoice Rectifying { get; set; }

			[Column, Nullable] public int? RefundByInvoiceId { get; set; }

			[Association(ThisKey = "RefundByInvoiceId", OtherKey = "Id", CanBeNull = true, KeyName = "invoice_refund_by_id_fkey", BackReferenceName = "accountinvoicerefundbyidfkeys")]
			public Invoice RefundBy { get; set; }
		}

		[Table(Schema = "public", Name = "invoice_tax")]
		public class InvoiceTaxLine : BaseEntity
		{
			[Column, Nullable] public int InvoiceId { get; set; } // integer

			[Column, Nullable] public int? TaxId { get; set; } // integer

			[Association(ThisKey = "TaxId", OtherKey = "Id", CanBeNull = false, KeyName = "invoice_TaxId_fkey", BackReferenceName = "accountinvoicetaxidfkeys")]
			public AccountTax Tax { get; set; }
		}

		[Table(Schema = "public", Name = "account_tax")]
		public class AccountTax : BaseEntity
		{
		}


		[Table(Schema = "public", Name = "invoice_line")]
		public class InvoiceLine : BaseEntity
		{
			[Column, Nullable] public int? ProductUnitId { get; set; } // integer
			[Column, Nullable] public int? InvoiceId { get; set; } // integer
			[Column, Nullable] public int? ProductId { get; set; } // integer

			[Association(ThisKey = "ProductUnitId", OtherKey = "Id", CanBeNull = true,
				KeyName = "invoice_line_uos_fkey",
				BackReferenceName = "accountinvoicelineuosfkeys")]
			public ProductUnit ProductUnit { get; set; }

			[Association(ThisKey = "ProductId", OtherKey = "Id", CanBeNull = true,
				KeyName = "invoice_line_procut_fkey", BackReferenceName = "invoice_line_procut_fkeys")]
			public Product Product { get; set; }
		}

		[Table(Schema = "public", Name = "town")]
		public class Town : BaseEntity
		{
			[Column, Nullable] public int? StateId { get; set; } // integer

			[Association(ThisKey = "StateId", OtherKey = "Id", CanBeNull = true, KeyName = "town_state_fkey")]
			public CountryState State { get; set; }
		}

		[Table(Schema = "public", Name = "country_state")]
		public class CountryState : BaseEntity
		{
			[Column, NotNull] public int CountryId { get; set; } // integer
			[Column, Nullable] public int? AutonomousCommunityId { get; set; } // integer

			[Association(ThisKey = "AutonomousCommunityId", OtherKey = "Id", CanBeNull = true, KeyName = "country_state_autonomous_community_fkey")]
			public AutonomousCommunity Community { get; set; }

			[Association(ThisKey = "CountryId", OtherKey = "Id", CanBeNull = true, KeyName = "country_state_contry_fkey")]
			public Country Country { get; set; }
		}

		[Table(Schema = "public", Name = "autonomous_community")]
		public class AutonomousCommunity : BaseEntity
		{
		}

		[Table(Schema = "public", Name = "country")]
		public class Country : BaseEntity
		{
		}

		[Table(Schema = "public", Name = "invoice_pending_state")]
		public class InvoicePendingState : BaseEntity
		{
		}

		[Table(Schema = "public", Name = "type_b_measures")]
		public class TypeBMeasures : BaseEntity
		{
			[Column, NotNull] public int CustomInvoiceId { get; set; }
		}

		[Table(Schema = "public", Name = "access_tariff")]
		public class AccessTariff : BaseEntity
		{
		}

		[Table(Schema = "public", Name = "product_product")]
		public class Product : BaseEntity
		{
		}


		[Table(Schema = "public", Name = "product_pricelist")]
		public class PriceList : BaseEntity
		{
		}

		[Test]
		public void TestLoadWithInfiniteLoop([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			{
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

				Assert.DoesNotThrow(() => _ = query.ToString());
			}
		}
	}
}
