// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;
using System;
using System.Collections.Generic;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.All.Sybase
{
	[Table("TestMergeIdentity")]
	public class TestMergeIdentity : IEquatable<TestMergeIdentity>
	{
		[Column("Id"   , DataType = DataType.Int32, DbType = "int", Length = 4, IsPrimaryKey = true, IsIdentity = true, SkipOnInsert = true, SkipOnUpdate = true)] public int  Id    { get; set; } // int
		[Column("Field", DataType = DataType.Int32, DbType = "int", Length = 4                                                                                  )] public int? Field { get; set; } // int

		#region IEquatable<T> support
		private static readonly IEqualityComparer<TestMergeIdentity> _equalityComparer = ComparerBuilder.GetEqualityComparer<TestMergeIdentity>(c => c.Id);

		public bool Equals(TestMergeIdentity? other)
		{
			return _equalityComparer.Equals(this, other!);
		}

		public override int GetHashCode()
		{
			return _equalityComparer.GetHashCode(this);
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as TestMergeIdentity);
		}
		#endregion
	}
}
