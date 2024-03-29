// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using IBM.Data.DB2Types;
using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;
using System;
using System.Collections.Generic;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.All.DB2
{
	[Table("KeepIdentityTest")]
	public class KeepIdentityTest : IEquatable<KeepIdentityTest>
	{
		[Column("ID"   , DataType = DataType.Int32, DbType = "INTEGER", IsPrimaryKey = true, IsIdentity = true, SkipOnInsert = true, SkipOnUpdate = true)] public DB2Int32  Id    { get; set; } // INTEGER
		[Column("Value", DataType = DataType.Int32, DbType = "INTEGER"                                                                                  )] public DB2Int32? Value { get; set; } // INTEGER

		#region IEquatable<T> support
		private static readonly IEqualityComparer<KeepIdentityTest> _equalityComparer = ComparerBuilder.GetEqualityComparer<KeepIdentityTest>(c => c.Id);

		public bool Equals(KeepIdentityTest? other)
		{
			return _equalityComparer.Equals(this, other!);
		}

		public override int GetHashCode()
		{
			return _equalityComparer.GetHashCode(this);
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as KeepIdentityTest);
		}
		#endregion
	}
}
