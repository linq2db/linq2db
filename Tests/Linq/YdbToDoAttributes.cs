#if NET8_0_OR_GREATER
using System;

using LinqToDB;

using Ydb.Sdk.Ado;

namespace Tests
{
	public sealed class YdbTableNotFoundAttribute : ThrowsForProviderAttribute
	{
		public YdbTableNotFoundAttribute()
			: base(typeof(LinqToDBException),
			ProviderName.Ydb)
		{
			ErrorMessage = "Table not found for";
		}
	}

	public sealed class YdbUnexpectedSqlQueryAttribute : ThrowsForProviderAttribute
	{
		public YdbUnexpectedSqlQueryAttribute()
			: base(typeof(InvalidOperationException),
			ProviderName.Ydb)
		{
			ErrorMessage = "Unexpected table type SqlQuery";
		}
	}

	public sealed class YdbMemberNotFoundAttribute : ThrowsForProviderAttribute
	{
		public YdbMemberNotFoundAttribute()
			: base(typeof(YdbException),
			ProviderName.Ydb)
		{
			ErrorMessage = "Error: Member not found";
		}
	}

	public sealed class YdbIntoValuesNotImplementedAttribute : ThrowsForProviderAttribute
	{
		public YdbIntoValuesNotImplementedAttribute()
			: base(typeof(YdbException),
			ProviderName.Ydb)
		{
			ErrorMessage = "into_values_source: alternative is not implemented yet";
		}
	}

	public sealed class YdbCteAsSourceAttribute : ThrowsForProviderAttribute
	{
		public YdbCteAsSourceAttribute()
			: base(typeof(InvalidCastException),
			ProviderName.Ydb)
		{
			ErrorMessage = "Unable to cast object of type 'LinqToDB.Internal.SqlQuery.SqlCteTable' to type 'LinqToDB.Internal.SqlQuery.SelectQuery'";
		}
	}
}
#endif
