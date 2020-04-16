using System;
using System.Diagnostics;
using LinqToDB.Mapping;

namespace LinqToDB.Common
{
	/// <summary>
	/// Stores database type attributes.
	/// </summary>
	public struct DbDataType
	{
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

		public DbDataType WithSetValues(DbDataType from)
		{
			return new DbDataType(
				SystemType,
				from.DataType != DataType.Undefined ? from.DataType : DataType,
				!from.DbType.IsNullOrEmpty()        ? from.DbType   : DbType,
				from.Length    ?? Length,
				from.Precision ?? Precision,
				from.Scale     ?? Scale);
		}


		public DbDataType WithoutSystemType(DbDataType       from) => new DbDataType(SystemType, from.DataType, from.DbType, from.Length, from.Precision, from.Scale);
		public DbDataType WithoutSystemType(ColumnDescriptor from) => new DbDataType(SystemType, from.DataType, from.DbType, from.Length, from.Precision, from.Scale);

		public DbDataType WithSystemType(Type     systemType) => new DbDataType(systemType, DataType, DbType, Length, Precision, Scale);
		public DbDataType WithDataType  (DataType dataType  ) => new DbDataType(SystemType, dataType, DbType, Length, Precision, Scale);
		public DbDataType WithDbType    (string?  dbName    ) => new DbDataType(SystemType, DataType, dbName, Length, Precision, Scale);
		public DbDataType WithLength    (int?     length    ) => new DbDataType(SystemType, DataType, DbType, length, Precision, Scale);
		public DbDataType WithPrecision (int?     precision ) => new DbDataType(SystemType, DataType, DbType, Length, precision, Scale);
		public DbDataType WithScale     (int?     scale     ) => new DbDataType(SystemType, DataType, DbType, Length, Precision, scale);

		public override string ToString()
		{
			var dataTypeStr  = DataType == DataType.Undefined ? string.Empty : $", {DataType}";
			var dbTypeStr    = string.IsNullOrEmpty(DbType)   ? string.Empty : $", \"{DbType}\"";
			var lengthStr    = Length == null                 ? string.Empty : $", \"{Length}\"";
			var precisionStr = Precision == null              ? string.Empty : $", \"{Precision}\"";
			var scaleStr     = Scale == null                  ? string.Empty : $", \"{Scale}\"";
			return $"{SystemType}{dataTypeStr}{dbTypeStr}{lengthStr}{precisionStr}{scaleStr}";
		}

		#region Equality members

		public bool Equals(DbDataType other)
		{
			return SystemType == other.SystemType
				&& DataType   == other.DataType
				&& Length     == other.Length
				&& Precision  == other.Precision
				&& Scale      == other.Scale
				&& string.Equals(DbType, other.DbType);
		}

		public override bool Equals(object? obj)
		{
			if (obj is null) return false;
			return obj is DbDataType type && Equals(type);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (SystemType != null ? SystemType.GetHashCode() : 0);
				hashCode     = (hashCode * 397) ^ (int) DataType;
				hashCode     = (hashCode * 397) ^ (DbType    != null ? DbType.GetHashCode()          : 0);
				hashCode     = (hashCode * 397) ^ (Length    != null ? Length.Value.GetHashCode()    : 0);
				hashCode     = (hashCode * 397) ^ (Precision != null ? Precision.Value.GetHashCode() : 0);
				hashCode     = (hashCode * 397) ^ (Scale     != null ? Scale.Value.GetHashCode()     : 0);
				return hashCode;
			}
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
