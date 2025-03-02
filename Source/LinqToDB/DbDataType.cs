using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Model;

namespace LinqToDB
{
	/// <summary>
	/// Stores database type attributes.
	/// </summary>
	[DebuggerDisplay("DbDataType: {ToString()}")]
	public struct DbDataType : IEquatable<DbDataType>
	{
		public static readonly DbDataType Undefined = new (typeof(object), DataType.Undefined);

		[DebuggerStepThrough]
		public DbDataType(Type systemType) : this()
		{
			SystemType = systemType;
		}

		[DebuggerStepThrough]
		public DbDataType(Type systemType, DataType dataType) : this(systemType)
		{
			DataType   = dataType;
		}

		[DebuggerStepThrough]
		public DbDataType(Type systemType, DataType dataType, string? dbType) : this(systemType)
		{
			DataType   = dataType;
			DbType     = dbType;
		}

		[DebuggerStepThrough]
		public DbDataType(Type systemType, DataType dataType, string? dbType, int? length) : this(systemType)
		{
			DataType   = dataType;
			DbType     = dbType;
			Length     = length;
		}

		[DebuggerStepThrough]
		public DbDataType(Type systemType, DataType dataType, string? dbType, int? length, int? precision, int? scale) : this(systemType)
		{
			DataType  = dataType;
			DbType    = dbType;
			Length    = length;
			Precision = precision;
			Scale     = scale;
		}

		[DebuggerStepThrough]
		public DbDataType(Type systemType, string dbType) : this(systemType)
		{
			DbType = dbType;
		}

		public Type     SystemType { get; }
		public DataType DataType   { get; }
		public string?  DbType     { get; }
		public int?     Length     { get; }
		public int?     Precision  { get; }
		public int?     Scale      { get; }

		internal static MethodInfo WithSetValuesMethodInfo =
			MemberHelper.MethodOf<DbDataType>(dt => dt.WithSetValues(dt));

		internal static MethodInfo WithSystemTypeMethodInfo =
			MemberHelper.MethodOf<DbDataType>(dt => dt.WithSystemType(typeof(object)));

		public readonly DbDataType WithSetValues(DbDataType from)
		{
			return new DbDataType(
				from.SystemType != typeof(object)   ? from.SystemType : SystemType,
				from.DataType != DataType.Undefined ? from.DataType   : DataType,
				!string.IsNullOrEmpty(from.DbType)  ? from.DbType     : DbType,
				from.Length    ?? Length,
				from.Precision ?? Precision,
				from.Scale     ?? Scale);
		}

		public readonly DbDataType WithoutSystemType(DbDataType       from) => new (SystemType, from.DataType, from.DbType, from.Length, from.Precision, from.Scale);
		public readonly DbDataType WithoutSystemType(ColumnDescriptor from) => new (SystemType, from.DataType, from.DbType, from.Length, from.Precision, from.Scale);

		public readonly DbDataType WithSystemType    (Type     systemType           ) => new (systemType, DataType, DbType, Length, Precision, Scale);
		public readonly DbDataType WithDataType      (DataType dataType             ) => new (SystemType, dataType, DbType, Length, Precision, Scale);
		public readonly DbDataType WithDbType        (string?  dbName               ) => new (SystemType, DataType, dbName, Length, Precision, Scale);
		public readonly DbDataType WithLength        (int?     length               ) => new (SystemType, DataType, DbType, length, Precision, Scale);
		public readonly DbDataType WithPrecision     (int?     precision            ) => new (SystemType, DataType, DbType, Length, precision, Scale);
		public readonly DbDataType WithScale         (int?     scale                ) => new (SystemType, DataType, DbType, Length, Precision, scale);
		public readonly DbDataType WithPrecisionScale(int?     precision, int? scale) => new (SystemType, DataType, DbType, Length, precision, scale);

		public readonly override string ToString()
		{
			var dataTypeStr  = DataType == DataType.Undefined ? string.Empty : $", {DataType}";
			var dbTypeStr    = string.IsNullOrEmpty(DbType)   ? string.Empty : $", \"{DbType}\"";
			var lengthStr    = Length == null                 ? string.Empty : $", \"{Length}\"";
			var precisionStr = Precision == null              ? string.Empty : $", \"{Precision}\"";
			var scaleStr     = Scale == null                  ? string.Empty : $", \"{Scale}\"";
			return $"({SystemType}{dataTypeStr}{dbTypeStr}{lengthStr}{precisionStr}{scaleStr})";
		}

		#region Equality members

		public readonly bool Equals(DbDataType other)
		{
			return SystemType == other.SystemType
				&& DataType   == other.DataType
				&& Length     == other.Length
				&& Precision  == other.Precision
				&& Scale      == other.Scale
				&& string.Equals(DbType, other.DbType, StringComparison.Ordinal);
		}

		public readonly bool EqualsDbOnly(DbDataType other)
		{
			return DataType   == other.DataType
				&& Length     == other.Length
				&& Precision  == other.Precision
				&& Scale      == other.Scale
				&& string.Equals(DbType, other.DbType, StringComparison.Ordinal);
		}

		public override bool Equals(object? obj)
		{
			if (obj is null) return false;
			return obj is DbDataType type && Equals(type);
		}

		int? _hashCode;

		[SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
		public override int GetHashCode()
		{
			if (_hashCode != null)
				return _hashCode.Value;

			unchecked
			{
				var hashCode = (SystemType != null ? SystemType.GetHashCode() : 0);
				hashCode     = (hashCode * 397) ^ (int) DataType;
				hashCode     = (hashCode * 397) ^ (DbType    != null ? DbType.GetHashCode()          : 0);
				hashCode     = (hashCode * 397) ^ (Length    != null ? Length.Value.GetHashCode()    : 0);
				hashCode     = (hashCode * 397) ^ (Precision != null ? Precision.Value.GetHashCode() : 0);
				hashCode     = (hashCode * 397) ^ (Scale     != null ? Scale.Value.GetHashCode()     : 0);
				_hashCode    = hashCode;
			}

			return _hashCode.Value;
		}

#endregion

		#region Operators

		public static bool operator ==(DbDataType t1, DbDataType t2)
		{
			return t1.Equals(t2);
		}

		public static bool operator !=(DbDataType t1, DbDataType t2)
		{
			return !(t1 == t2);
		}

		#endregion
	}
}
