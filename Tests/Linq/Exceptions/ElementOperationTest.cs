using System;
using System.Linq;

using NUnit.Framework;

namespace Tests.Exceptions
{
	[TestFixture]
	public class ElementOperationTest : TestBase
	{
		[Test, ExpectedException(typeof(InvalidOperationException))]
		public void First()
		{
			ForEachProvider(typeof(InvalidOperationException), db => db.Parent.First(p => p.ParentID == 100));
		}

		[Test, ExpectedException(typeof(InvalidOperationException))]
		public void Single()
		{
			ForEachProvider(typeof(InvalidOperationException), db => db.Parent.Single());
		}
	}
}
