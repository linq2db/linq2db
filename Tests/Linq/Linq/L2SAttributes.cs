using System;
using System.Data.Linq.Mapping;

using LinqToDB;
using LinqToDB.Common;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[System.Data.Linq.Mapping.Table(Name = "Person")]
	public class L2SPersons
	{
		private int _personID;

		[System.Data.Linq.Mapping.Column(
			Storage       = "_personID",
			Name          = "PersonID",
			DbType        = "integer(32,0)",
			IsPrimaryKey  = true,
			IsDbGenerated = true,
			AutoSync      = AutoSync.Never,
			CanBeNull     = false)]
		public int PersonID
		{
			get { return _personID;  }
			set { _personID = value; }
		}
		[System.Data.Linq.Mapping.Column] public string FirstName { get; set; }
		[System.Data.Linq.Mapping.Column] public string LastName;
		[System.Data.Linq.Mapping.Column] public string MiddleName;
		[System.Data.Linq.Mapping.Column] public string Gender;
	}

	[TestFixture]
	public class L2SAttributes : TestBase
	{
		[Test]
		public void IsDbGeneratedTest()
		{
			using (var db = new TestDataConnection())
			{
				db.BeginTransaction();

				var id = db.InsertWithIdentity(new L2SPersons
				{
					FirstName = "Test",
					LastName  = "Test",
					Gender    = "M"
				});

				db.GetTable<L2SPersons>().Delete(p => p.PersonID == ConvertToOld<int>.From(id));
			}
		}
	}
}
