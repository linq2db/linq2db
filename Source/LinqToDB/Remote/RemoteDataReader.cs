using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

using LinqToDB.Mapping;

namespace LinqToDB.Remote
{
	sealed class RemoteDataReader : DbDataReader
	{
		public RemoteDataReader(MappingSchema mappingSchema, LinqServiceResult result)
		{
			_mappingSchema = mappingSchema;
			_result = result;

			for (var i = 0; i < result.FieldNames.Length; i++)
				_ordinal.Add(result.FieldNames[i], i);
		}

		readonly MappingSchema          _mappingSchema;
		readonly LinqServiceResult      _result;
		readonly Dictionary<string,int> _ordinal = new ();

		string?[]? _data;
		int        _current = -1;

		#region DbDataRecord

		public override object this[int ordinal] => GetValue(ordinal)!;
		public override object this[string name] => GetValue(GetOrdinal(name))!;

		public override int  FieldCount => _result.FieldCount;
		public override bool HasRows    => _result.Data.Count > 0;
		public override int  Depth      => 0;

		public override bool     GetBoolean (int ordinal) => (bool    )GetValue(ordinal)!;
		public override byte     GetByte    (int ordinal) => (byte    )GetValue(ordinal)!;
		public override char     GetChar    (int ordinal) => (char    )GetValue(ordinal)!;
		public override DateTime GetDateTime(int ordinal) => (DateTime)GetValue(ordinal)!;
		public override decimal  GetDecimal (int ordinal) => (decimal )GetValue(ordinal)!;
		public override double   GetDouble  (int ordinal) => (double  )GetValue(ordinal)!;
		public override float    GetFloat   (int ordinal) => (float   )GetValue(ordinal)!;
		public override Guid     GetGuid    (int ordinal) => (Guid    )GetValue(ordinal)!;
		public override short    GetInt16   (int ordinal) => (short   )GetValue(ordinal)!;
		public override int      GetInt32   (int ordinal) => (int     )GetValue(ordinal)!;
		public override long     GetInt64   (int ordinal) => (long    )GetValue(ordinal)!;
		public override string   GetString  (int ordinal) => (string  )GetValue(ordinal)!;

		public override object GetValue(int ordinal) => GetValueInternal(ordinal) ?? throw new InvalidOperationException("Value is NULL");

		private object? GetValueInternal(int ordinal)
		{
			var type = _result.FieldTypes[ordinal];
			var value = _data![ordinal];

			return SerializationConverter.Deserialize(_mappingSchema, type, value);
		}

		public override bool IsDBNull(int ordinal) => _data![ordinal] == null;

		public override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).FullName!;
		public override Type   GetFieldType   (int ordinal) => _result.FieldTypes[ordinal];
		public override string GetName        (int ordinal) => _result.FieldNames[ordinal];
		public override int    GetOrdinal     (string name) => _ordinal[name];

		public override void Close()
		{
		}

		public override bool Read()
		{
			if (++_current < _result.RowCount)
			{
				_data = _result.Data[_current];

				return true;
			}

			_data = null;

			return false;
		}

		public override long        GetBytes      (int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotImplementedException();
		public override long        GetChars      (int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => throw new NotImplementedException();
		public override int         GetValues     (object[] values) => throw new NotImplementedException();
		public override IEnumerator GetEnumerator () => throw new NotImplementedException();
		public override DataTable   GetSchemaTable() => throw new NotImplementedException();
		public override bool        NextResult    () => throw new NotImplementedException();

		public override bool IsClosed       => throw new NotImplementedException();
		public override int RecordsAffected => throw new NotImplementedException();
		#endregion
	}
}
