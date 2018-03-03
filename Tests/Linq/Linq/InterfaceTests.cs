using System;
using System.Linq;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class InterfaceTests : TestBase
	{
		[Test, DataContextSource]
		public void Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Parent2
					group p by p.ParentID into gr
					select new
					{
						Count = gr.Count()
					};

				q.ToList();
			}
		}
	}
}
