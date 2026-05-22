using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Metadata;

using NUnit.Framework;

using Tests.Model;

using ColumnAttribute = System.ComponentModel.DataAnnotations.Schema.ColumnAttribute;
using TableAttribute = System.ComponentModel.DataAnnotations.Schema.TableAttribute;

namespace Tests.Linq
{
	[Table("Person")]
	public class L2DAPersons
	{
		private int _personID;

		[Column("PersonID", TypeName = "integer(32,0)")]
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
	public class L2DAAttributeTests : TestBase
	{
		[Test]
		public void IsDbGeneratedTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ms = new LinqToDB.Mapping.MappingSchema();
			ms.AddMetadataReader(new SystemComponentModelDataAnnotationsSchemaAttributeReader());

			ResetPersonIdentity(context);

			using var db = GetDataContext(context, ms);
			db.BeginTransaction();

			var id = db.InsertWithIdentity(new L2DAPersons
			{
				FirstName = "Test",
				LastName  = "Test",
				Gender    = "M"
			});

			db.GetTable<L2DAPersons>().Delete(p => p.PersonID == ConvertTo<int>.From(id));
		}
	}
}
