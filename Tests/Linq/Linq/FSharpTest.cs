using System;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class FSharpTest : TestBase
	{
		[Test, DataContextSource]
		public void LoadSingle(string context)
		{
			using (var db = GetDataContext(context))
				FSharp.WhereTest.LoadSingle(db);
		}

		[Test, DataContextSource]
		public void LoadSingleComplexPerson(string context)
		{
			using (var db = GetDataContext(context))
				FSharp.WhereTest.LoadSingleComplexPerson(db);
		}

		[Test, DataContextSource]
		public void LoadSingleDeeplyComplexPerson(string context)
		{
			using (var db = GetDataContext(context))
				FSharp.WhereTest.LoadSingleDeeplyComplexPerson(db);
		}

        [Test, DataContextSource]
        public void LoadByOption(string context)
        {
            using (var db = GetDataContext(context))
                FSharp.WhereTest.LoadByOption(db);
        }

		[Test, DataContextSource]
		public void LoadColumnOfDeeplyComplexPerson(string context)
		{
			using (var db = GetDataContext(context))
				FSharp.WhereTest.LoadColumnOfDeeplyComplexPerson(db);
		}

		[Test, DataContextSource]
		public void SelectField(string context)
		{
			using (var db = GetDataContext(context))
				FSharp.SelectTest.SelectField(db);
		}

		[Test, DataContextSource, Ignore("Not currently supported")]
		public void SelectFieldDeeplyComplexPerson(string context)
		{
			using (var db = GetDataContext(context))
				FSharp.SelectTest.SelectFieldDeeplyComplexPerson(db);
		}

		[Test, DataContextSource]
		public void Insert1(string context)
		{
			using (var db = GetDataContext(context))
				FSharp.InsertTest.Insert1(db);
		}

		[Test, DataContextSource, Ignore("It breaks following tests.")]
		public void Insert2(string context)
		{
			using (var db = GetDataContext(context))
				FSharp.InsertTest.Insert2(db);
		}

        [Test, DataContextSource]
        public void InsertNoneOption(string context)
        {
            using (var db = GetDataContext(context))
                FSharp.InsertTest.InsertNoneOption(db);
        }

        [Test, DataContextSource]
        public void InsertSomeOption(string context)
        {
            using (var db = GetDataContext(context))
                FSharp.InsertTest.InsertSomeOption(db);
        }

        [Test]
        public void MapSomeType()
        {
            FSharp.MappingTest.MapSomeType();
        }
	}
}
