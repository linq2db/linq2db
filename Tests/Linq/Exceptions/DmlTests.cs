﻿using LinqToDB;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Exceptions
{
	[TestFixture]
	public class DmlTests : TestBase
	{
		[Test]
		public void InsertOrUpdate1([InsertOrUpdateDataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();

				Assert.Throws<LinqToDBException>(
					() =>
						db.Doctor.InsertOrUpdate(
							() => new Doctor
							{
								PersonID = 10,
								Taxonomy = "....",
							},
							p => new Doctor
							{
								Taxonomy = "...",
							}),
					"InsertOrUpdate method requires the 'Doctor' table to have a primary key.");
			}
		}

		[Test]
		public void InsertOrUpdate2([InsertOrUpdateDataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();

				Assert.Throws<LinqToDBException>(
					() =>
						db.Patient.InsertOrUpdate(
							() => new Patient
							{
								Diagnosis = "....",
							},
							p => new Patient
							{
								Diagnosis = "...",
							}),
					"InsertOrUpdate method requires the 'Patient.PersonID' field to be included in the insert setter.");
			}
		}
	}
}
