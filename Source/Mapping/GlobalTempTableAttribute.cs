using System;

namespace LinqToDB.Mapping
{
	using SqlQuery;

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
	public class GlobalTempTableAttribute : TableTempTypeAttribute
	{
		public GlobalTempTableAttribute() : base(SqlTableTempType.GlobalTemp)
		{
		}
	}
}
