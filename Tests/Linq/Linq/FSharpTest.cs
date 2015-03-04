using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;
	using VisualBasic;

	[TestFixture]
	public class FSharpTest : TestBase
	{
        [Test, DataContextSource]
		public void LoadSingle(string context)
        {
            using (var db = GetDataContext(context))
                Tests.FSharp.WhereTest.LoadSingle(db);
        }
	}
}
