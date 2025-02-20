using System.Diagnostics;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Extensions;
using LinqToDB.Internal.SqlProvider;

using NUnit.Framework;

namespace Tests.DataProvider
{
	public class DataProviderTestBase : TestBase
	{
		protected virtual string  GetNullSql  (DataConnection dc) => "SELECT {0} FROM {1} WHERE ID = 1";
		protected virtual string  GetValueSql (DataConnection dc) => "SELECT {0} FROM {1} WHERE ID = 2";
		protected virtual string? PassNullSql(DataConnection dc, out int paramCount)
		{
			// number of parameters to create for providers with unnamed parameters
			paramCount = 1;
			return "SELECT ID FROM {1} WHERE @p IS NULL AND {0} IS NULL OR @p IS NOT NULL AND {0} = @p";
		}
		protected virtual string  PassValueSql(DataConnection dc) => "SELECT ID FROM {1} WHERE {0} = @p";

		protected T TestType<T>(DataConnection conn, string fieldName,
			DataType dataType          = DataType.Undefined,
			string   tableName         = "AllTypes",
			bool     skipPass          = false,
			bool     skipNull          = false,
			bool     skipDefinedNull   = false,
			bool     skipDefaultNull   = false,
			bool     skipUndefinedNull = false,
			bool     skipNotNull       = false,
			bool     skipDefined       = false,
			bool     skipDefault       = false,
			bool     skipUndefined     = false)
		{
			var type = typeof(T).IsNullable() ? typeof(T).GetGenericArguments()[0] : typeof(T);

			// Get NULL value.
			//
			Debug.WriteLine("{0} {1}:{2} -> NULL", fieldName, type.Name, dataType);

			tableName = conn.DataProvider.CreateSqlBuilder(conn.DataProvider.MappingSchema, conn.Options).ConvertInline(tableName, ConvertType.NameToQueryTable);
			var sql   = string.Format(GetNullSql(conn),  fieldName, tableName);
			var value = conn.Execute<T>(sql);

			Assert.That(value, Is.EqualTo(conn.MappingSchema.GetDefaultValue(typeof(T))));

			int? id;

			var nullSql = PassNullSql(conn, out var paramCount);
			if (!skipNull && !skipPass && nullSql != null)
			{
				sql = string.Format(nullSql, fieldName, tableName);

				if (!skipDefinedNull && dataType != DataType.Undefined)
				{
					// Get NULL ID with dataType.
					//
					Debug.WriteLine("{0} {1}:{2} -> NULL ID with dataType", fieldName, type.Name, dataType);
					var parameters = Enumerable.Range(0, paramCount).Select((_, i) => new DataParameter(paramCount == 1 ? "p" : $"p{i}", value, dataType)).ToArray();
					id = conn.Execute<int?>(sql, parameters);
					Assert.That(id, Is.EqualTo(1));
				}

				if (!skipDefaultNull)
				{
					// Get NULL ID with default dataType.
					//
					Debug.WriteLine("{0} {1}:{2} -> NULL ID with default dataType", fieldName, type.Name, dataType);
					id = conn.Execute<int?>(sql, new { p = value });
					Assert.That(id, Is.EqualTo(1));
				}

				if (!skipUndefinedNull)
				{
					// Get NULL ID without dataType.
					//
					Debug.WriteLine("{0} {1}:{2} -> NULL ID without dataType", fieldName, type.Name, dataType);
					var parameters = Enumerable.Range(0, paramCount).Select((_, i) => new DataParameter(paramCount == 1 ? "p" : $"p{i}", value)).ToArray();
					id = conn.Execute<int?>(sql, parameters);
					Assert.That(id, Is.EqualTo(1));
				}
			}

			// Get value.
			//
			Debug.WriteLine("{0} {1}:{2} -> value", fieldName, type.Name, dataType);
			sql   = string.Format(GetValueSql(conn),  fieldName, tableName);
			value = conn.Execute<T>(sql);

			if (!skipNotNull && !skipPass)
			{
				sql = string.Format(PassValueSql(conn), fieldName, tableName);

				if (!skipDefined && dataType != DataType.Undefined)
				{
					// Get value ID with dataType.
					//
					Debug.WriteLine("{0} {1}:{2} -> value ID with dataType", fieldName, type.Name, dataType);
					id = conn.Execute<int?>(sql, new DataParameter("p", value, dataType));
					Assert.That(id, Is.EqualTo(2));
				}

				if (!skipDefault)
				{
					// Get value ID with default dataType.
					//
					Debug.WriteLine("{0} {1}:{2} -> value ID with default dataType", fieldName, type.Name, dataType);
					id = conn.Execute<int?>(sql, new { p = value });
					Assert.That(id, Is.EqualTo(2));
				}

				if (!skipUndefined)
				{
					// Get value ID without dataType.
					//
					Debug.WriteLine("{0} {1}:{2} -> value ID without dataType", fieldName, type.Name, dataType);
					id = conn.Execute<int?>(sql, new DataParameter("p", value));
					Assert.That(id, Is.EqualTo(2));
				}
			}

			return value;
		}
	}
}
