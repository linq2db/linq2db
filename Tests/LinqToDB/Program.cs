using System;

using DataModel;

namespace T4Model.LinqToDB
{
	class Program
	{
		static void Main(string[] args)
		{
			using (var dc = new TestDataDB("TestData"))
			{
				var int1 = (int?)1;
				var str2 = "2";

				dc.Scalar_OutputParameter(ref int1, ref str2);
			}
		}
	}
}
