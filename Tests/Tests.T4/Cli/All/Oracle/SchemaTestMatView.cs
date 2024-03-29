// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.All.Oracle
{
	/// <summary>
	/// This is matview
	/// </summary>
	[Table("SchemaTestMatView", IsView = true)]
	public class SchemaTestMatView : IEquatable<SchemaTestMatView>
	{
		/// <summary>
		/// This is matview column
		/// </summary>
		[Column("Id", DataType = DataType.Decimal, DbType = "NUMBER", Length = 22, IsPrimaryKey = true)] public OracleDecimal Id { get; set; } // NUMBER

		#region IEquatable<T> support
		private static readonly IEqualityComparer<SchemaTestMatView> _equalityComparer = ComparerBuilder.GetEqualityComparer<SchemaTestMatView>(c => c.Id);

		public bool Equals(SchemaTestMatView? other)
		{
			return _equalityComparer.Equals(this, other!);
		}

		public override int GetHashCode()
		{
			return _equalityComparer.GetHashCode(this);
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as SchemaTestMatView);
		}
		#endregion
	}
}
