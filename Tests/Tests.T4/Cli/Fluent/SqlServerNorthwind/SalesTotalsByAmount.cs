// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using System;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.Fluent.SqlServerNorthwind
{
	public class SalesTotalsByAmount
	{
		public decimal?  SaleAmount  { get; set; } // money
		public int       OrderId     { get; set; } // int
		public string    CompanyName { get; set; } = null!; // nvarchar(40)
		public DateTime? ShippedDate { get; set; } // datetime
	}
}
