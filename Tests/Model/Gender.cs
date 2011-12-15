using System;

using LinqToDB.Mapping;

namespace Tests.Model
{
	public enum Gender
	{
		[MapValue("M")] Male,
		[MapValue("F")] Female,
		[MapValue("U")] Unknown,
		[MapValue("O")] Other,
	}
}
