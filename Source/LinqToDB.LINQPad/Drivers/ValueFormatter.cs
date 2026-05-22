using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using AdoNetCore.AseClient;

using ClickHouse.Driver.Numerics;

using FirebirdSql.Data.Types;

using IBM.Data.DB2Types;

using LINQPad;

using Microsoft.SqlServer.Types;

using MySqlConnector;

using NpgsqlTypes;

using Oracle.ManagedDataAccess.Types;

namespace LinqToDB.LINQPad;

internal static class ValueFormatter
{
	private static readonly object _null = Util.RawHtml(new XElement("span", new XAttribute("style", "text-align:center;"), new XElement("i", new XAttribute("style", "font-style: italic"), "null")));

	// don't use IDatabaseProvider interface as:
	// 1. some providers used by multiple databases
	// 2. user could use those types with any database
	private static readonly FrozenDictionary<Type, Func<object, object>>   _typeConverters;
	private static readonly FrozenDictionary<Type, Func<object, object>>   _baseTypeConverters;
	private static readonly FrozenDictionary<string, Func<object, object>> _byTypeNameConverters;

#pragma warning disable CA1810 // Initialize reference type static fields inline
	static ValueFormatter()
#pragma warning restore CA1810 // Initialize reference type static fields inline
	{
		var typeConverters       = new Dictionary<Type, Func<object, object>>();
		var baseTypeConverters   = new Dictionary<Type, Func<object, object>>();
		var byTypeNameConverters = new Dictionary<string, Func<object, object>>(StringComparer.Ordinal);

		// generic types
		typeConverters.Add(typeof(BigInteger)     , ConvertToString);
		typeConverters.Add(typeof(BitArray)       , ConvertBitArray);
		typeConverters.Add(typeof(BitVector32)    , ConvertToString);
		typeConverters.Add(typeof(PhysicalAddress), ConvertToString);

		// base generic types
		baseTypeConverters.Add(typeof(IPAddress), ConvertToString);

		// provider-specific types

		// SQLCE/SQLSERVER types
		typeConverters.Add(typeof(SqlXml)   , ConvertSqlXml);
		typeConverters.Add(typeof(SqlChars) , ConvertSqlChars);
		typeConverters.Add(typeof(SqlBytes) , ConvertSqlBytes);
		typeConverters.Add(typeof(SqlBinary), ConvertSqlBinary);

		// ClickHouse.Client
		typeConverters.Add(typeof(ClickHouseDecimal), ConvertToString);

		// Firebird
		typeConverters.Add(typeof(FbZonedTime)    , ConvertToString);
		typeConverters.Add(typeof(FbZonedDateTime), ConvertToString);
		typeConverters.Add(typeof(FbDecFloat)     , ConvertFbDecFloat);

		// Sybase ASE
		typeConverters.Add(typeof(AseDecimal), ConvertToString);

		// MySqlConnector
		typeConverters.Add(typeof(MySqlDateTime), ConvertToString);
		typeConverters.Add(typeof(MySqlDecimal) , ConvertToString);
		typeConverters.Add(typeof(MySqlGeometry), ConvertMySqlGeometry);

		// sql server spatial types
		typeConverters.Add(typeof(SqlGeography), ConvertToString);
		typeConverters.Add(typeof(SqlGeometry) , ConvertToString);

		// npgsql
		baseTypeConverters.Add(typeof(NpgsqlTsQuery)      , ConvertToString);
#pragma warning disable CS0618 // Type or member is obsolete
		typeConverters.Add(typeof(NpgsqlInet)             , ConvertToString);
#pragma warning restore CS0618 // Type or member is obsolete
		typeConverters.Add(typeof(NpgsqlInterval)         , ConvertNpgsqlInterval);
		typeConverters.Add(typeof(NpgsqlLogSequenceNumber), ConvertToString);
		typeConverters.Add(typeof(NpgsqlTid)              , ConvertToString);
		typeConverters.Add(typeof(NpgsqlTsVector)         , ConvertToString);
		typeConverters.Add(typeof(NpgsqlLine)             , ConvertToString);
		typeConverters.Add(typeof(NpgsqlCircle)           , ConvertToString);
		typeConverters.Add(typeof(NpgsqlPolygon)          , ConvertToString);
		typeConverters.Add(typeof(NpgsqlPath)             , ConvertToString);
		typeConverters.Add(typeof(NpgsqlBox)              , ConvertToString);
		typeConverters.Add(typeof(NpgsqlLSeg)             , ConvertToString);
		typeConverters.Add(typeof(NpgsqlPoint)            , ConvertToString);

		// oracle
		typeConverters.Add(typeof(OracleClob)        , ConvertOracleClob);
		typeConverters.Add(typeof(OracleBinary)      , ConvertOracleBinary);
		typeConverters.Add(typeof(OracleBoolean)     , ConvertOracleBoolean);
		typeConverters.Add(typeof(OracleDate)        , ConvertToString);
		typeConverters.Add(typeof(OracleDecimal)     , ConvertToString);
		typeConverters.Add(typeof(OracleIntervalDS)  , ConvertToString);
		typeConverters.Add(typeof(OracleIntervalYM)  , ConvertToString);
		typeConverters.Add(typeof(OracleString)      , ConvertToString);
		typeConverters.Add(typeof(OracleTimeStamp)   , ConvertToString);
		typeConverters.Add(typeof(OracleTimeStampLTZ), ConvertToString);
		typeConverters.Add(typeof(OracleTimeStampTZ) , ConvertToString);
		typeConverters.Add(typeof(OracleBlob)        , ConvertOracleBlob);
		typeConverters.Add(typeof(OracleBFile)       , ConvertOracleBFile);
		typeConverters.Add(typeof(OracleXmlType)     , ConvertOracleXmlType);

		// sap hana
		byTypeNameConverters.Add("Sap.Data.Hana.HanaDecimal", ConvertToString);

		// db2
		// use strings to avoid exceptions when DB2 provider loaded from process with wrong bitness
		byTypeNameConverters.Add("IBM.Data.DB2Types.DB2Binary"         , ConvertToString);
		byTypeNameConverters.Add("IBM.Data.DB2Types.DB2Blob"           , ConvertToString);
		byTypeNameConverters.Add("IBM.Data.DB2Types.DB2Clob"           , ConvertToString);
		byTypeNameConverters.Add("IBM.Data.DB2Types.DB2Date"           , ConvertToString);
		byTypeNameConverters.Add("IBM.Data.DB2Types.DB2DateTime"       , ConvertToString);
		byTypeNameConverters.Add("IBM.Data.DB2Types.DB2Decimal"        , ConvertToString);
		byTypeNameConverters.Add("IBM.Data.DB2Types.DB2DecimalFloat"   , ConvertToString);
		byTypeNameConverters.Add("IBM.Data.DB2Types.DB2Double"         , ConvertToString);
		byTypeNameConverters.Add("IBM.Data.DB2Types.DB2Int16"          , ConvertToString);
		byTypeNameConverters.Add("IBM.Data.DB2Types.DB2Int32"          , ConvertToString);
		byTypeNameConverters.Add("IBM.Data.DB2Types.DB2Int64"          , ConvertToString);
		byTypeNameConverters.Add("IBM.Data.DB2Types.DB2Real"           , ConvertToString);
		byTypeNameConverters.Add("IBM.Data.DB2Types.DB2Real370"        , ConvertToString);
		byTypeNameConverters.Add("IBM.Data.DB2Types.DB2RowId"          , ConvertToString);
		byTypeNameConverters.Add("IBM.Data.DB2Types.DB2String"         , ConvertToString);
		byTypeNameConverters.Add("IBM.Data.DB2Types.DB2Time"           , ConvertToString);
		byTypeNameConverters.Add("IBM.Data.DB2Types.DB2TimeStamp"      , ConvertToString);
		byTypeNameConverters.Add("IBM.Data.DB2Types.DB2TimeStampOffset", ConvertToString);
		byTypeNameConverters.Add("IBM.Data.DB2Types.DB2XsrObjectId"    , ConvertToString);
		byTypeNameConverters.Add("IBM.Data.DB2Types.DB2Xml"            , ConvertDB2Xml);

		_typeConverters       = typeConverters.ToFrozenDictionary();
		_baseTypeConverters   = baseTypeConverters.ToFrozenDictionary();
		_byTypeNameConverters = byTypeNameConverters.ToFrozenDictionary();
	}

