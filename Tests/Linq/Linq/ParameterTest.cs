using System;
using System.Linq;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class ParameterTest : TestBase
	{
		[Test, DataContextSource]
		public void InlineParameter(string context)
		{
			using (var  db = GetDataContext(context))
			{
				db.InlineParameters = true;

				var id = 1;

				var parent1 = db.Parent.FirstOrDefault(p => p.ParentID == id);
				id++;
				var parent2 = db.Parent.FirstOrDefault(p => p.ParentID == id);

				Assert.That(parent1.ParentID, Is.Not.EqualTo(parent2.ParentID));
			}
		}
	}
}
