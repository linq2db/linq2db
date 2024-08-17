using System;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	public static class ExceptionExtensions
	{
		public static Unit Throw(this Exception e) => throw e;
	}
}
