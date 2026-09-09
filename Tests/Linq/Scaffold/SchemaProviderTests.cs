using System.Linq;

using LinqToDB.CodeModel;
using LinqToDB.Data;
using LinqToDB.Scaffold;
using LinqToDB.Schema;

using NUnit.Framework;

namespace Tests.Scaffold
{
	[TestFixture]
	public class SchemaProviderTests : TestBase
	{
		[ActiveIssueNew(4444, ErrorTypeName = "System.InvalidOperationException", ErrorMessage = "IsResultDynamic set for function public.dblink",
			Details = "Issue number taken from the test's own Description, which the bare attribute did not carry. The schema provider refuses dblink's record-returning overloads instead of describing them - #4444's subject.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4444")]
		public void Issue4444Test([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataConnection(context);

			db.Execute("CREATE EXTENSION dblink");

			try
			{
				var options = ScaffoldOptions.Default().Schema;
				options.LoadProceduresSchema = true;
				options.LoadedObjects = SchemaObjects.StoredProcedure | SchemaObjects.ScalarFunction | SchemaObjects.AggregateFunction | SchemaObjects.TableFunction;

				ISchemaProvider provider = new LegacySchemaProvider(db, options, LanguageProviders.CSharp);

				provider.GetProcedures(true, false).ToArray();
				provider.GetTableFunctions().ToArray();
				provider.GetScalarFunctions().ToArray();
				provider.GetAggregateFunctions().ToArray();
			}
			finally
			{
				db.Execute("DROP EXTENSION IF EXISTS dblink");
			}
		}
	}
}