	public static object Format(object value)
	{
		// handle special NULL values
		if (IsNull(value))
			return _null;

		// convert specialized type to simple value (e.g. string)
		var valueType = value.GetType();
		if (_typeConverters.TryGetValue(valueType, out var converter))
			value = converter(value);
		else if (_byTypeNameConverters.TryGetValue(valueType.FullName!, out converter))
			value = converter(value);
		else
		{
			foreach (var type in _baseTypeConverters.Keys)
				if (type.IsAssignableFrom(valueType))
				{
					value = _baseTypeConverters[type](value);
					break;
				}
		}

		// apply simple values formatting
		return value switch
		{
			string strVal => Format(strVal),
			bool boolVal  => Format(boolVal),
			char[] chars  => Format(chars),
			byte[] binary => Format(binary),
			_             => value,
		};
	}

	private static bool IsNull(object value)
	{
		// note that linqpad will call formatter only for non-primitive values.
		// It will not call it for null and DBNull values so we cannot change their formatting (technically we can do it by formatting owner object, but it doesn't make sense)

		// INullable implemented by System.Data.SqlTypes.Sql* types
		return (value is System.Data.SqlTypes.INullable nullable && nullable.IsNull)
			|| (value is Oracle.ManagedDataAccess.Types.INullable onull && onull.IsNull)
			|| (value.GetType().FullName!.StartsWith("IBM.Data.DB2Types.", StringComparison.Ordinal) && IsDB2Null(value));

		// moved to function to avoid assembly load errors when loaded with wrong process bitness
		static bool IsDB2Null(object value) => value is IBM.Data.DB2Types.INullable db2null && db2null.IsNull;
	}

