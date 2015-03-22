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

        [Test, DataContextSource]
        public void LoadSingleComplexPerson(string context)
        {
            using (var db = GetDataContext(context))
                Tests.FSharp.WhereTest.LoadSingleComplexPerson(db);
        }

        [Test, DataContextSource]
        public void LoadSingleDeeplyComplexPerson(string context)
        {
            using (var db = GetDataContext(context))
                Tests.FSharp.WhereTest.LoadSingleDeeplyComplexPerson(db);
        }

        [Test, DataContextSource]
        public void LoadColumnOfDeeplyComplexPerson(string context)
        {
            using (var db = GetDataContext(context))
                Tests.FSharp.WhereTest.LoadColumnOfDeeplyComplexPerson(db);
        }

        [Test, DataContextSource]
        public void SelectField(string context)
        {
            using (var db = GetDataContext(context))
                Tests.FSharp.SelectTest.SelectField(db);
        }

        [Test, DataContextSource, Ignore("Not currently supported")]
        public void SelectFieldDeeplyComplexPerson(string context)
        {
            using (var db = GetDataContext(context))
                Tests.FSharp.SelectTest.SelectFieldDeeplyComplexPerson(db);
        }

        [Test, DataContextSource]
        public void Insert1(string context)
        {
            using (var db = GetDataContext(context))
                Tests.FSharp.InsertTest.Insert1(db);
        }

        [Test, DataContextSource]
        public void Insert2(string context)
        {
            using (var db = GetDataContext(context))
                Tests.FSharp.InsertTest.Insert2(db);
        }

    }

}
