using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.Firebird;
using LinqToDB.Internal.DataProvider.Firebird;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue982Tests : TestBase
	{
		private sealed class Issue982FirebirdSqlOptimizer : FirebirdSqlOptimizer
		{
			public Issue982FirebirdSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
			{
			}

			public override SqlStatement Finalize(MappingSchema mappingSchema, SqlStatement statement, DataOptions dataOptions)
			{
				statement = base.Finalize(mappingSchema, statement, dataOptions);

				AddConditions(statement);

				return statement;
			}

			private void AddConditions(SqlStatement statement)
			{
				if (statement.SelectQuery?.Where.IsEmpty == false)
					statement.SelectQuery.Where.SearchCondition.Add(new SqlPredicate.Expr(
						new SqlExpression(typeof(bool), "'one' != 'two'", Precedence.Comparison, SqlFlags.IsPredicate, ParametersNullabilityType.IfAllParametersNullable, null), 0));
			}
		}

		sealed class Issue982FirebirdDataProvider(string name, FirebirdVersion version, ISqlOptimizer sqlOptimizer) : FirebirdDataProvider(name, version)
		{
			public override ISqlOptimizer GetSqlOptimizer(DataOptions dataOptions) => sqlOptimizer;
		}

		[Test(Description = "Ensure we can subclass and replace sql optimizer")]
		public void Test([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			var connectionString = DataConnection.GetConnectionString(context);
			var oldProvider      = (FirebirdDataProvider)DataConnection.GetDataProvider(context);

			try
			{
				DataConnection.AddConfiguration(
					context,
					connectionString,
					new Issue982FirebirdDataProvider(oldProvider.Name, oldProvider.Version, new Issue982FirebirdSqlOptimizer(oldProvider.SqlProviderFlags)));

				using (var db = GetDataContext(context))
				{
					var query = from p in db.Parent
								from c in db.Child.InnerJoin(c => c.ParentID == p.ParentID)
								from cg in (
									from cc in db.Child
									group cc by cc.ChildID
									into g
									select g.Key
								).InnerJoin(cg => c.ChildID == cg)
								where p.ParentID > 1 || p.ParentID > 0
								select new
								{
									p,
									c
								};

					Assert.That(query.ToSqlQuery().Sql, Does.Contain("'one' != 'two'"));

					var _ = query.ToArray();
				}
			}
			finally
			{
				// restore
				DataConnection.AddConfiguration(context, connectionString, oldProvider);
			}
		}
	}
}
