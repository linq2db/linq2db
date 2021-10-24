using System;

namespace LinqToDB.DataProvider.SqlServer
{
	public enum TableHint
	{
		ForceScan,
		ForceSeek,
		HoldLock,
		NoLock,
		NoWait,
		PagLock,
		ReadCommitted,
		ReadCommittedLock,
		ReadPast,
		ReadUncommitted,
		RepeatableRead,
		RowLock,
		Serializable,
		Snapshot,
		SpatialWindowMaxCells,
		TabLock,
		TabLockX,
		UpdLock,
		XLock,
	}
}
