using System;
using System.Diagnostics;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Extensions;

using NUnit.Framework;

namespace Tests.DataProvider
{
	public class DataProviderTestBase : TestBase
	{
		protected string GetNullSql   = "SELECT {0} FROM {1} WHERE ID = 1";
		protected string GetValueSql  = "SELECT {0} FROM {1} WHERE ID = 2";
		protected string PassNullSql  = "SELECT ID FROM {1} WHERE @p IS NULL AND {0} IS NULL OR @p IS NOT NULL AND {0} = @p";
		protected string PassValueSql = "SELECT ID FROM {1} WHERE {0} = @p";

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
			var type = typeof(T).IsGenericTypeEx() && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>) ?
				typeof(T).GetGenericArgumentsEx()[0] : typeof(T);

			// Get NULL value.
			//
			Debug.WriteLine("{0} {1}:{2} -> NULL", fieldName, (object)type.Name, dataType);

			var sql   = string.Format(GetNullSql,  fieldName, tableName);
			var value = conn.Execute<T>(sql);

			Assert.That(value, Is.EqualTo(conn.MappingSchema.GetDefaultValue(typeof(T))));

			int? id;

			if (!skipNull && !skipPass && PassNullSql != null)
			{
				sql = string.Format(PassNullSql, fieldName, tableName);

				if (!skipDefinedNull && dataType != DataType.Undefined)
				{
					// Get NULL ID with dataType.
					//
					Debug.WriteLine("{0} {1}:{2} -> NULL ID with dataType", fieldName, (object)type.Name, dataType);
					id = conn.Execute<int?>(sql, new DataParameter("p", value, dataType));
					Assert.That(id, Is.EqualTo(1));
				}

				if (!skipDefaultNull)
				{
					// Get NULL ID with default dataType.
					//
					Debug.WriteLine("{0} {1}:{2} -> NULL ID with default dataType", fieldName, (object)type.Name, dataType);
					id = conn.Execute<int?>(sql, new { p = value });
					Assert.That(id, Is.EqualTo(1));
				}

				if (!skipUndefinedNull)
				{
					// Get NULL ID without dataType.
					//
					Debug.WriteLine("{0} {1}:{2} -> NULL ID without dataType", fieldName, (object)type.Name, dataType);
					id = conn.Execute<int?>(sql, new DataParameter("p", value));
					Assert.That(id, Is.EqualTo(1));
				}
			}

			// Get value.
			//
			Debug.WriteLine("{0} {1}:{2} -> value", fieldName, (object)type.Name, dataType);
			sql   = string.Format(GetValueSql,  fieldName, tableName);
			value = conn.Execute<T>(sql);

			if (!skipNotNull && !skipPass)
			{
				sql = string.Format(PassValueSql, fieldName, tableName);

				if (!skipDefined && dataType != DataType.Undefined)
				{
					// Get value ID with dataType.
					//
					Debug.WriteLine("{0} {1}:{2} -> value ID with dataType", fieldName, (object)type.Name, dataType);
					id = conn.Execute<int?>(sql, new DataParameter("p", value, dataType));
					Assert.That(id, Is.EqualTo(2));
				}

				if (!skipDefault)
				{
					// Get value ID with default dataType.
					//
					Debug.WriteLine("{0} {1}:{2} -> value ID with default dataType", fieldName, (object)type.Name, dataType);
					id = conn.Execute<int?>(sql, new { p = value });
					Assert.That(id, Is.EqualTo(2));
				}

				if (!skipUndefined)
				{
					// Get value ID without dataType.
					//
					Debug.WriteLine("{0} {1}:{2} -> value ID without dataType", fieldName, (object)type.Name, dataType);
					id = conn.Execute<int?>(sql, new DataParameter("p", value));
					Assert.That(id, Is.EqualTo(2));
				}
			}

			return value;
		}
	}
}
