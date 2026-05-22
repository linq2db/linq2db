using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1721Tests : TestBase
	{
		public class I1721Model
		{
			#region Specified default values
			// Expect: "DateTime2"
			[Column(DataType = DataType.DateTime2, Precision = 7), NotNull]
			public DateTime TestDateTime2 { get; set; }

			// Expect: "DateTimeOffset"
			[Column(DataType = DataType.DateTimeOffset, Precision = 7), NotNull]
			public DateTimeOffset TestDateTimeOffset { get; set; }

			// Expect: "Time"
			[Column(DataType = DataType.Time, Precision = 7), NotNull]
			public TimeSpan TestTime { get; set; }
			#endregion

			// Expect: "DateTime2"
			[Column(DataType = DataType.DateTime2), NotNull]
			public DateTime TestDefaultPrecision { get; set; }

			// Expect: "DateTime2(0)"
			[Column(DataType = DataType.DateTime2, Precision = 0), NotNull]
			public DateTime TestNonDefaultPrecision { get; set; }

			// Expect: "DateTime2(1)"
			[Column(DataType = DataType.DateTime2, Precision = 1), NotNull]
			public DateTime TestNonZeroPrecision { get; set; }
		}

		[Test]
		public void Issue1721Test([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			// Create table and get SQL.
			string createSQL = "";
			using (var db = (DataConnection)GetDataContext(context))
			{
				Assert.DoesNotThrow(() => createSQL = GetCreateTableSQL<I1721Model>(db));
			}

			// Parse field name and data type from SQL.
			var fields = GetFieldDataTypes(createSQL);
			Assert.That(fields, Has.Count.EqualTo(6));
			using (Assert.EnterMultipleScope())
			{
				// Check that correct data types were generated.
				Assert.That(fields["TestDateTime2"], Is.EqualTo("DateTime2"));
				Assert.That(fields["TestDateTimeOffset"], Is.EqualTo("DateTimeOffset"));
				Assert.That(fields["TestTime"], Is.EqualTo("Time"));
				Assert.That(fields["TestDefaultPrecision"], Is.EqualTo("DateTime2"));
				Assert.That(fields["TestNonDefaultPrecision"], Is.EqualTo("DateTime2(0)"));
				Assert.That(fields["TestNonZeroPrecision"], Is.EqualTo("DateTime2(1)"));
			}
		}

		/// <summary>
		/// Create temporary table and return generated SQL.
		/// </summary>
		/// <typeparam name="T">Type of table to create.</typeparam>
		/// <param name="connection"><see cref="DataConnection"/> to create temporary table in.</param>
		/// <returns>SQL used to create the table.</returns>
		private static string GetCreateTableSQL<T>(DataConnection connection)
			where T : class
		{
			using var temp = connection.CreateTempTable<T>();
			return connection.LastQuery!;
		}

		static Regex FieldRE = new Regex(@"^\s+\[?([^\] ]+)\]?\s+([^ ]*(?:Time|Date)[^ ]*)\s+(NOT NULL|NULL),?$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

		/// <summary>
		/// Parse CREATE TABLE SQL statement to extract field name and data type.
		/// </summary>
		/// <param name="sql">CREATE TABLE SQL statement.</param>
		/// <returns><see cref="Dictionary{String, String}"/> of data type keyed by field name.</returns>
		static Dictionary<string, string> GetFieldDataTypes(string sql)
		{
			var fields =
				sql.Replace("\r\n", "\n").Split('\n')
				.Select(_ => FieldRE.Match(_))
				.Where(_ => _.Success && _.Groups.Count >= 3)
				.Select(_ => new { Field = _.Groups[1].Value, DataType = _.Groups[2].Value });

			return fields.ToDictionary(_ => _.Field, _ => _.DataType);
		}
	}
}
