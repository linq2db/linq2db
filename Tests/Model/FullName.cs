using System;

using LinqToDB.Mapping;

namespace Tests.Model
{
	public class FullName
	{
		public string FirstName { get; set; } = null!;
		public string LastName = null!;
		[Nullable]
		public string? MiddleName;
	}
}
