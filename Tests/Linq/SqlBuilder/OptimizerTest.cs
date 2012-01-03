using System;

using NUnit.Framework;

namespace Tests.SqlBuilder
{
	[TestFixture]
	public class OptimizerTest
	{
		static void TTT()
		{
			var n = (LinqToDB_Temp.SqlBuilder.SqlDataType)null;
			n.Equals(null);
		}

		[Test] public void ConvertString() { Nemerle.OptimizerTest.ConvertString(); }
		[Test] public void ConvertNumber() { Nemerle.OptimizerTest.ConvertNumber(); }
		[Test] public void ConvertCase  () { Nemerle.OptimizerTest.ConvertCase  (); }
	}
}
