#if NETFRAMEWORK
using System.Data.Linq.Mapping;

using LinqToDB;
using LinqToDB.Common;

using NUnit.Framework;

using ColumnAttribute = System.Data.Linq.Mapping.ColumnAttribute;
using TableAttribute = System.Data.Linq.Mapping.TableAttribute;

namespace Tests.Linq
{
	using LinqToDB.Metadata;

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
		[Column]
		public string FirstName { get; set; } = null!;

		[Column]
		public string LastName = null!;

		[Column]
		public string? MiddleName;

		[Column]
		public string Gender = null!;
	}

	[TestFixture]
	public class L2SAttributeTests : TestBase
	{
		[Test]
		public void IsDbGeneratedTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ms = new LinqToDB.Mapping.MappingSchema();
			ms.AddMetadataReader(new SystemDataLinqAttributeReader());

			ResetPersonIdentity(context);

			using (var db = GetDataContext(context, ms))
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
#endif
