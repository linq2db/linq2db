// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB.Mapping;
using System.Collections.Generic;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.Default.DB2
{
	[Table("MASTERTABLE")]
	public class Mastertable
	{
		[Column("ID1", IsPrimaryKey = true, PrimaryKeyOrder = 0)] public int Id1 { get; set; } // INTEGER
		[Column("ID2", IsPrimaryKey = true, PrimaryKeyOrder = 1)] public int Id2 { get; set; } // INTEGER

		#region Associations
		/// <summary>
		/// FK_SLAVETABLE_MASTERTABLE backreference
		/// </summary>
		[Association(ThisKey = nameof(Id1) + "," + nameof(Id2), OtherKey = nameof(Slavetable.Id222222222222222222222222) + "," + nameof(Slavetable.Id1))]
		public IEnumerable<Slavetable> Slavetables { get; set; } = null!;
		#endregion
	}
}