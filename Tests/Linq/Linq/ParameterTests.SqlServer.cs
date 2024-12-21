using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	public partial class ParameterTests
	{
		[Table("AllTypes")]
		sealed class AllTypesWithLength
		{
			[Column(                             Length = 1)]  public byte[]? VarBinaryDataType;
			[Column(DataType = DataType.VarChar, Length = 20)] public string? VarcharDataType;
			[Column(                             Length = 20)] public string? NVarcharDataType;
		}

		[Table("AllTypes")]
		sealed class AllTypesCustom
		{
			[Column] public VarBinary? VarBinaryDataType;
			[Column] public VarChar?   VarcharDataType;
			[Column] public NVarChar?  NVarcharDataType;
		}

		[Table("AllTypes")]
		sealed class AllTypesCustomWithLength
		{
			[Column(Length = 1)]  public VarBinary? VarBinaryDataType;
			[Column(Length = 20)] public VarChar?   VarcharDataType;
			[Column(Length = 20)] public NVarChar?  NVarcharDataType;
		}

		sealed class AllTypesCustomMaxLength
		{
			public VarBinary? VarBinary;
			public VarChar?   VarChar;
			public NVarChar?  NVarChar;
		}

		sealed class VarChar : CustomBase<string>
		{
			public override string ToString(IFormatProvider? provider)
			{
				return Value;
			}
		}

		sealed class NVarChar : CustomBase<string>
		{
			public override string ToString(IFormatProvider? provider)
			{
				return Value;
			}
		}

		sealed class VarBinary : CustomBase<byte[]>
		{
			public override object ToType(Type conversionType, IFormatProvider? provider)
			{
				if (conversionType == typeof(byte[]))
					return Value;

				throw new NotImplementedException();
			}
		}

		abstract class CustomBase<TValue> : IConvertible
		{
			public TValue Value { get; set; } = default!;

			TypeCode IConvertible.GetTypeCode()
			{
				throw new NotImplementedException();
			}

			bool IConvertible.ToBoolean(IFormatProvider? provider)
			{
				throw new NotImplementedException();
			}

			byte IConvertible.ToByte(IFormatProvider? provider)
			{
				throw new NotImplementedException();
			}

			char IConvertible.ToChar(IFormatProvider? provider)
			{
				throw new NotImplementedException();
			}

			DateTime IConvertible.ToDateTime(IFormatProvider? provider)
			{
				throw new NotImplementedException();
			}

			decimal IConvertible.ToDecimal(IFormatProvider? provider)
			{
				throw new NotImplementedException();
			}

			double IConvertible.ToDouble(IFormatProvider? provider)
			{
				throw new NotImplementedException();
			}

			short IConvertible.ToInt16(IFormatProvider? provider)
			{
				throw new NotImplementedException();
			}

			int IConvertible.ToInt32(IFormatProvider? provider)
			{
				throw new NotImplementedException();
			}

			long IConvertible.ToInt64(IFormatProvider? provider)
			{
				throw new NotImplementedException();
			}

			sbyte IConvertible.ToSByte(IFormatProvider? provider)
			{
				throw new NotImplementedException();
			}

			float IConvertible.ToSingle(IFormatProvider? provider)
			{
				throw new NotImplementedException();
			}

			public virtual string ToString(IFormatProvider? provider)
			{
				throw new NotImplementedException();
			}

			public virtual object ToType(Type conversionType, IFormatProvider? provider)
			{
				throw new NotImplementedException();
			}

			ushort IConvertible.ToUInt16(IFormatProvider? provider)
			{
				throw new NotImplementedException();
			}

			uint IConvertible.ToUInt32(IFormatProvider? provider)
			{
				throw new NotImplementedException();
			}

			ulong IConvertible.ToUInt64(IFormatProvider? provider)
			{
				throw new NotImplementedException();
			}
		}

		[Test]
		public void SqlServerNVarChar4000ParameterSize([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var p = "abc";

				var query =  db.GetTable<Person>().Where(t => t.FirstName == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("(4000)"));
			}
		}

		[Test]
		public void SqlServerVarChar8000ParameterSize([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var p = "abc";

				var query =  db.GetTable<AllTypes>().Where(t => t.VarcharDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("(8000)"));
			}
		}

		[Test]
		public void SqlServerVarBinary8000ParameterSize([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var p = new byte[] { 1 };

				var query =  db.GetTable<AllTypes>().Where(t => t.VarBinaryDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("(8000)"));
			}
		}

		[Test]
		public void SqlServerNVarCharKnownParameterSize([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var p = "abc";

				var query =  db.GetTable<AllTypesWithLength>().Where(t => t.NVarcharDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("(20)"));
			}
		}

		[Test]
		public void SqlServerVarCharKnownParameterSize([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var p = "abc";

				var query =  db.GetTable<AllTypesWithLength>().Where(t => t.VarcharDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("(20)"));
			}
		}

		[Test]
		public void SqlServerVarBinaryKnownParameterSize([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var p = new byte[] { 1 };

				var query =  db.GetTable<AllTypesWithLength>().Where(t => t.VarBinaryDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("(1)"));
			}
		}

		[Test]
		public void SqlServerNVarCharKnownOverflowParameterSize([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var p = "abcdeabcdeabcdeabcde1";

				var query =  db.GetTable<AllTypesWithLength>().Where(t => t.NVarcharDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("(4000)"));
			}
		}

		[Test]
		public void SqlServerVarCharKnownOverflowParameterSize([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var p = "abcdeabcdeabcdeabcde1";

				var query =  db.GetTable<AllTypesWithLength>().Where(t => t.VarcharDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("(8000)"));
			}
		}

		[Test]
		public void SqlServerVarBinaryKnownOverflowParameterSize([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var p = new byte[] { 1, 2 };

				var query =  db.GetTable<AllTypesWithLength>().Where(t => t.VarBinaryDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("(8000)"));
			}
		}

		[Test]
		public void SqlServerCustomNVarChar4000ParameterSize([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context, new MappingSchema()))
			{
				SetupCustomTypes(db.MappingSchema);
				var p = new NVarChar() { Value = "abc" };

				var query =  db.GetTable<AllTypesCustom>().Where(t => t.NVarcharDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("NVarChar -- String"));
			}
		}

		[Test]
		public void SqlServerCustomVarChar8000ParameterSize([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context, new MappingSchema()))
			{
				SetupCustomTypes(db.MappingSchema);
				var p = new VarChar() { Value = "abc" };

				var query =  db.GetTable<AllTypesCustom>().Where(t => t.VarcharDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring(" VarChar -- AnsiString"));
			}
		}

		[Test]
		public void SqlServerCustomVarBinary8000ParameterSize([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context, new MappingSchema()))
			{
				SetupCustomTypes(db.MappingSchema);
				var p = new VarBinary() { Value = new byte[] { 1 } };

				var query =  db.GetTable<AllTypesCustom>().Where(t => t.VarBinaryDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("VarBinary -- Binary"));
			}
		}

		[Test]
		public void SqlServerCustomNVarCharKnownParameterSize([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context, new MappingSchema()))
			{
				SetupCustomTypes(db.MappingSchema);
				var p = new NVarChar() { Value = "abc" };

				var query =  db.GetTable<AllTypesCustomWithLength>().Where(t => t.NVarcharDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("NVarChar -- String"));
			}
		}

		[Test]
		public void SqlServerCustomVarCharKnownParameterSize([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context, new MappingSchema()))
			{
				SetupCustomTypes(db.MappingSchema);
				var p = new VarChar() { Value = "abc" };

				var query =  db.GetTable<AllTypesCustomWithLength>().Where(t => t.VarcharDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring(" VarChar -- AnsiString"));
			}
		}

		[Test]
		public void SqlServerCustomVarBinaryKnownParameterSize([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context, new MappingSchema()))
			{
				SetupCustomTypes(db.MappingSchema);
				var p = new VarBinary() { Value = new byte[] { 1 } };

				var query =  db.GetTable<AllTypesCustomWithLength>().Where(t => t.VarBinaryDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("VarBinary -- Binary"));
			}
		}

		[Test]
		public void SqlServerCustomNVarCharKnownOverflowParameterSize([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context, new MappingSchema()))
			{
				SetupCustomTypes(db.MappingSchema);
				var p = new NVarChar() { Value = "abcdeabcdeabcdeabcde1" };

				var query =  db.GetTable<AllTypesCustomWithLength>().Where(t => t.NVarcharDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("NVarChar -- String"));
			}
		}

		[Test]
		public void SqlServerCustomVarCharKnownOverflowParameterSize([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context, new MappingSchema()))
			{
				SetupCustomTypes(db.MappingSchema);
				var p = new VarChar() { Value = "abcdeabcdeabcdeabcde1" };

				var query =  db.GetTable<AllTypesCustomWithLength>().Where(t => t.VarcharDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring(" VarChar -- AnsiString"));
			}
		}

		[Test]
		public void SqlServerCustomVarBinaryKnownOverflowParameterSize([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context, new MappingSchema()))
			{
				SetupCustomTypes(db.MappingSchema);
				var p = new VarBinary() { Value = new byte[] { 1, 2 } };

				var query =  db.GetTable<AllTypesCustomWithLength>().Where(t => t.VarBinaryDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("VarBinary -- Binary"));
			}
		}

		[Test]
		public void SqlServerCustomNVarCharMaxOverflowParameterSize([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context, new MappingSchema()))
			{
				SetupCustomTypes(db.MappingSchema);
				using (var table = db.CreateLocalTable<AllTypesCustomMaxLength>())
				{
					var value = new string('я', 5000);
					table.Insert(() => new AllTypesCustomMaxLength()
					{
						NVarChar = new NVarChar() { Value = value }
					});

					var records = table.ToList();
					var p = new NVarChar() { Value = value };

					var query =  table.Where(t => t.NVarChar == p);
					query.ToArray();
					var sql = GetCurrentBaselines();

					Assert.That(records, Has.Count.EqualTo(1));
					Assert.That(records[0].NVarChar, Is.Not.Null);
					Assert.Multiple(() =>
					{
						Assert.That(records[0].NVarChar!.Value, Is.EqualTo(value));
						Assert.That(sql, Does.Contain("NVarChar -- String"));
					});
				}
			}
		}

		[Test]
		public void SqlServerCustomVarCharMaxOverflowParameterSize([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context, new MappingSchema()))
			{
				SetupCustomTypes(db.MappingSchema);
				using (var table = db.CreateLocalTable<AllTypesCustomMaxLength>())
				{
					var value = new string('z', 10000);
					table.Insert(() => new AllTypesCustomMaxLength()
					{
						VarChar = new VarChar() { Value = value }
					});

					var records = table.ToList();
					var p = new VarChar() { Value = value };

					var query =  table.Where(t => t.VarChar == p);
					query.ToArray();
					var sql = GetCurrentBaselines();

					Assert.That(records, Has.Count.EqualTo(1));
					Assert.That(records[0].VarChar, Is.Not.Null);
					Assert.Multiple(() =>
					{
						Assert.That(records[0].VarChar!.Value, Is.EqualTo(value));
						Assert.That(sql, Contains.Substring(" VarChar -- AnsiString"));
					});
				}
			}
		}

		[Test]
		public void SqlServerCustomVarBinaryMaxOverflowParameterSize([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context, new MappingSchema()))
			{
				SetupCustomTypes(db.MappingSchema);
				using (var table = db.CreateLocalTable<AllTypesCustomMaxLength>())
				{
					var value = new byte[10000];
					for (var i = 0; i < value.Length; i++)
						value[i] = (byte)(i % 256);

					table.Insert(() => new AllTypesCustomMaxLength()
					{
						VarBinary = new VarBinary() { Value = value }
					});

					var records = table.ToList();
					var p = new VarBinary() { Value = value };

					var query =  table.Where(t => t.VarBinary == p);
					query.ToArray();
					var sql = GetCurrentBaselines();

					Assert.That(records, Has.Count.EqualTo(1));
					Assert.That(records[0].VarBinary, Is.Not.Null);
					Assert.Multiple(() =>
					{
						Assert.That(records[0].VarBinary!.Value, Is.EqualTo(value));
						Assert.That(sql, Does.Contain("VarBinary -- Binary"));
					});
				}
			}
		}

		[Test]
		public void SqlServerCustomNVarChar4000ParameterSizeAsDataParameter([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context, new MappingSchema()))
			{
				SetupCustomTypes(db.MappingSchema, true);
				var p = new NVarChar() { Value = "abc" };

				var query = db.GetTable<AllTypesCustom>().Where(t => t.NVarcharDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("(4000)"));
			}
		}

		[Test]
		public void SqlServerCustomVarChar8000ParameterSizeAsDataParameter([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context, new MappingSchema()))
			{
				SetupCustomTypes(db.MappingSchema, true);
				var p = new VarChar() { Value = "abc" };

				var query = db.GetTable<AllTypesCustom>().Where(t => t.VarcharDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("(8000)"));
			}
		}

		[Test]
		public void SqlServerCustomVarBinary8000ParameterSizeAsDataParameter([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context, new MappingSchema()))
			{
				SetupCustomTypes(db.MappingSchema, true);
				var p = new VarBinary() { Value = new byte[] { 1 } };

				var query = db.GetTable<AllTypesCustom>().Where(t => t.VarBinaryDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("(8000)"));
			}
		}

		[Test]
		public void SqlServerCustomNVarCharKnownParameterSizeAsDataParameter([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context, new MappingSchema()))
			{
				SetupCustomTypes(db.MappingSchema, true);
				var p = new NVarChar() { Value = "abc" };

				var query = db.GetTable<AllTypesCustomWithLength>().Where(t => t.NVarcharDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("(20)"));
			}
		}

		[Test]
		public void SqlServerCustomVarCharKnownParameterSizeAsDataParameter([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context, new MappingSchema()))
			{
				SetupCustomTypes(db.MappingSchema, true);
				var p = new VarChar() { Value = "abc" };

				var query = db.GetTable<AllTypesCustomWithLength>().Where(t => t.VarcharDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("(20)"));
			}
		}

		[Test]
		public void SqlServerCustomVarBinaryKnownParameterSizeAsDataParameter([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context, new MappingSchema()))
			{
				SetupCustomTypes(db.MappingSchema, true);
				var p = new VarBinary() { Value = new byte[] { 1 } };

				var query = db.GetTable<AllTypesCustomWithLength>().Where(t => t.VarBinaryDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("(1)"));
			}
		}

		[Test]
		public void SqlServerCustomNVarCharKnownOverflowParameterSizeAsDataParameter([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context, new MappingSchema()))
			{
				SetupCustomTypes(db.MappingSchema, true);
				var p = new NVarChar() { Value = "abcdeabcdeabcdeabcde1" };

				var query = db.GetTable<AllTypesCustomWithLength>().Where(t => t.NVarcharDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("(4000)"));
			}
		}

		[Test]
		public void SqlServerCustomVarCharKnownOverflowParameterSizeAsDataParameter([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context, new MappingSchema()))
			{
				SetupCustomTypes(db.MappingSchema, true);
				var p = new VarChar() { Value = "abcdeabcdeabcdeabcde1" };

				var query = db.GetTable<AllTypesCustomWithLength>().Where(t => t.VarcharDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("(8000)"));
			}
		}

		[Test]
		public void SqlServerCustomVarBinaryKnownOverflowParameterSizeAsDataParameter([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context, new MappingSchema()))
			{
				SetupCustomTypes(db.MappingSchema, true);
				var p = new VarBinary() { Value = new byte[] { 1, 2 } };

				var query = db.GetTable<AllTypesCustomWithLength>().Where(t => t.VarBinaryDataType == p);
				query.ToArray();
				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("(8000)"));
			}
		}

		[Test]
		public void SqlServerCustomNVarCharMaxOverflowParameterSizeAsDataParameter([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context, new MappingSchema()))
			{
				SetupCustomTypes(db.MappingSchema, true);

				using (var table = db.CreateLocalTable<AllTypesCustomMaxLength>())
				{
					var value = new string('я', 5000);
					table.Insert(() => new AllTypesCustomMaxLength()
					{
						NVarChar = new NVarChar() { Value = value }
					});

					var records = table.ToList();
					var p = new NVarChar() { Value = value };

					var query = table.Where(t => t.NVarChar == p);
					query.ToArray();
					var sql = GetCurrentBaselines();

					Assert.That(records, Has.Count.EqualTo(1));
					Assert.That(records[0].NVarChar, Is.Not.Null);
					Assert.Multiple(() =>
					{
						Assert.That(records[0].NVarChar!.Value, Is.EqualTo(value));
						Assert.That(sql, Does.Contain("NVarChar(5000) -- String"));
					});
				}
			}
		}

		[Test]
		public void SqlServerCustomVarCharMaxOverflowParameterSizeAsDataParameter([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context, new MappingSchema()))
			{
				SetupCustomTypes(db.MappingSchema, true);
				using (var table = db.CreateLocalTable<AllTypesCustomMaxLength>())
				{
					var value = new string('z', 10000);
					table.Insert(() => new AllTypesCustomMaxLength()
					{
						VarChar = new VarChar() { Value = value }
					});

					var records = table.ToList();
					var p = new VarChar() { Value = value };

					var query = table.Where(t => t.VarChar == p);
					query.ToArray();
					var sql = GetCurrentBaselines();

					Assert.That(records, Has.Count.EqualTo(1));
					Assert.That(records[0].VarChar, Is.Not.Null);
					Assert.Multiple(() =>
					{
						Assert.That(records[0].VarChar!.Value, Is.EqualTo(value));
						Assert.That(sql, Does.Contain(" VarChar(10000) -- AnsiString"));
					});
				}
			}
		}

		[Test]
		public void SqlServerCustomVarBinaryMaxOverflowParameterSizeAsDataParameter([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context, new MappingSchema()))
			{
				SetupCustomTypes(db.MappingSchema, true);
				using (var table = db.CreateLocalTable<AllTypesCustomMaxLength>())
				{
					var value = new byte[10000];
					for (var i = 0; i < value.Length; i++)
						value[i] = (byte)(i % 256);

					table.Insert(() => new AllTypesCustomMaxLength()
					{
						VarBinary = new VarBinary() { Value = value }
					});

					var records = table.ToList();
					var p = new VarBinary() { Value = value };

					var query = table.Where(t => t.VarBinary == p);
					query.ToArray();
					var sql = GetCurrentBaselines();

					Assert.That(records, Has.Count.EqualTo(1));
					Assert.That(records[0].VarBinary, Is.Not.Null);
					Assert.Multiple(() =>
					{
						Assert.That(records[0].VarBinary!.Value, Is.EqualTo(value));
						Assert.That(sql, Does.Contain("VarBinary(10000) -- Binary"));
					});
				}
			}
		}

		private void SetupCustomTypes(MappingSchema ms, bool asDataParameter = false)
		{
			ms.AddScalarType(typeof(VarBinary), DataType.VarBinary);
			ms.AddScalarType(typeof(VarChar),   DataType.VarChar);
			ms.AddScalarType(typeof(NVarChar),  DataType.NVarChar);

			ms.SetConvertExpression<string, VarChar>  (v => new () { Value = v });
			ms.SetConvertExpression<string, NVarChar> (v => new () { Value = v });
			ms.SetConvertExpression<byte[], VarBinary>(v => new () { Value = v });

			if (!asDataParameter)
			{
				ms.SetConvertExpression<VarChar?,   string?>(v => v == null ? null : v.Value);
				ms.SetConvertExpression<NVarChar?,  string?>(v => v == null ? null : v.Value);
				ms.SetConvertExpression<VarBinary?, byte[]?>(v => v == null ? null : v.Value);
			}
			else
			{
				ms.SetConvertExpression<VarChar?,   DataParameter?>(v => v == null ? null : DataParameter.VarChar  (null, v.Value));
				ms.SetConvertExpression<NVarChar?,  DataParameter?>(v => v == null ? null : DataParameter.NVarChar (null, v.Value));
				ms.SetConvertExpression<VarBinary?, DataParameter?>(v => v == null ? null : DataParameter.VarBinary(null, v.Value));
			}
		}
	}
}
