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
	[Table("Person")]
	public class Person : IEquatable<Person>
	{
		[Column("PersonID"  , DataType = DataType.Int32  , DbType = "INTEGER"     , IsPrimaryKey = true, IsIdentity = true, SkipOnInsert = true, SkipOnUpdate = true)] public DB2Int32   PersonId   { get; set; } // INTEGER
		[Column("FirstName" , DataType = DataType.VarChar, DbType = "VARCHAR(50)" , Length       = 50                                                               )] public DB2String  FirstName  { get; set; } // VARCHAR(50)
		[Column("LastName"  , DataType = DataType.VarChar, DbType = "VARCHAR(50)" , Length       = 50                                                               )] public DB2String  LastName   { get; set; } // VARCHAR(50)
		[Column("MiddleName", DataType = DataType.VarChar, DbType = "VARCHAR(50)" , Length       = 50                                                               )] public DB2String? MiddleName { get; set; } // VARCHAR(50)
		[Column("Gender"    , DataType = DataType.Char   , DbType = "CHARACTER(1)", Length       = 1                                                                )] public DB2String  Gender     { get; set; } // CHARACTER(1)

		#region IEquatable<T> support
		private static readonly IEqualityComparer<Person> _equalityComparer = ComparerBuilder.GetEqualityComparer<Person>(c => c.PersonId);

		public bool Equals(Person? other)
		{
			return _equalityComparer.Equals(this, other!);
		}

		public override int GetHashCode()
		{
			return _equalityComparer.GetHashCode(this);
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as Person);
		}
		#endregion

		#region Associations
		/// <summary>
		/// FK_Doctor_Person backreference
		/// </summary>
		[Association(ThisKey = nameof(PersonId), OtherKey = nameof(DB2.Doctor.PersonId))]
		public Doctor? Doctor { get; set; }

		/// <summary>
		/// FK_Patient_Person backreference
		/// </summary>
		[Association(ThisKey = nameof(PersonId), OtherKey = nameof(DB2.Patient.PersonId))]
		public Patient? Patient { get; set; }
		#endregion
	}
}
