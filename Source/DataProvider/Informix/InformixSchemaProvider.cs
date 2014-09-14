using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.DataProvider.Informix
{
	using Common;
	using Data;
	using SchemaProvider;

	class InformixSchemaProvider : SchemaProviderBase
	{
		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			return new[]
			{
				new DataTypeInfo { TypeName = "CHAR",       DataType = typeof(string).  FullName, CreateFormat = "CHAR({0})",        CreateParameters = "length" },
				new DataTypeInfo { TypeName = "SMALLINT",   DataType = typeof(short).   FullName },
				new DataTypeInfo { TypeName = "INTEGER",    DataType = typeof(int).     FullName },
				new DataTypeInfo { TypeName = "FLOAT",      DataType = typeof(double).  FullName },
				new DataTypeInfo { TypeName = "SMALLFLOAT", DataType = typeof(float).   FullName },
				new DataTypeInfo { TypeName = "DECIMAL",    DataType = typeof(decimal). FullName, CreateFormat = "DECIMAL({0},{1})", CreateParameters = "precision,scale" },
				new DataTypeInfo { TypeName = "SERIAL",     DataType = typeof(int).     FullName },
				new DataTypeInfo { TypeName = "DATE",       DataType = typeof(DateTime).FullName },
				new DataTypeInfo { TypeName = "MONEY",      DataType = typeof(decimal). FullName, CreateFormat = "MONEY({0},{1})",   CreateParameters = "precision,scale" },
				new DataTypeInfo { TypeName = "DATETIME",   DataType = typeof(DateTime).FullName },
				new DataTypeInfo { TypeName = "BYTE",       DataType = typeof(byte[]).  FullName },
				new DataTypeInfo { TypeName = "TEXT",       DataType = typeof(string).  FullName },
				new DataTypeInfo { TypeName = "VARCHAR",    DataType = typeof(string).  FullName, CreateFormat = "VARCHAR({0})",     CreateParameters = "length" },
				new DataTypeInfo { TypeName = "INTERVAL",   DataType = typeof(TimeSpan).FullName },
				new DataTypeInfo { TypeName = "NCHAR",      DataType = typeof(string).  FullName, CreateFormat = "NCHAR({0})",       CreateParameters = "length" },
				new DataTypeInfo { TypeName = "NVARCHAR",   DataType = typeof(string).  FullName, CreateFormat = "NVARCHAR({0})",    CreateParameters = "length" },
				new DataTypeInfo { TypeName = "INT8",       DataType = typeof(long).    FullName },
				new DataTypeInfo { TypeName = "SERIAL8",    DataType = typeof(long).    FullName },
				new DataTypeInfo { TypeName = "LVARCHAR",   DataType = typeof(string).  FullName, CreateFormat = "LVARCHAR({0})",    CreateParameters = "length" },
				new DataTypeInfo { TypeName = "BOOLEAN",    DataType = typeof(bool).    FullName },
				new DataTypeInfo { TypeName = "BIGINT",     DataType = typeof(long).    FullName },
				new DataTypeInfo { TypeName = "BIGSERIAL",  DataType = typeof(long).    FullName },
				//new DataTypeInfo { TypeName = "SET",        DataType = typeof(object).  FullName },
				//new DataTypeInfo { TypeName = "MULTISET",   DataType = typeof(object).  FullName },
				//new DataTypeInfo { TypeName = "LIST",       DataType = typeof(object).  FullName },
				//new DataTypeInfo { TypeName = "ROW",        DataType = typeof(object).  FullName },
				//new DataTypeInfo { TypeName = "COLLECTION", DataType = typeof(object).  FullName },
			}.ToList();
		}

		protected override DataType GetDataType(string dataType, string columnType)
		{
			switch (dataType)
			{
				case "CHAR"       : return DataType.Char;
				case "SMALLINT"   : return DataType.Int16;
				case "INTEGER"    : return DataType.Int32;
				case "FLOAT"      : return DataType.Double;
				case "SMALLFLOAT" : return DataType.Single;
				case "DECIMAL"    : return DataType.Decimal;
				case "SERIAL"     : return DataType.Int32;
				case "DATE"       : return DataType.DateTime;
				case "MONEY"      : return DataType.Decimal;
				case "DATETIME"   : return DataType.DateTime;
				case "BYTE"       : return DataType.Binary;
				case "TEXT"       : return DataType.Text;
				case "VARCHAR"    : return DataType.VarChar;
				case "INTERVAL"   : return DataType.Time;
				case "NCHAR"      : return DataType.NChar;
				case "NVARCHAR"   : return DataType.NVarChar;
				case "INT8"       : return DataType.Int64;
				case "SERIAL8"    : return DataType.Int64;
				case "LVARCHAR"   : return DataType.VarChar;
				case "BOOLEAN"    : return DataType.Boolean;
				case "BIGINT"     : return DataType.Int64;
				case "BIGSERIAL"  : return DataType.Int64;
				//case "SET"        : return DataType.object).  ;
				//case "MULTISET"   : return DataType.object).  ;
				//case "LIST"       : return DataType.object).  ;
				//case "ROW"        : return DataType.object).  ;
				//case "COLLECTION" : return DataType.object).  ;
			}

			return DataType.Undefined;
		}

		protected override List<TableInfo> GetTables(DataConnection dataConnection)
		{
			return dataConnection.Query<TableInfo>(@"
				SELECT
					tabid         as TableID,
					tabname       as TableName,
					1             as IsDefaultSchema,
					tabtype = 'V' as IsView
				FROM
					systables
				WHERE
					tabid >= 100")
				.ToList();
		}

		List<PrimaryKeyInfo> _pks;

		protected override List<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection)
		{
			return _pks =
			(
				from pk in dataConnection.Query(
					rd =>
					{
						var arr = new string[16];

						for (var i = 0; i < arr.Length; i++)
						{
							var value = rd["col" + (i + 1)];
							arr[i] = value is DBNull ? null : (string)value;
						}

						return new
						{
							TableID        = rd["tabid"].ToString(),
							PrimaryKeyName = (string)rd["idxname"],
							arr
						};
					}, @"
						SELECT
							t.tabid,
							x.idxname,
							(SELECT colname FROM syscolumns c WHERE c.tabid = t.tabid AND c.colno = x.part1)  as col1,
							(SELECT colname FROM syscolumns c WHERE c.tabid = t.tabid AND c.colno = x.part2)  as col2,
							(SELECT colname FROM syscolumns c WHERE c.tabid = t.tabid AND c.colno = x.part3)  as col3,
							(SELECT colname FROM syscolumns c WHERE c.tabid = t.tabid AND c.colno = x.part4)  as col4,
							(SELECT colname FROM syscolumns c WHERE c.tabid = t.tabid AND c.colno = x.part5)  as col5,
							(SELECT colname FROM syscolumns c WHERE c.tabid = t.tabid AND c.colno = x.part6)  as col6,
							(SELECT colname FROM syscolumns c WHERE c.tabid = t.tabid AND c.colno = x.part7)  as col7,
							(SELECT colname FROM syscolumns c WHERE c.tabid = t.tabid AND c.colno = x.part8)  as col8,
							(SELECT colname FROM syscolumns c WHERE c.tabid = t.tabid AND c.colno = x.part9)  as col9,
							(SELECT colname FROM syscolumns c WHERE c.tabid = t.tabid AND c.colno = x.part10) as col10,
							(SELECT colname FROM syscolumns c WHERE c.tabid = t.tabid AND c.colno = x.part11) as col11,
							(SELECT colname FROM syscolumns c WHERE c.tabid = t.tabid AND c.colno = x.part12) as col12,
							(SELECT colname FROM syscolumns c WHERE c.tabid = t.tabid AND c.colno = x.part13) as col13,
							(SELECT colname FROM syscolumns c WHERE c.tabid = t.tabid AND c.colno = x.part14) as col14,
							(SELECT colname FROM syscolumns c WHERE c.tabid = t.tabid AND c.colno = x.part15) as col15,
							(SELECT colname FROM syscolumns c WHERE c.tabid = t.tabid AND c.colno = x.part16) as col16
						FROM systables t
							JOIN sysindexes x ON t.tabid = x.tabid
						WHERE t.tabid >= 100 AND x.idxtype = 'U'")
				group pk by pk.TableID into gr
				select gr.First() into pk
				from c in pk.arr.Select((c,i) => new { c, i })
				where c.c != null
				select new PrimaryKeyInfo
				{
					TableID        = pk.TableID,
					PrimaryKeyName = pk.PrimaryKeyName,
					ColumnName     = c.c,
					Ordinal        = c.i
				}
			).ToList();
		}

		static void SetDate(ColumnInfo c, int num)
		{
			var arr = new[]
			{
				new { datetype = "",            start_point =  0, end_point =  0 },
				new { datetype = "YEAR",        start_point =  1, end_point =  5 },
				new { datetype = "",            start_point =  0, end_point =  0 },
				new { datetype = "MONTH",       start_point =  5, end_point =  7 },
				new { datetype = "",            start_point =  0, end_point =  0 },
				new { datetype = "DAY",         start_point =  7, end_point =  9 },
				new { datetype = "",            start_point =  0, end_point =  0 },
				new { datetype = "HOUR",        start_point =  9, end_point = 11 },
				new { datetype = "",            start_point =  0, end_point =  0 },
				new { datetype = "MINUTE",      start_point = 11, end_point = 13 },
				new { datetype = "",            start_point =  0, end_point =  0 },
				new { datetype = "SECOND",      start_point = 13, end_point = 15 },
				new { datetype = "FRACTION(1)", start_point = 15, end_point = 16 },
				new { datetype = "FRACTION(2)", start_point = 16, end_point = 17 },
				new { datetype = "FRACTION(3)", start_point = 17, end_point = 18 },
				new { datetype = "FRACTION(4)", start_point = 18, end_point = 19 },
				new { datetype = "FRACTION(5)", start_point = 19, end_point = 20 },
			};

			var i = num % 16 + 1;       // offset again
			var j = num % 256 / 16 + 1; // offset again
			var k = num / 256;          // length of value

			/*
				If this is an interval then life gets interesting, 'k' is
				the length of the entire string.  So a YEAR TO DAY is
				YYYYMMDD or 8.  A DAY(3) TO MINUTE is DDDHHMM or 7.
				We don't know how long the first one is, but
				we can work it out by computing the 'should
				be length' of the string and then adding/subtracting
				the result from the 'should be length' of
				the major element.

				Keep in mind --->    YYYYMMDDHHMMSSFFFFF
				    vs.         j =    1  2 3 4 5 678901

				I was just working an algorithm to do this, 4
				notepads, 90 minutes, and 50 lines into it I realized
				that I was creating something impossible to test or
				maintain.  Therefore I am opting for something a lot simpler.

				In the globals I have created an ARRAY of RECORD with
				start and end points for the major and minor pieces.
				By subtracting the START point of the
				major element from the END point of the minor element
				I get the 'should be length'
			*/

			var len = arr[i].end_point - arr[j].start_point;

			/*
				len should match k. e.g.:
					DAY(5)  TO MINUTE ==> k =  9, len =  6
					YEAR(6) TO HOUR   ==> k = 12, len = 14
			*/

			len = k - len; // add len to the major

			if (len == 0 || j > 11) // is the default 12 on have the precision already coded
			{
				c.ColumnType = c.DataType + " " + arr[j].datetype + " TO " + arr[i].datetype;
			}
			else // # isn't the default
			{
				// uh-oh, how long IS the default major?
				k = arr[j].end_point - arr[j].start_point;
 
				// add in the extra
				k = k + len;
 
				c.ColumnType = c.DataType + " " + arr[j].datetype + " (" + k + ") TO " + arr[i].datetype;
				c.Precision = 5;
			}
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection)
		{
			return dataConnection
				.Query<ColumnInfo>(@"
					SELECT
						c.tabid     as TableID,
						c.colname   as Name,
						c.colno     as Ordinal,
						c.coltype   as DataType,
						c.collength as Length
					FROM systables t
						JOIN syscolumns c ON t.tabid = c.tabid
					WHERE t.tabid >= 100")
				.Select(c =>
				{
					var typeid = ConvertTo<int>.From(c.DataType);
					var len    = c.Length;

					c.Length     = 0;
					c.IsNullable = (typeid & 0x100) != 0x100;

					switch (typeid & 0xFF)
					{
						case    0 :
							c.DataType = "CHAR";
							c.Length = len;
							break;
						case    1 : c.DataType = "SMALLINT";         break;
						case    2 : c.DataType = "INTEGER";          break;
						case    3 : c.DataType = "FLOAT";            break;
						case    4 : c.DataType = "SMALLFLOAT";       break;
						case    5 :
							c.DataType  = "DECIMAL";
							c.Precision = (int)(len / 256);
							if (c.Precision >= len % 256)
								c.Scale = (int)(len % 256);
							break;
						case    6 :
							c.DataType   = "SERIAL";
							c.IsIdentity = c.SkipOnInsert = c.SkipOnUpdate = true;
							break;
						case    7 : c.DataType = "DATE";             break;
						case    8 :
							c.DataType  = "MONEY";
							c.Precision = (int)(len / 256);
							if (c.Precision >= len % 256)
								c.Scale = (int)(len % 256);
							break;
						case    9 : c.DataType = "NULL";             break;
						case   10 :
							c.DataType = "DATETIME";
							SetDate(c, (int)len);
							break;
						case   11 : c.DataType = "BYTE";             break;
						case   12 : c.DataType = "TEXT";             break;
						case   13 :
							c.DataType = "VARCHAR";
							c.Length   = len;
							break;
						case   14 :
							c.DataType = "INTERVAL";
							SetDate(c, (int)len);
							break;
						case   15 :
							c.DataType = "NCHAR";
							c.Length   = len;
							break;
						case   16 :
							c.DataType = "NVARCHAR";
							c.Length   = len;
							break;
						case   17 : c.DataType = "INT8";             break;
						case   18 : c.DataType = "SERIAL8";          break;
						case   19 : c.DataType = "SET";              break;
						case   20 : c.DataType = "MULTISET";         break;
						case   21 : c.DataType = "LIST";             break;
						case   22 : c.DataType = "ROW";              break;
						case   23 : c.DataType = "COLLECTION";       break;
						case   40 : // Variable-length opaque type
							c.DataType = "LVARCHAR";
							c.Length   = len;
							break;
						case   41 : c.DataType = "BOOLEAN";          break; // Fixed-length opaque type
						case   43 :
							c.DataType = "LVARCHAR";
							c.Length   = len;
							break;
						case   45 : c.DataType = "BOOLEAN";          break;
						case   52 : c.DataType = "BIGINT";           break;
						case   53 :
							c.DataType   = "BIGSERIAL";
							c.IsIdentity = c.SkipOnInsert = c.SkipOnUpdate = true;
							break;
						case 2061 : c.DataType = "IDSSECURITYLABEL"; break;
						case 4118 : c.DataType = "ROW";              break;
					}

					return c;
				})
				.ToList();
		}

		protected override List<ForeingKeyInfo> GetForeignKeys(DataConnection dataConnection)
		{
			var names = new HashSet<string>();

			return
			(
				from fk in dataConnection.Query(
					rd =>
					{
						var arr = new string[16][];

						for (var i = 0; i < arr.Length; i++)
						{
							var value1 = rd["ThisCol"  + (i + 1)];
							var value2 = rd["OtherCol" + (i + 1)];

							arr[i] = new[]
							{
								value1 is DBNull ? null : (string)value1,
								value2 is DBNull ? null : (string)value2
							};
						}

						var id           = rd["ID"].ToString();
						var name         = rd["Name"].ToString();
						var thisTableID  = rd["ThisTableID"]. ToString();
						var otherTableID = rd["OtherTableID"].ToString();

						if (name.StartsWith("r"))
						{
							var ns = name.Substring(1).Split('_');

							if (ns.Length == 2 && ns[0] == thisTableID && ns[1] == id)
							{
								name = "FK_" + rd["ThisTableName"] + "_" + rd["OtherTableName"];

								var origName = name;
								var n        = 0;

								while (names.Contains(name))
									name = origName + "_" + ++n;

								names.Add(name);
							}
						}

						return new
						{
							Name         = name,
							ThisTableID  = thisTableID,
							OtherTableID = otherTableID,
							arr
						};
					}, @"
						SELECT
							r.constrid    as ID,
							tc.constrname as Name,
							tc.tabid      as ThisTableID,
							tt.tabname    as ThisTableName,
							(SELECT colname FROM syscolumns c WHERE c.tabid = tc.tabid AND c.colno = tx.part1)  as ThisCol1,
							(SELECT colname FROM syscolumns c WHERE c.tabid = tc.tabid AND c.colno = tx.part2)  as ThisCol2,
							(SELECT colname FROM syscolumns c WHERE c.tabid = tc.tabid AND c.colno = tx.part3)  as ThisCol3,
							(SELECT colname FROM syscolumns c WHERE c.tabid = tc.tabid AND c.colno = tx.part4)  as ThisCol4,
							(SELECT colname FROM syscolumns c WHERE c.tabid = tc.tabid AND c.colno = tx.part5)  as ThisCol5,
							(SELECT colname FROM syscolumns c WHERE c.tabid = tc.tabid AND c.colno = tx.part6)  as ThisCol6,
							(SELECT colname FROM syscolumns c WHERE c.tabid = tc.tabid AND c.colno = tx.part7)  as ThisCol7,
							(SELECT colname FROM syscolumns c WHERE c.tabid = tc.tabid AND c.colno = tx.part8)  as ThisCol8,
							(SELECT colname FROM syscolumns c WHERE c.tabid = tc.tabid AND c.colno = tx.part9)  as ThisCol9,
							(SELECT colname FROM syscolumns c WHERE c.tabid = tc.tabid AND c.colno = tx.part10) as ThisCol10,
							(SELECT colname FROM syscolumns c WHERE c.tabid = tc.tabid AND c.colno = tx.part11) as ThisCol11,
							(SELECT colname FROM syscolumns c WHERE c.tabid = tc.tabid AND c.colno = tx.part12) as ThisCol12,
							(SELECT colname FROM syscolumns c WHERE c.tabid = tc.tabid AND c.colno = tx.part13) as ThisCol13,
							(SELECT colname FROM syscolumns c WHERE c.tabid = tc.tabid AND c.colno = tx.part14) as ThisCol14,
							(SELECT colname FROM syscolumns c WHERE c.tabid = tc.tabid AND c.colno = tx.part15) as ThisCol15,
							(SELECT colname FROM syscolumns c WHERE c.tabid = tc.tabid AND c.colno = tx.part16) as ThisCol16,
							oc.tabid      as OtherTableID,
							ot.tabname    as OtherTableName,
							(SELECT colname FROM syscolumns c WHERE c.tabid = oc.tabid AND c.colno = ox.part1)  as OtherCol1,
							(SELECT colname FROM syscolumns c WHERE c.tabid = oc.tabid AND c.colno = ox.part2)  as OtherCol2,
							(SELECT colname FROM syscolumns c WHERE c.tabid = oc.tabid AND c.colno = ox.part3)  as OtherCol3,
							(SELECT colname FROM syscolumns c WHERE c.tabid = oc.tabid AND c.colno = ox.part4)  as OtherCol4,
							(SELECT colname FROM syscolumns c WHERE c.tabid = oc.tabid AND c.colno = ox.part5)  as OtherCol5,
							(SELECT colname FROM syscolumns c WHERE c.tabid = oc.tabid AND c.colno = ox.part6)  as OtherCol6,
							(SELECT colname FROM syscolumns c WHERE c.tabid = oc.tabid AND c.colno = ox.part7)  as OtherCol7,
							(SELECT colname FROM syscolumns c WHERE c.tabid = oc.tabid AND c.colno = ox.part8)  as OtherCol8,
							(SELECT colname FROM syscolumns c WHERE c.tabid = oc.tabid AND c.colno = ox.part9)  as OtherCol9,
							(SELECT colname FROM syscolumns c WHERE c.tabid = oc.tabid AND c.colno = ox.part10) as OtherCol10,
							(SELECT colname FROM syscolumns c WHERE c.tabid = oc.tabid AND c.colno = ox.part11) as OtherCol11,
							(SELECT colname FROM syscolumns c WHERE c.tabid = oc.tabid AND c.colno = ox.part12) as OtherCol12,
							(SELECT colname FROM syscolumns c WHERE c.tabid = oc.tabid AND c.colno = ox.part13) as OtherCol13,
							(SELECT colname FROM syscolumns c WHERE c.tabid = oc.tabid AND c.colno = ox.part14) as OtherCol14,
							(SELECT colname FROM syscolumns c WHERE c.tabid = oc.tabid AND c.colno = ox.part15) as OtherCol15,
							(SELECT colname FROM syscolumns c WHERE c.tabid = oc.tabid AND c.colno = ox.part16) as OtherCol16
						FROM
							sysreferences r
								JOIN sysconstraints tc ON r.constrid = tc.constrid
									JOIN sysindexes tx ON tc.tabid   = tx.tabid AND tc.idxname = tx.idxname
									JOIN systables  tt ON tc.tabid   = tt.tabid
								JOIN sysconstraints oc ON r.primary  = oc.constrid
									JOIN sysindexes ox ON oc.tabid   = ox.tabid AND oc.idxname = ox.idxname
									JOIN systables  ot ON oc.tabid   = ot.tabid")
				from c in fk.arr.Select((c,i) => new { c, i })
				where c.c[0] != null
				select new ForeingKeyInfo
				{
					Name         = fk.Name.Trim(),
					ThisTableID  = fk.ThisTableID,
					ThisColumn   = c.c[0],
					OtherTableID = fk.OtherTableID,
					OtherColumn  = c.c[1],
					Ordinal      = c.i
				}
			).ToList();
		}
	}
}