	#region Final formatters
	private static object Format(string str)
	{
		var components = new List<object>();
		var sb         = new StringBuilder();

		// encode invalid characters as C# escape sequence
		foreach (var chr in str)
		{
			var formattedChar = Format(chr);
			if (formattedChar is string chrStr)
				sb.Append(chrStr);
			else
			{
				if (sb.Length > 0)
				{
					components.Add(sb.ToString());
					sb.Clear();
				}

				components.Add(formattedChar);
			}
		}

		if (sb.Length > 0)
			components.Add(sb.ToString());

		return Util.RawHtml(new XElement("span", [.. components]));
	}

	private static object Format(char[] chars)
	{
		var components = new List<object>();
		var sb         = new StringBuilder();

		// encode invalid characters as C# escape sequence
		var first = true;
		foreach (var chr in chars)
		{
			if (first)
				first = false;
			else
				sb.Append(' ');

			var formattedChar = Format(chr);
			if (formattedChar is string chrStr)
				sb.Append(chrStr);
			else
			{
				if (sb.Length > 0)
				{
					components.Add(sb.ToString());
					sb.Clear();
				}

				components.Add(formattedChar);
			}
		}

		if (sb.Length > 0)
			components.Add(sb.ToString());

		return Util.RawHtml(new XElement("span", [.. components]));
	}

	private static object Format(byte[] value)
	{
		var sb = new StringBuilder($" Len:{value.Length} ");

		int i;

		for (i = 0; i < value.Length && i < 10; i++)
			sb.Append(CultureInfo.InvariantCulture, $"{value[i]:X2}:");

		if (i > 0)
			sb.Length--;

		if (i < value.Length)
			sb.Append("...");

		return Util.RawHtml(new XElement("span", sb.ToString()));
	}

