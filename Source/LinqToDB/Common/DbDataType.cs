using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace LinqToDB.Common
{
	public struct DbDataType
	{
		[DebuggerStepThrough]
		public DbDataType([NotNull] Type systemType) : this()
		{
			SystemType = systemType;
		}

		public override string ToString()
		{
			var dataTypeStr = DataType == DataType.Undefined ? string.Empty : $", {DataType}";
			var dbTypeStr   = string.IsNullOrEmpty(DbType)   ? string.Empty : $", \"{DbType}\"";
			var lengthStr   = Length == null                 ? string.Empty : $", \"{Length}\"";
			return $"{SystemType}{dataTypeStr}{dbTypeStr}{lengthStr}";
		}

		[DebuggerStepThrough]
		public DbDataType(Type systemType, DataType dataType) : this(systemType)
		{
			DataType   = dataType;
		}

		[DebuggerStepThrough]
		public DbDataType(Type systemType, DataType dataType, string dbType) : this(systemType)
		{
			DataType   = dataType;
			DbType     = dbType;
		}

		[DebuggerStepThrough]
		public DbDataType(Type systemType, DataType dataType, string dbType, int? length) : this(systemType)
		{
			DataType   = dataType;
			DbType     = dbType;
			Length     = length;
		}

		[DebuggerStepThrough]
		public DbDataType(Type systemType, string dbType) : this(systemType)
		{
			DbType = dbType;
		}

		public Type     SystemType { get; }
		public DataType DataType   { get; }
		public string   DbType     { get; }
		public int?     Length     { get; }

		public DbDataType WithSystemType(Type     systemType) => new DbDataType(systemType, DataType, DbType, Length);
		public DbDataType WithDataType  (DataType dataType  ) => new DbDataType(SystemType, dataType, DbType, Length);
		public DbDataType WithDbType    (string   dbName    ) => new DbDataType(SystemType, DataType, dbName, Length);
		public DbDataType WithLength    (int?     length    ) => new DbDataType(SystemType, DataType, DbType, length);

		#region Equality members

		public bool Equals(DbDataType other)
		{
			return SystemType == other.SystemType && DataType == other.DataType && string.Equals(DbType, other.DbType) && Length == other.Length;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is DbDataType && Equals((DbDataType) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (SystemType != null ? SystemType.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (int) DataType;
				hashCode = (hashCode * 397) ^ (DbType != null ? DbType.GetHashCode()       : 0);
				hashCode = (hashCode * 397) ^ (Length != null ? Length.Value.GetHashCode() : 0);
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
