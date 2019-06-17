using System;
using System.Collections.Generic;
using System.Data;

namespace LinqToDB.ServiceModel
{
	using Mapping;

	class ServiceModelDataReader : IDataReader
	{
		public ServiceModelDataReader(MappingSchema mappingSchema, LinqServiceResult result)
		{
			_mappingSchema = mappingSchema;
			_result = result;

			for (var i = 0; i < result.FieldNames.Length; i++)
				_ordinal.Add(result.FieldNames[i], i);
		}

		readonly MappingSchema          _mappingSchema;
		readonly LinqServiceResult      _result;
		readonly Dictionary<string,int> _ordinal = new Dictionary<string,int>();

		string[] _data;
		int      _current = -1;

		#region IDataRecord

		object IDataRecord.this[int i]       => GetValue(i);

		object IDataRecord.this[string name] => GetValue(GetOrdinal(name));

		int IDataRecord.FieldCount           => _result.FieldCount;

		long IDataRecord.GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
		{
			throw new NotImplementedException();
		}

		long IDataRecord.GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
		{
			throw new NotImplementedException();
		}

		IDataReader IDataRecord.GetData(int i) => throw new NotImplementedException();

		string IDataRecord.GetDataTypeName(int i) => GetFieldType(i).FullName;

		public Type GetFieldType(int i) => _result.FieldTypes[i];

		bool     IDataRecord.GetBoolean (int i) => (bool)    GetValue(i);
		byte     IDataRecord.GetByte    (int i) => (byte)    GetValue(i);
		char     IDataRecord.GetChar    (int i) => (char)    GetValue(i);
		DateTime IDataRecord.GetDateTime(int i) => (DateTime)GetValue(i);
		decimal  IDataRecord.GetDecimal (int i) => (decimal) GetValue(i);
		double   IDataRecord.GetDouble  (int i) => (double)  GetValue(i);
		float    IDataRecord.GetFloat   (int i) => (float)   GetValue(i);
		Guid     IDataRecord.GetGuid    (int i) => (Guid)    GetValue(i);
		short    IDataRecord.GetInt16   (int i) => (short)   GetValue(i);
		int      IDataRecord.GetInt32   (int i) => (int)     GetValue(i);
		long     IDataRecord.GetInt64   (int i) => (long)    GetValue(i);

		string IDataRecord.GetName(int i) => _result.FieldNames[i];

		public int GetOrdinal(string name) => _ordinal[name];

		string IDataRecord.GetString(int i) => (string)GetValue(i);

		public object GetValue(int i)
		{
			var type = _result.FieldTypes[i];
			var value = _data[i];

			return SerializationConverter.Deserialize(_mappingSchema, type, value);
		}

		int IDataRecord.GetValues(object[] values) => throw new NotImplementedException();

		bool IDataRecord.IsDBNull(int i) => _data[i] == null;

		#endregion

		#region IDataReader
		bool IDataReader.Read()
		{
			if (++_current < _result.RowCount)
			{
				_data = _result.Data[_current];

				return true;
			}

			_data = null;

			return false;
		}

		int IDataReader.Depth => 0;

		void IDataReader.Close()
		{
		}

		bool IDataReader.IsClosed              => throw new NotImplementedException();
		int IDataReader.RecordsAffected        => throw new NotImplementedException();
		DataTable IDataReader.GetSchemaTable() => throw new NotImplementedException();
		bool IDataReader.NextResult()          => throw new NotImplementedException();

		#endregion

		#region IDisposable

		void IDisposable.Dispose()
		{
		}

		#endregion



		
	}
}
