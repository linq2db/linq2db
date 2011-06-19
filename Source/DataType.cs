using System;

namespace LinqToDB
{
	public enum DataType
	{
		/// <summary>
		/// A fixed-length stream of non-Unicode characters ranging between 1 and 8,000 characters.
		/// </summary>
		Char,

		/// <summary>
		/// A variable-length stream of non-Unicode characters ranging between 1 and 8,000 characters.
		/// Use VarChar when the database column is varchar(max).
		/// </summary>
		VarChar,

		/// <summary>
		/// A variable-length stream of non-Unicode data with a maximum length of 2 31 -1 (or 2,147,483,647) characters.
		/// </summary>
		Text,

		/// <summary>
		/// A fixed-length stream of Unicode characters ranging between 1 and 4,000 characters.
		/// </summary>
		NChar,

		/// <summary>
		/// A variable-length stream of Unicode characters ranging between 1 and 4,000 characters.
		/// Implicit conversion fails if the string is greater than 4,000 characters.
		/// </summary>
		NVarChar,

		/// <summary>
		/// A variable-length stream of Unicode data with a maximum length of 2 30 - 1 (or 1,073,741,823) characters.
		/// </summary>
		NText,

		/// <summary>
		/// A fixed-length stream of binary data ranging between 1 and 8,000 bytes.
		/// </summary>
		Binary,

		/// <summary>
		/// A variable-length stream of binary data ranging between 1 and 8,000 bytes.
		/// Implicit conversion fails if the byte array is greater than 8,000 bytes.
		/// </summary>
		VarBinary,

		/// <summary>
		/// A variable-length stream of binary data ranging from 0 to 2 31 -1 (or 2,147,483,647) bytes.
		/// </summary>
		Image,

		/// <summary>
		/// A simple type representing Boolean values of true or false.
		/// </summary>
		Boolean,

		/// <summary>
		/// A globally unique identifier (or GUID).
		/// </summary>
		Guid,

		/// <summary>
		/// An integral type representing signed 8-bit integers with values between -128 and 127.
		/// </summary>
		SByte,

		/// <summary>
		/// An integral type representing signed 16-bit integers with values between -32768 and 32767.
		/// </summary>
		Int16,

		/// <summary>
		/// An integral type representing signed 32-bit integers with values between -2147483648 and 2147483647.
		/// </summary>
		Int32,

		/// <summary>
		/// An integral type representing signed 64-bit integers with values between -9223372036854775808 and 9223372036854775807.
		/// </summary>
		Int64,

		/// <summary>
		/// An 8-bit unsigned integer ranging in value from 0 to 255.
		/// </summary>
		Byte,

		/// <summary>
		/// An integral type representing unsigned 16-bit integers with values between 0 and 65535.
		/// </summary>
		UInt16,

		/// <summary>
		/// An integral type representing unsigned 32-bit integers with values between 0 and 4294967295.
		/// </summary>
		UInt32,

		/// <summary>
		/// An integral type representing unsigned 64-bit integers with values between 0 and 18446744073709551615.
		/// </summary>
		UInt64,

		/// <summary>
		/// A floating point number within the range of -3.40E +38 through 3.40E +38.
		/// </summary>
		Single,

		/// <summary>
		/// A floating point number within the range of -1.79E +308 through 1.79E +308.
		/// </summary>
		Double,

		/// <summary>
		/// A simple type representing values ranging from 1.0 x 10 -28 to approximately 7.9 x 10 28 with 28-29 significant digits.
		/// </summary>
		Decimal,

		/// <summary>
		/// A currency value ranging from -2 63 (or -9,223,372,036,854,775,808) to 2 63 -1 (or +9,223,372,036,854,775,807)
		/// with an accuracy to a ten-thousandth of a currency unit.
		/// </summary>
		Money,

		/// <summary>
		/// A currency value ranging from -214,748.3648 to +214,748.3647 with an accuracy to a ten-thousandth of a currency unit.
		/// </summary>
		SmallMoney,


	/*
	DateTime	 DateTime . Date and time data ranging in value from January 1, 1753 to December 31, 9999 to an accuracy of 3.33 milliseconds.
	SmallDateTime	 DateTime . Date and time data ranging in value from January 1, 1900 to June 6, 2079 to an accuracy of one minute.
	Timestamp	 Array of type Byte. Automatically generated binary numbers, which are guaranteed to be unique within a database. timestamp is used typically as a mechanism for version-stamping table rows. The storage size is 8 bytes.
	Variant	 Object . A special data type that can contain numeric, string, binary, or date data as well as the SQL Server values Empty and Null, which is assumed if no other type is declared.
	Xml	An XML value. Obtain the XML as a string using the GetValue method or Value property, or as an XmlReader by calling the CreateReader method.
	Udt	A SQL Server 2005 user-defined type (UDT).
	Structured	A special data type for specifying structured data contained in table-valued parameters.
	Date	Date data ranging in value from January 1,1 AD through December 31, 9999 AD.
	Time	Time data based on a 24-hour clock. Time value range is 00:00:00 through 23:59:59.9999999 with an accuracy of 100 nanoseconds. Corresponds to a SQL Server time value.
	DateTime2	Date and time data. Date value range is from January 1,1 AD through December 31, 9999 AD. Time value range is 00:00:00 through 23:59:59.9999999 with an accuracy of 100 nanoseconds.
	DateTimeOffset	Date and time data with time zone awareness. Date value range is from January 1,1 AD through December 31, 9999 AD. Time value range is 00:00:00 through 23:59:59.9999999 with an accuracy of 100 nanoseconds. Time zone value range is -14:00 through +14:00.

	Date	A type representing a date value.
	DateTime	A type representing a date and time value.
	Object	A general type representing any reference or value type not explicitly represented by another DbType value.
	Time	A type representing a SQL Server DateTime value. If you want to use a SQL Server time value, use Time.
	VarNumeric	A variable-length numeric value.
	Xml	A parsed representation of an XML document or fragment.
	DateTime2	Date and time data. Date value range is from January 1,1 AD through December 31, 9999 AD. Time value range is 00:00:00 through 23:59:59.9999999 with an accuracy of 100 nanoseconds.
	DateTimeOffset	Date and time data with time zone awareness. Date value range is from January 1,1 AD through December 31, 9999 AD. Time value range is 00:00:00 through 23:59:59.9999999 with an accuracy of 100 nanoseconds. Time zone value range is -14:00 through +14:00.		 */
	}
}
