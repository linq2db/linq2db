using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class CharTypesTests : TestBase
	{
		[Table]
		public class StringTestTable
		{
			[Column, PrimaryKey]
			public int Id;

			[Column(DataType = DataType.Char,  Length = 11)]
			public string? String;

			[Column(DataType = DataType.NChar, Length = 11)]
			[Column(Configuration = ProviderName.Firebird, IsColumn = false)]
			public string? NString;
		}

		[Table]
		public class CharTestTable
		{
			[Column, PrimaryKey]
			public int Id;

			[Column(DataType = DataType.Char,  Length = 2)]
			public char? Char;

			[Column(DataType = DataType.NChar, Length = 3)]
			[Column(Configuration = ProviderName.Firebird, IsColumn = false)]
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
		public void StringTrimming([DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse, TestProvName.AllDuckDB)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<StringTestTable>();

			var testData = GetStringData(context);

			for (var i = 0; i < testData.Length; i++)
				db.Insert(new StringTestTable { Id = i + 1, String = testData[i].String, NString = testData[i].NString });

			var records = table.OrderBy(_ => _.Id).ToArray();

			Assert.That(records, Has.Length.EqualTo(testData.Length));

			for (var i = 0; i < records.Length; i++)
			{
				if (context.IsAnyOf(TestProvName.AllSybase, TestProvName.AllOracleDevartOCI))
					Assert.That(records[i].String, Is.EqualTo(testData[i].String?.TrimEnd(' ')?.TrimEnd('\0')));
				else if (context.IsAnyOf(TestProvName.AllClickHouse))
					Assert.That(records[i].String, Is.EqualTo(testData[i].String?.TrimEnd('\0')));
				else if (context.IsAnyOf(TestProvName.AllYdb))
					// YDB has no fixed-length CHAR: the value round-trips verbatim (no padding, no trailing trim).
					Assert.That(records[i].String, Is.EqualTo(testData[i].String));
				else
					Assert.That(records[i].String, Is.EqualTo(testData[i].String?.TrimEnd(' ')));

				if (!context.IsAnyOf(TestProvName.AllFirebird))
				{
					if (context.IsAnyOf(TestProvName.AllSybase, TestProvName.AllOracleDevartOCI))
						Assert.That(records[i].NString, Is.EqualTo(testData[i].NString?.TrimEnd(' ')?.TrimEnd('\0')));
					else if (context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(records[i].NString, Is.EqualTo(testData[i].NString?.TrimEnd('\0')));
					else if (context.IsAnyOf(TestProvName.AllYdb))
						Assert.That(records[i].NString, Is.EqualTo(testData[i].NString));
					else
						Assert.That(records[i].NString, Is.EqualTo(testData[i].NString?.TrimEnd(' ')));
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
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<CharTestTable>();

			var testData = GetCharData(context);

			for (var i = 0; i < testData.Length; i++)
				db.Insert(new CharTestTable { Id = i + 1, Char = testData[i].Char, NChar = testData[i].NChar });

			var records = table.OrderBy(_ => _.Id).ToArray();

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

				if (context.IsAnyOf(TestProvName.AllMySql))
					// MySQL trims trailing spaces from CHAR on retrieval
					Assert.That(records[i].Char, Is.EqualTo(testData[i].Char == ' ' ? '\0' : testData[i].Char));
				else if (context.IsAnyOf(TestProvName.AllSybase))
					Assert.That(records[i].Char, Is.EqualTo(testData[i].Char == '\0' ? ' ' : testData[i].Char));
				else
					Assert.That(records[i].Char, Is.EqualTo(testData[i].Char));

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

		sealed class CharFieldRecord
		{
			[PrimaryKey(1)]
			[Identity] public int Id { get; set; }

			public char CharFld { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1279")]
		public void CharPropertyRoundTrip([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<CharFieldRecord>();
			var val = 'P';

			db.Insert(new CharFieldRecord { CharFld = val });

			var result = db.GetTable<CharFieldRecord>().First().CharFld;

			Assert.That(result, Is.EqualTo(val));
		}

		[Table("AllTypes")]
		[Table("ALLTYPES", Configuration = ProviderName.DB2)]
		private sealed class AllTypesCharRow
		{
			[Column]
			[Column("CHARDATATYPE", Configuration = ProviderName.DB2)]
			public char charDataType { get; set; }
		}

		[Table("AllTypes")]
		[Table("ALLTYPES", Configuration = ProviderName.DB2)]
		private sealed class AllTypesNullableCharRow
		{
			[Column]
			[Column("CHARDATATYPE", Configuration = ProviderName.DB2)]
			public char? charDataType { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1287")]
		public void NullableCharComparedToLiteral([DataSources(ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			var list = db.GetTable<AllTypesNullableCharRow>().Where(_ => _.charDataType == '1').ToList();

			Assert.That(list, Has.Count.EqualTo(1));
			Assert.That(list[0].charDataType, Is.EqualTo('1'));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1287")]
		public void CharComparedToLiteral([DataSources(ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			var list = db.GetTable<AllTypesCharRow>().Where(_ => _.charDataType == '1').ToList();

			Assert.That(list, Has.Count.EqualTo(1));
			Assert.That(list[0].charDataType, Is.EqualTo('1'));
		}

		public class CharKindsEntity
		{
			[Column(DataType = DataType.Char)]     public string? CharValue;
			[Column(DataType = DataType.VarChar)]  public string? VarCharValue;
			[Column(DataType = DataType.NChar)]    public string? NCharValue;
			[Column(DataType = DataType.NVarChar)] public string? NVarCharValue;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/271")]
		public void NPrefixOnlyForUnicodeColumnLiterals([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CharKindsEntity>();

			var q =
					from e in db.GetTable<CharKindsEntity>()
					where
						e.CharValue     == "CharValue"     &&
						e.VarCharValue  == "VarCharValue"  &&
						e.NCharValue    == "NCharValue"    &&
						e.NVarCharValue == "NVarCharValue"
					select e;

			var sql = q.ToSqlQuery().Sql;

			Assert.That(sql, Does.Not.Contain("N'CharValue'"));
			Assert.That(sql, Does.Not.Contain("N'VarCharValue'"));

			Assert.That(sql, Does.Contain("N'NCharValue'"));
			Assert.That(sql, Does.Contain("N'NVarCharValue'"));

			q.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/271")]
		public void NPrefixOnlyForUnicodeColumnArrayContains([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CharKindsEntity>();

			var @char     = new[] { "CharValue"     };
			var @varChar  = new[] { "VarCharValue"  };
			var @nChar    = new[] { "NCharValue"    };
			var @nVarChar = new[] { "NVarCharValue" };

			var q =
					from e in db.GetTable<CharKindsEntity>()
					where
						@char    .Contains(e.CharValue    ) &&
						@varChar .Contains(e.VarCharValue ) &&
						@nChar   .Contains(e.NCharValue   ) &&
						@nVarChar.Contains(e.NVarCharValue)
					select e;

			var sql = q.ToSqlQuery().Sql;

			Assert.That(sql, Does.Not.Contain("N'CharValue'"));
			Assert.That(sql, Does.Not.Contain("N'VarCharValue'"));

			Assert.That(sql, Does.Contain("N'NCharValue'"));
			Assert.That(sql, Does.Contain("N'NVarCharValue'"));

			q.ToArray();
		}

		[Table("Person", IsColumnAttributeRequired = false)]
		public class PersonWithGender
		{
			[SequenceName(ProviderName.Firebird, "PersonID")]
			[Column("PersonID"), Identity, PrimaryKey] public int     ID;
			[NotNull]                                  public string  FirstName { get; set; } = null!;
			[NotNull]                                  public string  LastName = null!;
			[Nullable]                                 public string? MiddleName;
			                                           public char    Gender;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/88")]
		public void CharColumnComparedWithLiteralAndVariable([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var llc = db
					.GetTable<PersonWithGender>()
					.Where(_ => _.ID == 1 && _.Gender == 'M');

			var lrc = db
					.GetTable<PersonWithGender>()
					.Where(_ => _.ID == 1 && 'M' == _.Gender);

			var gender = 'M';
			var llp = db
					.GetTable<PersonWithGender>()
					.Where(_ => _.ID == 1 && _.Gender == gender);

			var lrp = db
					.GetTable<PersonWithGender>()
					.Where(_ => _.ID == 1 && gender == _.Gender);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(llc, Is.Not.Empty);
				Assert.That(lrc, Is.Not.Empty);
				Assert.That(llp, Is.Not.Empty);
				Assert.That(lrp, Is.Not.Empty);
			}

		}
	}
}
