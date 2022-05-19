using System;

namespace LinqToDB.Linq;

[Flags]
internal enum QueryFlags
{
	None                 = 0,
	/// <summary>
	/// Bit set, when <see cref="Common.Configuration.Linq.GuardGrouping"/> enabled for connection.
	/// </summary>
	GroupByGuard         = 0x01,
	/// <summary>
	/// Bit set, when inline parameters enabled for connection.
	/// </summary>
	InlineParameters     = 0x02,
	/// <summary>
	/// Bit set, when <see cref="Common.Configuration.Linq.ParameterizeTakeSkip"/> is enabled for query.
	/// </summary>
	ParameterizeTakeSkip = 0x04,
	/// <summary>
	/// Bit set, when <see cref="Common.Configuration.Linq.PreferApply"/> is enabled for query.
	/// </summary>
	PreferApply          = 0x08,
	/// <summary>
	/// BIt set, when <see cref="Common.Configuration.Linq.CompareNullsAsValues"/> is enabled for query.
	/// </summary>
	CompareNullsAsValues = 0x10,
}
