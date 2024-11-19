using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class CharTypesTests : TestBase
	{
		[Table("ALLTYPES", Configuration = ProviderName.DB2)]
		[Table("AllTypes")]
		public class StringTestTable
		{
			[Column("ID")]
			public int Id;

			[Column("char20DataType")]
			[Column(Configuration = ProviderName.SqlCe,			 IsColumn = false)]
			[Column(Configuration = ProviderName.DB2,			 IsColumn = false)]
			[Column(Configuration = ProviderName.PostgreSQL,	 IsColumn = false)]
			[Column(Configuration = ProviderName.MySql,			 IsColumn = false)]
			public string? String;

			[Column("ncharDataType")]
			[Column("nchar20DataType", Configuration = ProviderName.SapHana)]
			[Column("CHAR20DATATYPE" , Configuration = ProviderName.DB2)]
			[Column("char20DataType" , Configuration = ProviderName.PostgreSQL)]
			[Column("char20DataType" , Configuration = ProviderName.MySql)]
			[Column(                   Configuration = ProviderName.Firebird, IsColumn = false)]
			public string? NString;
		}

		[Table("ALLTYPES", Configuration = ProviderName.DB2)]
		[Table("AllTypes")]
		public class CharTestTable
		{
			[Column("ID")]
			public int Id;

			[Column("char20DataType")]
			[Column(Configuration = ProviderName.SqlCe,			 IsColumn = false)]
			[Column(Configuration = ProviderName.DB2,			 IsColumn = false)]
			[Column(Configuration = ProviderName.PostgreSQL,	 IsColumn = false)]
			[Column(Configuration = ProviderName.MySql,			 IsColumn = false)]
			public char? Char;

			[Column("ncharDataType"  , DataType = DataType.NChar)]
			[Column("nchar20DataType", DataType = DataType.NChar, Configuration = ProviderName.SapHana)]
			[Column("CHAR20DATATYPE" , DataType = DataType.NChar, Configuration = ProviderName.DB2)]
			[Column("char20DataType" , DataType = DataType.NChar, Configuration = ProviderName.PostgreSQL)]
			[Column("char20DataType" , DataType = DataType.NChar, Configuration = ProviderName.MySql)]
			[Column(                   Configuration = ProviderName.Firebird, IsColumn = false)]
			public char? NChar;
		}

		// most of ending characters here trimmed by default by .net string TrimX methods
		// unicode test cases not used for String
		static readonly StringTestTable[] StringTestData =
		{
			new StringTestTable() { String = "test01",      NString = "test01"        },
			new StringTestTable() { String = "test02  ",    NString = "test02  "      },
			new StringTestTable() { String = "test03\x09 ", NString = "test03\x09 "   },
			new StringTestTable() { String = "test04\x0A ", NString = "test04\x0A "   },
			new StringTestTable() { String = "test05\x0B ", NString = "test05\x0B "   },
			new StringTestTable() { String = "test06\x0C ", NString = "test06\x0C "   },
			new StringTestTable() { String = "test07\x0D ", NString = "test07\x0D "   },
			new StringTestTable() { String = "test08\xA0 ", NString = "test08\xA0 "   },
			new StringTestTable() { String = "test09     ", NString = "test09\u2000 " },
			new StringTestTable() { String = "test10     ", NString = "test10\u2001 " },
			new StringTestTable() { String = "test11     ", NString = "test11\u2002 " },
			new StringTestTable() { String = "test12     ", NString = "test12\u2003 " },
			new StringTestTable() { String = "test13     ", NString = "test13\u2004 " },
			new StringTestTable() { String = "test14     ", NString = "test14\u2005 " },
			new StringTestTable() { String = "test15     ", NString = "test15\u2006 " },
			new StringTestTable() { String = "test16     ", NString = "test16\u2007 " },
			new StringTestTable() { String = "test17     ", NString = "test17\u2008 " },
			new StringTestTable() { String = "test18     ", NString = "test18\u2009 " },
			new StringTestTable() { String = "test19     ", NString = "test19\u200A " },
			new StringTestTable() { String = "test20     ", NString = "test20\u3000 " },
			new StringTestTable() { String = "test21\0   ", NString = "test21\0 "     },
			new StringTestTable()
		};

		// CH: We don't perform trimming of FixedString type
		[Test]
		public void StringTrimming([DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var lastId = db.GetTable<StringTestTable>().Select(_ => _.Id).Max();
				var nextId = lastId + 1;

				try
				{
					var testData = GetStringData(context);

					foreach (var record in testData)
					{
						var query = db.GetTable<StringTestTable>().Value(_ => _.NString, record.NString);

						if (!SkipChar(context))
							query = query.Value(_ => _.String, record.String);

						if (context.IsAnyOf(TestProvName.AllFirebird))
							query = db.GetTable<StringTestTable>().Value(_ => _.String, record.String);

						if (context.IsAnyOf(TestProvName.AllClickHouse))
							query = query.Value(_ => _.Id, nextId++);

						query.Insert();
					}

					var records = db.GetTable<StringTestTable>().Where(_ => _.Id > lastId).OrderBy(_ => _.Id).ToArray();

					Assert.That(records, Has.Length.EqualTo(testData.Length));

					for (var i = 0; i < records.Length; i++)
					{
						if (!SkipChar(context))
						{
							if (context.IsAnyOf(TestProvName.AllSybase, TestProvName.AllOracleDevartOCI))
								Assert.That(records[i].String, Is.EqualTo(testData[i].String?.TrimEnd(' ')?.TrimEnd('\0')));
							else if (context.IsAnyOf(TestProvName.AllClickHouse))
								Assert.That(records[i].String, Is.EqualTo(testData[i].String?.TrimEnd('\0')));
							else
								Assert.That(records[i].String, Is.EqualTo(testData[i].String?.TrimEnd(' ')));
						}

						if (!context.IsAnyOf(TestProvName.AllFirebird))
						{
							if (context.IsAnyOf(TestProvName.AllSybase, TestProvName.AllOracleDevartOCI))
								Assert.That(records[i].NString, Is.EqualTo(testData[i].NString?.TrimEnd(' ')?.TrimEnd('\0')));
							else if (context.IsAnyOf(TestProvName.AllClickHouse))
								Assert.That(records[i].NString, Is.EqualTo(testData[i].NString?.TrimEnd('\0')));
							else
								Assert.That(records[i].NString, Is.EqualTo(testData[i].NString?.TrimEnd(' ')));
						}
					}

				}
				finally
				{
					db.GetTable<StringTestTable>().Where(_ => _.Id > lastId).Delete();
				}
			}
		}

		private CharTestTable[] GetCharData([DataSources] string context)
		{
			// filter out null-character test cases for servers/providers without support
			if (   context.IsAnyOf(TestProvName.AllPostgreSQL)
				|| context.IsAnyOf(ProviderName.DB2, ProviderName.SqlCe)
				|| context.IsAnyOf(TestProvName.AllSapHana))
				return CharTestData.Where(_ => _.NChar != '\0').ToArray();

			// I wonder why
			if (context.IsAnyOf(TestProvName.AllFirebird))
				return CharTestData.Where(_ => _.NChar != '\xA0').ToArray();

			// also strange
			if (context.IsAnyOf(TestProvName.AllInformix))
				return CharTestData.Where(_ => _.NChar != '\0' && (_.NChar ?? 0) < byte.MaxValue).ToArray();

			return CharTestData;
		}

		private StringTestTable[] GetStringData([DataSources] string context)
		{
			var provider = GetProviderName(context, out var _);

			// filter out null-character test cases for servers/providers without support
			if (context.IsAnyOf(TestProvName.AllPostgreSQL)
				|| context.IsAnyOf(ProviderName.DB2)
				|| context.IsAnyOf(TestProvName.AllSQLite)
				|| context.IsAnyOf(ProviderName.SqlCe)
				|| context.IsAnyOf(TestProvName.AllSapHana))
				return StringTestData.Where(_ => !(_.NString ?? string.Empty).Contains("\0")).ToArray();

			// I wonder why
			if (context.IsAnyOf(TestProvName.AllFirebird))
				return StringTestData.Where(_ => !(_.NString ?? string.Empty).Contains("\xA0")).ToArray();

			// also strange
			if (context.IsAnyOf(TestProvName.AllInformix))
				return StringTestData.Where(_ => !(_.NString ?? string.Empty).Contains("\0")
					&& !(_.NString ?? string.Empty).Any(c => (int)c > byte.MaxValue)).ToArray();

			return StringTestData;
		}

		static readonly CharTestTable[] CharTestData =
		{
			new CharTestTable() { Char = ' ',    NChar = ' '      },
			new CharTestTable() { Char = '\x09', NChar = '\x09'   },
			new CharTestTable() { Char = '\x0A', NChar = '\x0A'   },
			new CharTestTable() { Char = '\x0B', NChar = '\x0B'   },
			new CharTestTable() { Char = '\x0C', NChar = '\x0C'   },
			new CharTestTable() { Char = '\x0D', NChar = '\x0D'   },
			new CharTestTable() { Char = '\xA0', NChar = '\xA0'   },
			new CharTestTable() { Char = ' ',    NChar = '\u2000' },
			new CharTestTable() { Char = ' ',    NChar = '\u2001' },
			new CharTestTable() { Char = ' ',    NChar = '\u2002' },
			new CharTestTable() { Char = ' ',    NChar = '\u2003' },
			new CharTestTable() { Char = ' ',    NChar = '\u2004' },
			new CharTestTable() { Char = ' ',    NChar = '\u2005' },
			new CharTestTable() { Char = ' ',    NChar = '\u2006' },
			new CharTestTable() { Char = ' ',    NChar = '\u2007' },
			new CharTestTable() { Char = ' ',    NChar = '\u2008' },
			new CharTestTable() { Char = ' ',    NChar = '\u2009' },
			new CharTestTable() { Char = ' ',    NChar = '\u200A' },
			new CharTestTable() { Char = ' ',    NChar = '\u3000' },
			new CharTestTable() { Char = '\0',   NChar = '\0'     },
			new CharTestTable()
		};

		[Test]
		public void CharTrimming([DataSources(TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var lastId = db.GetTable<CharTestTable>().Select(_ => _.Id).Max();
				var nextId = lastId + 1;
				try
				{
					var testData = GetCharData(context);

					foreach (var record in testData)
					{
						var query = db.GetTable<CharTestTable>().Value(_ => _.NChar, record.NChar);
						if (!SkipChar(context))
							query = query.Value(_ => _.Char, record.Char);

						if (context.IsAnyOf(TestProvName.AllFirebird))
							query = db.GetTable<CharTestTable>().Value(_ => _.Char, record.Char);

						if (context.IsAnyOf(TestProvName.AllClickHouse))
							query = query.Value(_ => _.Id, nextId++);

						query.Insert();
					}

					var records = db.GetTable<CharTestTable>().Where(_ => _.Id > lastId).OrderBy(_ => _.Id).ToArray();

					Assert.That(records, Has.Length.EqualTo(testData.Length));

					for (var i = 0; i < records.Length; i++)
					{
						if (context.IsAnyOf(TestProvName.AllSapHana))
						{
							// SAP or provider trims space and we return default value, which is \0 for char
							// or we insert it incorrectly?
							if (testData[i].Char == ' ')
								Assert.That(records[i].Char, Is.EqualTo('\0'));
							else
								Assert.That(records[i].Char, Is.EqualTo(testData[i].Char));

							if (testData[i].NChar == ' ')
								Assert.That(records[i].NChar, Is.EqualTo('\0'));
							else
								Assert.That(records[i].NChar, Is.EqualTo(testData[i].NChar));

							continue;
						}

						if (!SkipChar(context))
						{
							if (context.IsAnyOf(TestProvName.AllSybase))
								Assert.That(records[i].Char, Is.EqualTo(testData[i].Char == '\0' ? ' ' : testData[i].Char));
							else
								Assert.That(records[i].Char, Is.EqualTo(testData[i].Char));
						}

						if (context.IsAnyOf(TestProvName.AllMySql))
							// for some reason mysql doesn't insert space
							Assert.That(records[i].NChar, Is.EqualTo(testData[i].NChar == ' ' ? '\0' : testData[i].NChar));
						else if (!context.IsAnyOf(TestProvName.AllFirebird))
						{
							if (context.IsAnyOf(TestProvName.AllSybase))
								Assert.That(records[i].NChar, Is.EqualTo(testData[i].NChar == '\0' ? ' ' : testData[i].NChar));
							else
								Assert.That(records[i].NChar, Is.EqualTo(testData[i].NChar));
						}
					}
				}
				finally
				{
					db.GetTable<CharTestTable>().Where(_ => _.Id > lastId).Delete();
				}
			}
		}

		private static bool SkipChar([DataSources] string context)
		{
			return context.IsAnyOf(ProviderName.SqlCe, ProviderName.DB2, TestProvName.AllPostgreSQL, TestProvName.AllMySql);
		}
	}
}
