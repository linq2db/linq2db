using System;

namespace LinqToDB.Mapping
{
	using SqlQuery;

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
	public class TableTempTypeAttribute : Attribute
	{
		public TableTempTypeAttribute()
		{
		}

		public TableTempTypeAttribute(SqlTableTempType sqlTableTempType) : this()
		{
			SqlTableTempType = sqlTableTempType;
		}

		public string           Configuration    { get; set; }
		public SqlTableTempType SqlTableTempType { get; set; }
	}
}
