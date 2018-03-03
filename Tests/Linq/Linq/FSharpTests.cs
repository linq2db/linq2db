﻿#if !TRAVIS
using System;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class FSharpTests : TestBase
	{
		[Test, DataContextSource]
		public void LoadSingle(string context)
		{
			using (var db = GetDataContext(context))
				FSharp.WhereTest.LoadSingle(db);
		}

		[Test, DataContextSource]
		public void LoadSinglesWithPatient( string context)
		{
			using (var db = GetDataContext(context))
				FSharp.WhereTest.LoadSinglesWithPatient( db);
		}

		[Test, DataContextSource]
		public void LoadSingleWithOptions(string context)
		{

			var ms = Tests.FSharp.MappingSchema.Initialize();

			using (var db = GetDataContext(context, ms))
				FSharp.WhereTest.LoadSingleWithOptions(db);
		}

		[Test, DataContextSource]
		public void LoadSingleCLIMutable(string context)
		{
			using (var db = GetDataContext(context))
				FSharp.WhereTest.LoadSingleCLIMutable(db, null);
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
	}
}
#endif
