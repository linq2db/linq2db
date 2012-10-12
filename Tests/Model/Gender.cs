using System;

using LinqToDB.Mapping;

namespace Tests.Model
{
	public enum Gender
	{
		[MapValueOld("M")] Male,
		[MapValueOld("F")] Female,
		[MapValueOld("U")] Unknown,
		[MapValueOld("O")] Other,
	}
}
