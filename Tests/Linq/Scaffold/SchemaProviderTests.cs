using System.Linq;

using LinqToDB.CodeModel;
using LinqToDB.Data;
using LinqToDB.Naming;
using LinqToDB.Scaffold;
using LinqToDB.Schema;

using NUnit.Framework;

using Shouldly;

namespace Tests.Scaffold
{
	[TestFixture]
	public class SchemaProviderTests : TestBase
	{
		[ActiveIssue(4444, ErrorTypeName = "System.InvalidOperationException", ErrorMessage = "IsResultDynamic set for function public.dblink",
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1897")]
		public void SqlServerProcedureReturnValue([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataConnection(context);

			var options = ScaffoldOptions.Default();
			options.Schema.LoadedObjects              = SchemaObjects.StoredProcedure;
			options.Schema.LoadStoredProcedure        = name => name.Name == "Issue1897";
			options.Schema.EnableSqlServerReturnValue = true;

			var provider  = new LegacySchemaProvider(db, options.Schema, LanguageProviders.CSharp);
			var procedure = ((ISchemaProvider)provider).GetProcedures(false, false).ShouldHaveSingleItem();

			procedure.Parameters.ShouldBeEmpty();
			procedure.Result.ShouldBeOfType<ScalarResult>().Name.ShouldBe("@return");

			var scaffolder = new Scaffolder(LanguageProviders.CSharp, HumanizerNameConverter.Instance, options, null);
			var model      = scaffolder.LoadDataModel(provider, provider).DataContext.StoredProcedures.ShouldHaveSingleItem();

			model.Parameters.ShouldBeEmpty();
			model.Return.ShouldNotBeNull().Direction.ShouldBe(System.Data.ParameterDirection.ReturnValue);
		}
	}
}
