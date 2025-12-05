using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;

using Microsoft.Data.SqlTypes;

using NUnit.Framework;

namespace Tests.DataProvider
{
	// TODO: add more types
	public sealed  class MySqlTypeTestsTypeTests : TypeTestsBase
	{
		[Test]
		public async ValueTask TestVectorType([IncludeDataSources(TestProvName.AllMySql8Plus)] string context)
		{
			// https://dev.mysql.com/doc/relnotes/mysql/9.0/en/news-9-0-0.html#mysqld-9-0-0-vectors
			// https://mariadb.com/docs/server/reference/sql-structure/vectors/vector

			var mariaDB = context.IsAnyOf(TestProvName.AllMariaDB);

			var dt = DataType.Vector32;

			var asArray1 = new float[] { 1.2f, -1.1f };
			var asArray2 = new float[] { 5.2f, -3.1f };
			var asArray3 = new float[] { 11.2f, -4.1f };

			// no length
			if (!mariaDB)
			{
				await TestType<float[], float[]?>(context, new DbDataType(typeof(float[])), asArray1, default, filterByValue: false, testBulkCopyType: bulkCopyTypeAllowed);
				await TestType<float[], float[]?>(context, new DbDataType(typeof(float[])), asArray2, asArray1, filterByValue: false, filterByNullableValue: false, testBulkCopyType: bulkCopyTypeAllowed);
			}

			// no expicit DataType
			await TestType<float[], float[]?>(context, new DbDataType(typeof(float[])).WithLength(2), asArray1, default, filterByValue: false, testBulkCopyType: bulkCopyTypeAllowed);
			await TestType<float[], float[]?>(context, new DbDataType(typeof(float[])).WithLength(2), asArray2, asArray1, filterByValue: false, filterByNullableValue: false, testBulkCopyType: bulkCopyTypeAllowed);

			await TestType<float[], float[]?>(context, new DbDataType(typeof(float[]), dt).WithLength(2), asArray1, default, filterByValue: false, testBulkCopyType: bulkCopyTypeAllowed);
			await TestType<float[], float[]?>(context, new DbDataType(typeof(float[]), dt).WithLength(2), asArray2, asArray1, filterByValue: false, filterByNullableValue: false, testBulkCopyType: bulkCopyTypeAllowed);

			// MySql cannot handle bulk copy now: https://github.com/mysql-net/MySqlConnector/issues/1604#issuecomment-3451624327
			bool bulkCopyTypeAllowed(BulkCopyType type)
				=> type != BulkCopyType.ProviderSpecific
				|| context.IsAnyOf(TestProvName.AllMariaDB);
		}
	}
}
