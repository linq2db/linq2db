using System;

using LinqToDB.Mapping;

namespace Tests.Model
{
	public class FullName
	{
		public string FirstName { get; set; }
		public string LastName;
		[Nullable]
		public string MiddleName;
	}
}