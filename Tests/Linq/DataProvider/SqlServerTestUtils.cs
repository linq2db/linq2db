using System.Collections.Generic;
using System.Data;

using Microsoft.SqlServer.Server;

using SqlDataRecordMS = Microsoft.Data.SqlClient.Server.SqlDataRecord;
using SqlMetaDataMS = Microsoft.Data.SqlClient.Server.SqlMetaData;

namespace Tests.DataProvider
{
	internal static class SqlServerTestUtils
	{
		public sealed class TVPRecord
		{
			public int? Id { get; set; }

			public string? Name { get; set; }
		}

		public static TVPRecord[] TestUDTData = new[]
		{
			new TVPRecord(),
			new TVPRecord() { Id = 1, Name = "Value1" },
			new TVPRecord() { Id = 2, Name = "Value2" }
		};

		public static IEnumerable<SqlDataRecordMS> GetSqlDataRecordsMS()
		{
			var sqlRecord = new SqlDataRecordMS(
				new SqlMetaDataMS("Id", SqlDbType.Int),
				new SqlMetaDataMS("Name", SqlDbType.NVarChar, 10));

			foreach (var record in TestUDTData)
			{
				sqlRecord.SetValue(0, record.Id);
				sqlRecord.SetValue(1, record.Name);

				yield return sqlRecord;
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public static IEnumerable<SqlDataRecord> GetSqlDataRecords()
		{
			var sqlRecord = new SqlDataRecord(
				new SqlMetaData("Id",   SqlDbType.Int),
				new SqlMetaData("Name", SqlDbType.NVarChar, 10));
#pragma warning restore CS0618 // Type or member is obsolete

			foreach (var record in TestUDTData)
			{
				sqlRecord.SetValue(0, record.Id);
				sqlRecord.SetValue(1, record.Name);

				yield return sqlRecord;
			}
		}


	}
}