	private static object Format(char chr)
	{
		if (!XmlConvert.IsXmlChar(chr) && !char.IsHighSurrogate(chr) && !char.IsLowSurrogate(chr))
		{
			return new XElement(
				"span", 
				new XElement(
					"i",
					new XAttribute("style", "font-style: italic"),
					chr <= 255 
						? string.Create(CultureInfo.InvariantCulture, $"\\x{((short)chr):X2}")
						: string.Create(CultureInfo.InvariantCulture, $"\\u{((short)chr):X4}")
				)
			);
		}

		return chr.ToString();
	}

	private static object Format(bool value) => Util.RawHtml(new XElement("span", value.ToString()));
	#endregion

	#region Primitives (final types)

	// for types that already implement rendering of all data using ToString
	private static object ConvertToString(object value) => string.Create(CultureInfo.InvariantCulture, $"{value}");

	#region Runtime
	private static object ConvertBitArray(object value)
	{
		var val = (BitArray)value;
		var sb  = new StringBuilder($" Len:{val.Length} 0b");

		int i;

		for (i = 0; i < val.Length && i < 64; i++)
			sb.Append(val[i] ? '1' : '0');

		if (i < val.Length)
			sb.Append("...");

		return sb.ToString();
	}
	#endregion

	#region Npgsql
	private static object ConvertNpgsqlInterval(object value)
	{
		var val = (NpgsqlInterval)value;
		// let's use ISO8601 duration format
		// Time is microseconds
		return string.Create(CultureInfo.InvariantCulture, $"P{val.Months}M{val.Days}DT{((decimal)val.Time) / 1_000_000}S");
	}
	#endregion

	#region MySqlConnector
	private static object ConvertMySqlGeometry(object value)
	{
		var val = (MySqlGeometry)value;
		return new { SRID = val.SRID, WKB = val.Value.Skip(4) };
	}
	#endregion

	#region Firebird
	private static object ConvertFbDecFloat(object value)
	{
		// type reders as {Coefficient}E{Exponent} which is not very noice
		var typedValue = (FbDecFloat)value!;
		var isNegative = typedValue.Coefficient < 0;
		var strValue   = (isNegative ? BigInteger.Negate(typedValue.Coefficient) : typedValue.Coefficient).ToString(CultureInfo.InvariantCulture);

		// semi-localized rendering...
		if (typedValue.Exponent < 0)
		{
			var exp = -typedValue.Exponent;
			if (exp < strValue.Length)
				strValue = strValue.Insert(strValue.Length - exp, CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
			else if (exp == strValue.Length)
				strValue = $"0{CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator}{strValue}";
			else // Exponent > len(Coefficient)
				strValue = $"0{CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator}{new string('0', exp - strValue.Length)}{strValue}";
		}
		else if (typedValue.Exponent > 0)
			strValue = $"{strValue}{new string('0', typedValue.Exponent)}";

		return isNegative ? $"-{strValue}" : strValue;
	}
	#endregion

	#region Sql*
	private static object ConvertSqlXml   (object value) => ((SqlXml)value).Value;
	private static object ConvertSqlChars (object value) => ((SqlChars)value).Value;
	private static object ConvertSqlBytes (object value) => ((SqlBytes)value).Value;
	private static object ConvertSqlBinary(object value) => ((SqlBinary)value).Value;
	#endregion

	#region Oracle
	private static object ConvertOracleClob   (object value) => $"OracleClob(Length = {((OracleClob)value).Length})";
	private static object ConvertOracleBlob   (object value) => $"OracleBlob(Length = {((OracleBlob)value).Length})";
	private static object ConvertOracleBFile  (object value) => $"OracleBFile(Directory = {((OracleBFile)value).DirectoryName}, FileName = {((OracleBFile)value).FileName})";
	private static object ConvertOracleBinary (object value) => ((OracleBinary)value).Value;
	private static object ConvertOracleBoolean(object value) => ((OracleBoolean)value).Value;
	private static object ConvertOracleXmlType(object value) => ((OracleXmlType)value).Value;
	#endregion

	#region DB2
	private static object ConvertDB2Xml(object value) => ((DB2Xml)value).GetString();
	#endregion

	#endregion
}
