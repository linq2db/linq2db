using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1584Tests : TestBase
	{
		public enum TransportType
		{
			Udp                = 1,
			Connectionless     = 1,
			Tcp                = 2,
			ConnectionOriented = 2,
			All                = 3
		}
		private sealed class RateCharges
		{
			public RateEntry? RateEntry    { get; set; }
			public decimal    FlatRate     { get; set; }
			public decimal    MinRate      { get; set; }
			public decimal    VariableRate { get; set; }
		}

		public class RateEntry
		{
			public const string RateCategoryOrigin      = "ORG";
			public const string RateCategoryDestination = "DST";
			public const string HazardoudsCommodityCode = "HAZ";

			public Guid      TI_PK                                { get; set; }
			public short     TI_LineOrder                         { get; set; }
			public DateTime  TI_RateStartDate                     { get; set; }
			public DateTime? TI_RateEndDate                       { get; set; }
			public string?   TI_RX_NKCurrency                     { get; set; }
			public int       TI_Frequency                         { get; set; }
			public Guid?     TI_OH_Supplier                       { get; set; }
			public Guid?     TI_OH_TransportProvider              { get; set; }
			public Guid?     TI_OH_Consignor                      { get; set; }
			public Guid?     TI_OH_Consignee                      { get; set; }
			public Guid?     TI_OA_CartagePickupAddressOverride   { get; set; }
			public Guid?     TI_OA_CartageDeliveryAddressOverride { get; set; }
			public string?   TI_CartagePickupAddressPostCode      { get; set; }
			public string?   TI_CartageDeliveryAddressPostCode    { get; set; }
			public string?   TI_RS_NKServiceLevel_NI              { get; set; }
			public string?   TI_OriginLRC                         { get; set; }
			public string?   TI_DestinationLRC                    { get; set; }
			public string?   TI_ViaLRC                            { get; set; }
			public string?   TI_PageHeading                       { get; set; }
			public string?   TI_PageOpeningText                   { get; set; }
			public string?   TI_PageClosingText                   { get; set; }
			public Guid?     TI_OH_AgentOverride                  { get; set; }
			public Guid?     TI_ParentID                          { get; set; }
			public string?   TI_QuotePageIncoTerm                 { get; set; }
			public string?   TI_BuyersConsolRateMode              { get; set; }
			public DateTime? TI_SystemCreateTimeUtc               { get; set; }
			public string?   TI_SystemCreateUser                  { get; set; }
			public DateTime? TI_SystemLastEditTimeUtc             { get; set; }
			public string?   TI_SystemLastEditUser                { get; set; }
			public Guid      TI_TH                                { get; set; }
			public Guid?     TI_RC                                { get; set; }
			public string?   TI_ContractNumber                    { get; set; }
			public string?   TI_PL_NKCarrierServiceLevel          { get; set; }
			public Guid?     TI_TZ_OriginZone                     { get; set; }
			public Guid?     TI_TZ_DestinationZone                { get; set; }
			public bool      TI_IsValid                           { get; set; }
			public bool      TI_IsCrossTrade                      { get; set; }
			public bool      TI_DataChecked                       { get; set; }
			public bool      TI_MatchContainerRateClass           { get; set; }
			public string?   TI_TransitTime                       { get; set; }
			public string?   TI_FrequencyUnit                     { get; set; }

			[ColumnAlias(nameof(TI_Mode)), Column(IsColumn = false)]
			public TransportType? TI_ModeTT { get; set; }

			public string? TI_Mode               { get; set; }
			public string? TI_RH_NKCommodityCode { get; set; }
			public string? TI_RateCategory       { get; set; }

			[ColumnAlias(nameof(TI_RateCategory)), Column(IsColumn = false)]
			public TransportType? TI_RateCategoryTT { get; set; }

			public Guid?   TI_FromID                 { get; set; }
			public string? TI_FromTableCode          { get; set; }
			public Guid?   TI_ToId                   { get; set; }
			public string? TI_ToTableCode            { get; set; }
			public Guid?   TI_OH_ControllingCustomer { get; set; }
			public Guid    TI_GC_Publisher           { get; set; }
			public bool    TI_IsTact                 { get; set; }
			public string? TI_ParentTableCode        { get; set; }
			public int     TI_RateKey                { get; set; }

			[ExpressionMethod(nameof(GetModeExpression))]
			public TransportType? GetMode()
			{
				switch (TI_RateCategory)
				{
					case RateCategoryDestination:
					case RateCategoryOrigin:
						return TI_ModeTT;
					default:
						return TI_RateCategoryTT;
				}
			}

			private static Expression<Func<RateEntry, TransportType?>> GetModeExpression()
			{
				return rateEntry => rateEntry.TI_RateCategory == RateCategoryDestination
									|| rateEntry.TI_RateCategory == RateCategoryOrigin
					? rateEntry.TI_ModeTT
					: rateEntry.TI_RateCategoryTT;
			}
		}

		public class RateLines
		{
			public Guid TL_PK { get; set; }
			//a bunch of properties removed
			public Guid TL_TI { get; set; }
		}

		public class RateLineItem
		{
			public Guid    TM_PK        { get; set; }
			public byte    TM_LineOrder { get; set; }
			public string? TM_Type      { get; set; }
			public decimal TM_Value     { get; set; }
			public Guid    TM_TL        { get; set; }
		}

		[Test]
		public void CteTest([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<RateEntry>())
			using (db.CreateLocalTable<RateLines>())
			using (db.CreateLocalTable<RateLineItem>())
			{
				var cte = (from rateEntry in db.GetTable<RateEntry>()
						   from rateLine in db.GetTable<RateLines>().LeftJoin(lines => lines.TL_TI == rateEntry.TI_PK)
						   from rateLineItem in db.GetTable<RateLineItem>().LeftJoin(items =>
						items.TM_TL == rateLine.TL_PK)
						   where (rateEntry.TI_RateEndDate == null || rateEntry.TI_RateEndDate > Sql.CurrentTimestamp)
						  && rateLineItem.TM_Type.In("MIN", "FLT", "BAS", "UNT")
						   group rateLineItem by rateEntry
					into s
						   select new RateCharges
						   {
							   RateEntry = s.Key,
							   FlatRate = s.Sum(r => r.TM_Type.In("FLT", "BAS") ? r.TM_Value : 0),
							   MinRate = s.Sum(r => r.TM_Type == "MIN" ? r.TM_Value : 0),
							   VariableRate = s.Sum(r => r.TM_Type == "UNT" ? r.TM_Value : 0)
						   }).AsCte("rateCost");

				var query = from rateEntry in db.GetTable<RateEntry>()
							from c in cte.InnerJoin(c => c.RateEntry!.TI_PK == rateEntry.TI_PK)
							select new
							{
								RateEntry = rateEntry,
								CTE = c
							};
				var result = query.ToArray();
			}
		}
	}
}
