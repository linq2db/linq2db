using LinqToDB;
using LinqToDB.Extensions;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace Tests.Linq
{
	[TestFixture]
	public class CharTypesTests : TestBase
	{
		[Table("ALLTYPES", Configuration = ProviderName.DB2)]
		[Table("alltypes", Configuration = ProviderName.PostgreSQL)]
		[Table("AllTypes")]
		public class StringTestTable
		{
			[Column("ID")]
			[Column("id", Configuration = ProviderName.PostgreSQL)]
			public int Id;

			[Column(Configuration = ProviderName.SqlCe, IsColumn = false)]
			[Column(Configuration = ProviderName.DB2, IsColumn = false)]
			[Column(Configuration = ProviderName.PostgreSQL, IsColumn = false)]
			[Column(Configuration = ProviderName.MySql, IsColumn = false)]
			[Column(Configuration = TestProvName.MySql57, IsColumn = false)]
			[Column(Configuration = TestProvName.MariaDB, IsColumn = false)]
			[Column("char20DataType")]
			public string String;

			[Column("ncharDataType")]
			[Column("nchar20DataType", Configuration = ProviderName.SapHana)]
			[Column("CHAR20DATATYPE", Configuration = ProviderName.DB2)]
			[Column("char20datatype", Configuration = ProviderName.PostgreSQL)]
			[Column("char20DataType", Configuration = ProviderName.MySql)]
			[Column("char20DataType", Configuration = TestProvName.MySql57)]
			[Column("char20DataType", Configuration = TestProvName.MariaDB)]
			[Column(Configuration = ProviderName.Firebird, IsColumn = false)]
			public string NString;

			[Column("bitDataType", Configuration = ProviderName.Sybase)]
			public bool ThisIsSYBASE;
		}

		[Table("ALLTYPES", Configuration = ProviderName.DB2)]
		[Table("alltypes", Configuration = ProviderName.PostgreSQL)]
		[Table("AllTypes")]
		public class CharTestTable
		{
			[Column("ID")]
			[Column("id", Configuration = ProviderName.PostgreSQL)]
			public int Id;

			[Column(Configuration = ProviderName.SqlCe, IsColumn = false)]
			[Column(Configuration = ProviderName.DB2, IsColumn = false)]
			[Column(Configuration = ProviderName.PostgreSQL, IsColumn = false)]
			[Column(Configuration = ProviderName.MySql, IsColumn = false)]
			[Column(Configuration = TestProvName.MySql57, IsColumn = false)]
			[Column(Configuration = TestProvName.MariaDB, IsColumn = false)]
			[Column("char20DataType")]
			public char? Char;

			[Column("ncharDataType")]
			[Column("nchar20DataType", Configuration = ProviderName.SapHana)]
			[Column("CHAR20DATATYPE", Configuration = ProviderName.DB2)]
			[Column("char20datatype", Configuration = ProviderName.PostgreSQL)]
			[Column("char20DataType", Configuration = ProviderName.MySql)]
			[Column("char20DataType", Configuration = TestProvName.MySql57)]
			[Column("char20DataType", Configuration = TestProvName.MariaDB)]
			[Column(Configuration = ProviderName.Firebird, IsColumn = false)]
			public char? NChar;

			[Column("bitDataType", Configuration = ProviderName.Sybase)]
			public bool ThisIsSYBASE;
		}

		// most of ending characters here trimmed by default by .net string TrimX methods
		// unicode test cases not used for String
		private static StringTestTable[] StringTestData = new[]
		{
			new StringTestTable() { String = "test01",      NString = "test"        },
			new StringTestTable() { String = "test02  ",    NString = "test  "      },
			new StringTestTable() { String = "test03\x09 ", NString = "test\x09 "   },
			new StringTestTable() { String = "test04\x0A ", NString = "test\x0A "   },
			new StringTestTable() { String = "test05\x0B ", NString = "test\x0B "   },
			new StringTestTable() { String = "test06\x0C ", NString = "test\x0C "   },
			new StringTestTable() { String = "test07\x0D ", NString = "test\x0D "   },
			new StringTestTable() { String = "test08\xA0 ", NString = "test\xA0 "   },
			new StringTestTable() { String = "test09     ", NString = "test\u2000 " },
			new StringTestTable() { String = "test10     ", NString = "test\u2001 " },
			new StringTestTable() { String = "test11     ", NString = "test\u2002 " },
			new StringTestTable() { String = "test12     ", NString = "test\u2003 " },
			new StringTestTable() { String = "test14     ", NString = "test\u2004 " },
			new StringTestTable() { String = "test15     ", NString = "test\u2005 " },
			new StringTestTable() { String = "test16     ", NString = "test\u2006 " },
			new StringTestTable() { String = "test17     ", NString = "test\u2007 " },
			new StringTestTable() { String = "test18     ", NString = "test\u2008 " },
			new StringTestTable() { String = "test19     ", NString = "test\u2009 " },
			new StringTestTable() { String = "test20     ", NString = "test\u200A " },
			new StringTestTable() { String = "test21     ", NString = "test\u3000 " },
			new StringTestTable() { String = "test22\0   ", NString = "test\0 "     },
			new StringTestTable()
		};

		[DataContextSource(false)]
		public void StringTrimming(string context)
		{
			using (var db = GetDataContext(context))
			{
				var lastId = db.GetTable<StringTestTable>().Select(_ => _.Id).Max();

				try
				{
					var testData = GetStringData(context);

					foreach (var record in testData)
					{
						var query = db.GetTable<StringTestTable>().Value(_ => _.NString, record.NString);

						if (!SkipChar(context))
							query = query.Value(_ => _.String, record.String);

						if (context == ProviderName.Firebird
							|| context == ProviderName.Firebird + ".LinqService")
							query = db.GetTable<StringTestTable>().Value(_ => _.String, record.String);

						if (context == ProviderName.Sybase || context == ProviderName.Sybase + ".LinqService")
							query = query.Value(_ => _.ThisIsSYBASE, true);

						query.Insert();
					}

					var records = db.GetTable<StringTestTable>().Where(_ => _.Id > lastId).OrderBy(_ => _.Id).ToArray();

					Assert.AreEqual(testData.Length, records.Length);

					for (var i = 0; i < records.Length; i++)
					{
						if (!SkipChar(context))
							Assert.AreEqual(testData[i].String?.TrimEnd(' '), records[i].String);

						if (context != ProviderName.Firebird
							&& context == ProviderName.Firebird + ".LinqService")
							Assert.AreEqual(testData[i].NString?.TrimEnd(' '), records[i].NString);
					}

				}
				finally
				{
					db.GetTable<StringTestTable>().Where(_ => _.Id > lastId).Delete();
				}
			}
		}

		private static CharTestTable[] GetCharData(string context)
		{
			// filter out null-character test cases for servers/providers without support
			if (context == ProviderName.PostgreSQL
				|| context == ProviderName.PostgreSQL + ".LinqService"
				|| context == ProviderName.DB2
				|| context == ProviderName.DB2 + ".LinqService"
				|| context == ProviderName.SqlCe
				|| context == ProviderName.SqlCe + ".LinqService"
				|| context == ProviderName.SapHana
				|| context == ProviderName.SapHana + ".LinqService")
				return CharTestData.Where(_ => _.NChar != '\0').ToArray();

			// I wonder why
			if (context == ProviderName.Firebird
				|| context == ProviderName.Firebird + ".LinqService")
				return CharTestData.Where(_ => _.NChar != '\xA0').ToArray();

			// also strange
			if (context == ProviderName.Informix
				|| context == ProviderName.Informix + ".LinqService")
				return CharTestData.Where(_ => _.NChar != '\0' && (_.NChar ?? 0) < byte.MaxValue).ToArray();

			return CharTestData;
		}

		private static StringTestTable[] GetStringData(string context)
		{
			// filter out null-character test cases for servers/providers without support
			if (context == ProviderName.PostgreSQL
				|| context == ProviderName.PostgreSQL + ".LinqService"
				|| context == ProviderName.DB2
				|| context == ProviderName.DB2 + ".LinqService"
				|| context == ProviderName.SqlCe
				|| context == ProviderName.SqlCe + ".LinqService"
				|| context == ProviderName.SapHana
				|| context == ProviderName.SapHana + ".LinqService")
				return StringTestData.Where(_ => !(_.NString ?? string.Empty).Contains("\0")).ToArray();

			// I wonder why
			if (context == ProviderName.Firebird
				|| context == ProviderName.Firebird + ".LinqService")
				return StringTestData.Where(_ => !(_.NString ?? string.Empty).Contains("\xA0")).ToArray();

			// also strange
			if (context == ProviderName.Informix
				|| context == ProviderName.Informix + ".LinqService")
				return StringTestData.Where(_ => !(_.NString ?? string.Empty).Contains("\0")
					&& !(_.NString ?? string.Empty).Any(c => (int)c > byte.MaxValue)).ToArray();

			return StringTestData;
		}

		private static CharTestTable[] CharTestData = new[]
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

		[DataContextSource(false	)]
		public void CharTrimming(string context)
		{
			using (var db = GetDataContext(context))
			{
				var lastId = db.GetTable<CharTestTable>().Select(_ => _.Id).Max();

				try
				{
					var testData = GetCharData(context);

					foreach (var record in testData)
					{
						var query = db.GetTable<CharTestTable>().Value(_ => _.NChar, record.NChar);
						if (!SkipChar(context))
							query = query.Value(_ => _.Char, record.Char);

						if (context == ProviderName.Firebird
							|| context == ProviderName.Firebird + ".LinqService")
							query = db.GetTable<CharTestTable>().Value(_ => _.Char, record.Char);

						if (context == ProviderName.Sybase || context == ProviderName.Sybase + ".LinqService")
							query = query.Value(_ => _.ThisIsSYBASE, true);

						query.Insert();
					}

					var records = db.GetTable<CharTestTable>().Where(_ => _.Id > lastId).OrderBy(_ => _.Id).ToArray();

					Assert.AreEqual(testData.Length, records.Length);

					for (var i = 0; i < records.Length; i++)
					{
						if (context == ProviderName.SapHana || context == ProviderName.SapHana + ".LinqService")
						{
							// for some reason, SAP returns \0 for space character
							// or we insert it incorrectly?
							if (testData[i].Char == ' ')
								Assert.AreEqual('\0', records[i].Char);
							else
								Assert.AreEqual(testData[i].Char, records[i].Char);

							if (testData[i].NChar == ' ')
								Assert.AreEqual('\0', records[i].NChar);
							else
								Assert.AreEqual(testData[i].NChar, records[i].NChar);

							continue;
						}

						if (!SkipChar(context))
							Assert.AreEqual(testData[i].Char, records[i].Char);

						if (context != ProviderName.Firebird
							&& context == ProviderName.Firebird + ".LinqService")
							Assert.AreEqual(testData[i].NChar, records[i].NChar);
					}

				}
				finally
				{
					db.GetTable<CharTestTable>().Where(_ => _.Id > lastId).Delete();
				}
			}
		}

		private static bool SkipChar(string context)
		{
			return context == ProviderName.SqlCe
				|| context == ProviderName.SqlCe + ".LinqService"
				|| context == ProviderName.DB2
				|| context == ProviderName.DB2 + ".LinqService"
				|| context == ProviderName.PostgreSQL
				|| context == ProviderName.PostgreSQL + ".LinqService"
				|| context == ProviderName.MySql
				|| context == ProviderName.MySql + ".LinqService"
				|| context == TestProvName.MySql57
				|| context == TestProvName.MySql57 + ".LinqService"
				|| context == TestProvName.MariaDB
				|| context == TestProvName.MariaDB + ".LinqService";
		}
	}
}
