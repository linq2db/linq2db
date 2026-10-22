using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.Firebird;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue6101Tests : TestBase
	{
		sealed class CharTable
		{
			[Column] public char Ch { get; set; }
		}

		// SQL generation only: no Firebird server is needed.
		static DataConnection CreateConnection()
			=> new(new DataOptions().UseConnectionString(FirebirdTools.GetDataProvider(FirebirdVersion.v5), "Database=fake"));

		[Test]
		public void CharConstantQuoteIsEscaped()
		{
			using var db = CreateConnection();

			var sql = db.GetTable<CharTable>().Where(x => x.Ch == '\'').ToSqlQuery().Sql;

			sql.ShouldContain("= ''''");
			sql.ShouldNotContain("= ''' ");
		}

		[Test]
		public void CharInlinedParameterQuoteIsEscaped()
		{
			using var db = CreateConnection();
			db.InlineParameters = true;

			var ch  = '\'';
			var sql = db.GetTable<CharTable>().Where(x => x.Ch == ch).ToSqlQuery().Sql;

			sql.ShouldContain("= ''''");
		}
	}
}
