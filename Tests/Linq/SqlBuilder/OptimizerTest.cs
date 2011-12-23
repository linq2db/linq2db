using System;

using NUnit.Framework;

namespace Tests.SqlBuilder
{
	[TestFixture]
	public class OptimizerTest
	{
		[Test] public void ConvertString() { Nemerle.OptimizerTest.ConvertString(); }
		[Test] public void ConvertNumber() { Nemerle.OptimizerTest.ConvertNumber(); }
	}
}
