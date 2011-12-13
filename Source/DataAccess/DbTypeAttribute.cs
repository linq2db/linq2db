using System;
using System.Data;

namespace LinqToDB.DataAccess
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false), CLSCompliant(false)]
	public sealed class DbTypeAttribute : Attribute
	{
		public DbType DbType { get; set; }
		public int?   Size   { get; set; }

		public DbTypeAttribute(DbType sqlDbType)
		{
			DbType = sqlDbType;
		}

		public DbTypeAttribute(DbType sqlDbType, int size)
		{
			DbType = sqlDbType;
			Size   = size;
		}
	}
}
