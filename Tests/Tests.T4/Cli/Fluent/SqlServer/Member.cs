// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------


#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.Fluent.SqlServer
{
	public class Member
	{
		public int    MemberId { get; set; } // int
		public string Alias    { get; set; } = null!; // nvarchar(50)

		#region Associations
		/// <summary>
		/// FK_Provider_Member backreference
		/// </summary>
		public Provider? Provider { get; set; }
		#endregion
	}
}
