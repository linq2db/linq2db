# ClickHouse Provider

## Type Mappings

Table below contains information on pre-configured type mappings for ClickHouse provider.

Legend:

- `Database Type` column: ClickHouse data type (only supported types listed)
- `DataType` column: specify `DataType` enum value associated with current database type (could contain multiple values for some types)
- `.NET Type` column: specify .net types that already supported for current database type (no additional mappings configuration required from user except column mapping itself)
  - `(default)` in `.NET Type` column means that this .net type is mapped to specified database type by default. Non-default types should be configurered explicitly by specifying `DataType` in mapping.
- `Type Parameters` column contains information about type parameters and their defaults
- `Notes` column could contain additional information for type, e.g. issues with providers

|Database Type|DataType|.NET Type(s)|Type Parameters|Notes|
|-|-|-|-|-|
|Int8|SByte|1. sbyte (default)<br>2. unmapped Enum:sbyte (default)|||
|UInt8|Byte|1. byte (default)<br>2. bool<br>3. unmapped Enum:byte (default)|||
|Int16|Int16|1. short (default)<br>2. unmapped Enum:short (default)|||
|UInt16|UInt16|1. ushort (default)<br>2. unmapped Enum:ushort (default)|||
|Int32|Int32|1. int (default)<br>2. unmapped Enum:int (default)|||
|UInt32|UInt32|1. uint (default)<br>2. unmapped Enum:uint (default)|||
|Int64|Int64|1. long (default)<br>2. unmapped Enum:long (default)<br>3. TimeSpan (ticks)|||
|UInt64|UInt64|1. ulong (default)<br>2. unmapped Enum:ulong (default)|||
|Int128|Int128|BigInteger|||
|UInt128|UInt128|BigInteger|||
|Int256|Int256|BigInteger (default)|||
|UInt256|UInt256|BigInteger|||
|Float32|Single|float||1. [MySqlConnector]: `Infinity` values require at least `v2.1.11` provider version<br>2. [MySqlConnector]: `NaN` values require at least `v1.3` provider version|
|Float64|Double|double||1. [MySqlConnector]: `Infinity` values require at least `v2.1.11` provider version<br>2. [MySqlConnector]: `NaN` values require at least `v1.3` provider version|
|Decimal32(scale)|Decimal32|1. decimal<br>2. string<br>3. ClickHouseDecimal|scale (default: 10)||
|Decimal64(scale)|Decimal64|1. decimal<br>2. string<br>3. ClickHouseDecimal|scale (default: 10)||
|Decimal128(scale)|Decimal128|1. decimal (default)<br>2. string<br>3. ClickHouseDecimal|scale (default: 10)|1. Octonica provider [doesn't support](https://github.com/Octonica/ClickHouseClient/issues/28) values outside .net decimal type range|
|Decimal256(scale)|Decimal256|1. decimal<br>2. string<br>3. ClickHouseDecimal (default)|scale (default: 10)|1. Octonica provider [doesn't support](https://github.com/Octonica/ClickHouseClient/issues/28) values outside .net decimal type range|
|Bool|Boolean|bool (default)|||
|String|NVarChar<br>VarChar<br>VarBinary<br>|1. string (default)<br>2. byte[] (default)<br>3. Binary<br>4. Guid (DataType.VarChar or DataType.NVarChar)<br>5. Enum mapped to strings with different lengths (default)||1. ClickHouse.Client provider [doesn't support](https://github.com/DarkWanderer/ClickHouse.Client/issues/138) binary data<br>2. MySqlConnector provider [doesn't support](https://github.com/ClickHouse/ClickHouse/issues/38790) binary data|
|FixedString(length)|NChar<br>Char<br>Binary|1. string<br>2. byte[]<br>3. Binary (default)<br>4. Guid (DataType.Char or DataType.NChar, Lenght = 36)<br>5. Enum mapped to strings with same length (default)|length (default: 100)|1. ClickHouse.Client provider [doesn't support](https://github.com/DarkWanderer/ClickHouse.Client/issues/138) binary data<br>2. MySqlConnector provider [doesn't support](https://github.com/ClickHouse/ClickHouse/issues/38790) binary data<br>3. LinqToDB doesn't trim trailing `\x00` bytes|
|UUID|Guid|Guid (default)|||
|Date|Date|1. DateTime<br>2. DateOnly<br>3. DateTimeOffset|||
|Date32|Date32|1. DateTime<br>2. DateOnly (default)<br>3. DateTimeOffset|||
|DateTime|DateTime|1. DateTime<br>2. DateTimeOffset|||
|DateTime64(precision)|DateTime64|1. DateTime (default)<br>2. DateTimeOffset (default)|precision (default: 7)||
|Enum8|Enum8|Enum||Table creation requires full database type specified for column in mapping|
|Enum16|Enum16|Enum||Table creation requires full database type specified for column in mapping|
|JSON|Json|string||Type is still in experimental status in ClickHouse and mostly unusable:<br>1. ClickHouse.Client provider doesn't support JSON type<br>2. Octonica provider doesn't support JSON type<br>3. MySqlConnector returns non-json data from server|
|Interval|IntervalSecond<br>IntervalMinute<br>IntervalHour<br>IntervalDay<br>IntervalWeek<br>IntervalMonth<br>IntervalQuarter<br>IntervalYear|1. byte<br>2. sbyte<br>3. short<br>4. ushort<br>5. int<br>6. uint<br>7. long||As type cannot be used for table columns, support is limited to type name generation and literals generation|
|IPv4|IPv4|1. IPAddress<br>2. uint<br>3. string||MySqlConnector provider [cannot read](https://github.com/ClickHouse/ClickHouse/issues/39056) IPv4 values|
|IPv6|IPv4|1. IPAddress (default)<br>2. string<br>3. byte[4] (ipv4 addresses)<br>byte[16] (ipv6 addresses)|||
