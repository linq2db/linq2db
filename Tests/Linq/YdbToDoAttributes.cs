using System;

using LinqToDB;

#if NET8_0_OR_GREATER
using Ydb.Sdk.Ado;
#endif

namespace Tests
{
	public sealed class YdbUnexpectedSqlQueryAttribute : ThrowsForProviderAttribute
	{
		public YdbUnexpectedSqlQueryAttribute()
			: base(typeof(InvalidOperationException),
			ProviderName.Ydb)
		{
			ErrorMessage = "Unexpected table type SqlQuery";
		}
	}

	public sealed class YdbNotImplementedYetAttribute : ThrowsForProviderAttribute
	{
		public YdbNotImplementedYetAttribute()
			: base(typeof(NotImplementedException),
			ProviderName.Ydb)
		{
			ErrorMessage = "Not implemented yet";
		}
	}

	public sealed class YdbMemberNotFoundAttribute : ThrowsForProviderAttribute
	{
		public YdbMemberNotFoundAttribute()
#if NET8_0_OR_GREATER
			: base(typeof(YdbException),
#else
			: base(typeof(InvalidOperationException),
#endif
			ProviderName.Ydb)
		{
			ErrorMessage = "Error: Member not found";
		}
	}

	public sealed class YdbIntoValuesNotImplementedAttribute : ThrowsForProviderAttribute
	{
		public YdbIntoValuesNotImplementedAttribute()
#if NET8_0_OR_GREATER
			: base(typeof(YdbException),
#else
			: base(typeof(InvalidOperationException),
#endif
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
