using System;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Method)]
	public class NorthwindDataContextAttribute : IncludeDataContextSourceAttribute
	{
		public NorthwindDataContextAttribute(bool excludeSqlite, bool excludeSqliteMs = false)
			: base(
				excludeSqlite ?
					new[] { TestProvName.Northwind } :
				excludeSqliteMs ?
					new[] { TestProvName.Northwind, TestProvName.NorthwindSQLite } :
					new[] { TestProvName.Northwind, TestProvName.NorthwindSQLite, TestProvName.NorthwindSQLiteMS })
		{
		}

		public NorthwindDataContextAttribute() : this(false)
		{
		}
	}
}
