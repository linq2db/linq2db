using System;

using NUnit.Framework;

namespace Tests.Macros
{
	[TestFixture]
	public class InfoOfTest
	{
		[Test] public void FieldOf   () { Nemerle.InfoOfTest.FieldOf     (); }
		[Test] public void PropertyOf() { Nemerle.InfoOfTest.PropertyOf  (); }
		[Test] public void MethodOf  () { Nemerle.InfoOfTest.TestMethodOf(); }
	}
}
