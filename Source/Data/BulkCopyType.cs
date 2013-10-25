using System;

namespace LinqToDB.Data
{
	public enum BulkCopyType
	{
		Default = 0,
		RowByRow,
		MultipleRows,
		ProviderSpecific
	}
}
