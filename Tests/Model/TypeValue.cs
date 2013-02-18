using System;

using LinqToDB.Mapping;

namespace Tests.Model
{
	public enum TypeValue
	{
		[MapValue(null)] [NullValue]
		Value0 = 0,
		[MapValue(1)] Value1 = 10,
		[MapValue(2)] Value2 = 2,
		[MapValue(3)] Value3 = 3,
		[MapValue(4)] Value4 = 4,
		[MapValue(5)] Value5 = 5,
		[MapValue(6)] Value6 = 6
	}
}
