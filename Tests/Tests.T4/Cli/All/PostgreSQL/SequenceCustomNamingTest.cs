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

namespace Cli.All.PostgreSQL
{
	[Table("SequenceCustomNamingTest")]
	public class SequenceCustomNamingTest : IEquatable<SequenceCustomNamingTest>
	{
		[Column("ID"   , DataType = DataType.Int32   , DbType = "integer"              , Precision = 32, Scale = 0, IsPrimaryKey = true, IsIdentity = true, SkipOnInsert = true, SkipOnUpdate = true)] public int     Id    { get; set; } // integer
		[Column("Value", DataType = DataType.NVarChar, DbType = "character varying(50)", Length    = 50                                                                                             )] public string? Value { get; set; } // character varying(50)

		#region IEquatable<T> support
		private static readonly IEqualityComparer<SequenceCustomNamingTest> _equalityComparer = ComparerBuilder.GetEqualityComparer<SequenceCustomNamingTest>(c => c.Id);

		public bool Equals(SequenceCustomNamingTest? other)
		{
			return _equalityComparer.Equals(this, other!);
		}

		public override int GetHashCode()
		{
			return _equalityComparer.GetHashCode(this);
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as SequenceCustomNamingTest);
		}
		#endregion
	}
}
