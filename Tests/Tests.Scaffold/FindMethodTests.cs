using System;
using System.Linq;

using LinqToDB.DataModel;

using NUnit.Framework;

using Shouldly;

using Tests.Scaffold.Harness;

using static Tests.Scaffold.Harness.TestSchema;

namespace Tests.Scaffold
{
	[TestFixture]
	public sealed class FindMethodTests
	{
		static readonly FindTypes[] _flags =
		[
			FindTypes.FindByPkOnTable      , FindTypes.FindAsyncByPkOnTable      , FindTypes.FindQueryByPkOnTable,
			FindTypes.FindByRecordOnTable  , FindTypes.FindAsyncByRecordOnTable  , FindTypes.FindQueryByRecordOnTable,
			FindTypes.FindByPkOnContext    , FindTypes.FindAsyncByPkOnContext    , FindTypes.FindQueryByPkOnContext,
			FindTypes.FindByRecordOnContext, FindTypes.FindAsyncByRecordOnContext, FindTypes.FindQueryByRecordOnContext,
		];

		// composite key, which order in key definition (Id, Code) differs from parameter names order (code, id)
		static TestSchema CreateSchema()
		{
			var schema = ProviderProfile.SQLite.CreateSchema();
			schema.AddTable("Item", [Col("Id", "int"), Col("Code", "text"), Col("Name", "text", nullable: true)], primaryKey: ["Id", "Code"]);
			return schema;
		}

		[Test]
		public void Flag([Values] ScaffoldLanguage language, [ValueSource(nameof(_flags))] FindTypes flag)
		{
			var model = ScaffoldHarness.Build(
				$"{nameof(FindMethodTests)}_{flag}",
				language,
				ProviderProfile.SQLite,
				CreateSchema(),
				options => options.DataModel.GenerateFindExtensions = flag);

			var method = ModelSmoke.FindMethods(model).ShouldHaveSingleItem();

			var parameters = method.GetParameters();

			(parameters[0].ParameterType == model.ContextType).ShouldBe(flag.HasFlag(FindTypes.OnContext));
			parameters.Any(p => p.ParameterType == model.GetType("Item")).ShouldBe(flag.HasFlag(FindTypes.ByEntity));
			method.Name.EndsWith("Async", StringComparison.Ordinal).ShouldBe(flag.HasFlag(FindTypes.Async));
			method.Name.EndsWith("Query", StringComparison.Ordinal).ShouldBe(flag.HasFlag(FindTypes.Query));

			using var db = new SqliteTestDb();

			// invokes method and asserts it finds row
			ModelSmoke.Run(model, db.Options).FindMethods.Count.ShouldBe(1);
		}

		[Test]
		public void CompositeKeyParameterOrder([Values] ScaffoldLanguage language, [Values] bool byOrdinal)
		{
			var model = ScaffoldHarness.Build(
				$"{nameof(CompositeKeyParameterOrder)}_{byOrdinal}",
				language,
				ProviderProfile.SQLite,
				CreateSchema(),
				options =>
				{
					options.DataModel.GenerateFindExtensions             = FindTypes.FindByPkOnTable;
					options.DataModel.OrderFindParametersByColumnOrdinal = byOrdinal;
				});

			var method = ModelSmoke.FindMethods(model).ShouldHaveSingleItem();

			// by ordinal: primary key definition order (Id, Code); otherwise by name (code, id)
			method.GetParameters().Skip(1).Select(static p => p.Name).ShouldBe(byOrdinal ? ["id", "code"] : ["code", "id"]);

			using var db = new SqliteTestDb();

			ModelSmoke.Run(model, db.Options).FindMethods.Count.ShouldBe(1);
		}
	}
}
