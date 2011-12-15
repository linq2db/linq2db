using System;
using System.Data.Linq.Mapping;

using LinqToDB.Common;
using LinqToDB.Data.Linq;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[Table(Name = "Person")]
	public class L2SPersons
	{
		private int _personID;

		[Column(
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
		[Column] public string FirstName { get; set; }
		[Column] public string LastName;
		[Column] public string MiddleName;
		[Column] public string Gender;
	}

	[TestFixture]
	public class L2SAttributes : TestBase
	{
		[Test]
		public void IsDbGeneratedTest()
		{
			using (var db = new TestDbManager("Sql2008"))
			{
				db.BeginTransaction();

				var id = db.InsertWithIdentity(new L2SPersons
				{
					FirstName = "Test",
					LastName  = "Test",
					Gender    = "M"
				});

				db.GetTable<L2SPersons>().Delete(p => p.PersonID == ConvertTo<int>.From(id));
			}
		}
	}
}
