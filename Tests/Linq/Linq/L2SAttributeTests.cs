#if NETFRAMEWORK
using System.Data.Linq.Mapping;
using ColumnAttribute = System.Data.Linq.Mapping.ColumnAttribute;
using TableAttribute = System.Data.Linq.Mapping.TableAttribute;
#else
using ColumnAttribute = System.ComponentModel.DataAnnotations.Schema.ColumnAttribute;
using TableAttribute = System.ComponentModel.DataAnnotations.Schema.TableAttribute;
#endif

using LinqToDB;
using LinqToDB.Common;

using NUnit.Framework;

namespace Tests.Linq
{
	using LinqToDB.Metadata;
	using Model;

#if NETFRAMEWORK
	[Table(Name = "Person")]
#else
	[Table("Person")]
#endif
	public class L2SPersons
	{
		private int _personID;

#if NETFRAMEWORK
		[Column(
			Storage       = "_personID",
			Name          = "PersonID",
			DbType        = "integer(32,0)",
			IsPrimaryKey  = true,
			IsDbGenerated = true,
			AutoSync      = AutoSync.Never,
			CanBeNull     = false)]
#else
		[Column("PersonID",
			TypeName      = "integer(32,0)")]
#endif
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
			ms.AddMetadataReader(new SystemComponentModelDataAnnotationsSchemaAttributeReader());
#if NETFRAMEWORK
			ms.AddMetadataReader(new SystemDataLinqAttributeReader());
#endif

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
