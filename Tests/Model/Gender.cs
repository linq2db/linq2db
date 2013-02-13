using System;

using LinqToDB.Mapping;

namespace Tests.Model
{
	public enum Gender
	{
		[MapValueOld("M")] [MapValue("M")] Male,
		[MapValueOld("F")] [MapValue("F")] Female,
		[MapValueOld("U")] [MapValue("U")] Unknown,
		[MapValueOld("O")] [MapValue("O")] Other,
	}
}
