#if !TRAVIS
using System;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class FSharpTests : TestBase
	{
		[Test]
		public void LoadSingle([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				FSharp.WhereTest.LoadSingle(db);
		}

		[Test]
		public void LoadSinglesWithPatient([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				FSharp.WhereTest.LoadSinglesWithPatient( db);
		}

		[Test]
		public void LoadSingleWithOptions([DataSources] string context)
		{

			var ms = Tests.FSharp.MappingSchema.Initialize();

			using (var db = GetDataContext(context, ms))
				FSharp.WhereTest.LoadSingleWithOptions(db);
		}

		[Test]
		public void LoadSingleCLIMutable([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				FSharp.WhereTest.LoadSingleCLIMutable(db, null);
		}

		[Test]
		public void LoadSingleComplexPerson([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				FSharp.WhereTest.LoadSingleComplexPerson(db);
		}

		[Test]
		public void LoadSingleDeeplyComplexPerson([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				FSharp.WhereTest.LoadSingleDeeplyComplexPerson(db);
		}

		[Test]
		public void LoadColumnOfDeeplyComplexPerson([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				FSharp.WhereTest.LoadColumnOfDeeplyComplexPerson(db);
		}

		[Test]
		public void SelectField([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				FSharp.SelectTest.SelectField(db);
		}

		[Test, Ignore("Not currently supported")]
		public void SelectFieldDeeplyComplexPerson([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				FSharp.SelectTest.SelectFieldDeeplyComplexPerson(db);
		}

		[Test]
		public void Insert1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				FSharp.InsertTest.Insert1(db);
		}

		[Test, Ignore("It breaks following tests.")]
		public void Insert2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				FSharp.InsertTest.Insert2(db);
		}

		[ActiveIssue(416)]
		[Test]
		public void SelectLeftJoin([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				FSharp.SelectTest.SelectLeftJoin(db);
		}
	}
}
#endif
