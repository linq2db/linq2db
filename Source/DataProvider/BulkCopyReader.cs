using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace LinqToDB.DataProvider
{
	using Mapping;

	class BulkCopyReader : IDataReader
	{
		public BulkCopyReader(IDataProvider dataProvider, List<ColumnDescriptor> columns, IEnumerable collection)
		{
			_dataProvider = dataProvider;
			_columns      = columns;
			_enumerator   = collection.GetEnumerator();
			_columnTypes  = _columns
				.Select(c => c.DataType == DataType.Undefined ? dataProvider.MappingSchema.GetDataType(c.MemberType) : c.DataType)
				.ToArray();
		}

		public int Count;

		readonly DataType[]             _columnTypes;
		readonly IDataProvider          _dataProvider;
		readonly List<ColumnDescriptor> _columns;
		readonly IEnumerator            _enumerator;
		readonly Parameter              _valueConverter = new Parameter();

		internal class Parameter : IDbDataParameter
		{
			public DbType             DbType        { get; set; }
			public ParameterDirection Direction     { get; set; }
			public bool               IsNullable    { get { return Value == null || Value is DBNull; } }
			public string             ParameterName { get; set; }
			public string             SourceColumn  { get; set; }
			public DataRowVersion     SourceVersion { get; set; }
			public object             Value         { get; set; }
			public byte               Precision     { get; set; }
			public byte               Scale         { get; set; }
			public int                Size          { get; set; }
		}

		#region Implementation of IDataRecord

		public string GetName(int i)
		{
			return _columns[i].ColumnName;
		}

		public Type GetFieldType(int i)
		{
			return _dataProvider.ConvertParameterType(_columns[i].MemberType, _columnTypes[i]);
		}

		public object GetValue(int i)
		{
			var value = _columns[i].GetValue(_enumerator.Current);

			_dataProvider.SetParameter(_valueConverter, string.Empty, _columnTypes[i], value);

			return _valueConverter.Value;
		}

		public int FieldCount
		{
			get { return _columns.Count; }
		}

		public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
		{
			throw new NotImplementedException();
		}

		public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
		{
			throw new NotImplementedException();
		}

		public string      GetDataTypeName(int i)           { throw new NotImplementedException(); }
		public int         GetValues      (object[] values) { throw new NotImplementedException(); }
		public int         GetOrdinal     (string name)     { throw new NotImplementedException(); }
		public bool        GetBoolean     (int i)           { throw new NotImplementedException(); }
		public byte        GetByte        (int i)           { throw new NotImplementedException(); }
		public char        GetChar        (int i)           { throw new NotImplementedException(); }
		public Guid        GetGuid        (int i)           { throw new NotImplementedException(); }
		public short       GetInt16       (int i)           { throw new NotImplementedException(); }
		public int         GetInt32       (int i)           { throw new NotImplementedException(); }
		public long        GetInt64       (int i)           { throw new NotImplementedException(); }
		public float       GetFloat       (int i)           { throw new NotImplementedException(); }
		public double      GetDouble      (int i)           { throw new NotImplementedException(); }
		public string      GetString      (int i)           { throw new NotImplementedException(); }
		public decimal     GetDecimal     (int i)           { throw new NotImplementedException(); }
		public DateTime    GetDateTime    (int i)           { throw new NotImplementedException(); }
		public IDataReader GetData        (int i)           { throw new NotImplementedException(); }
		public bool        IsDBNull       (int i)           { return GetValue(i) == null;          }

		object IDataRecord.this[int i]
		{
			get { throw new NotImplementedException(); }
		}

		object IDataRecord.this[string name]
		{
			get { throw new NotImplementedException(); }
		}

		#endregion

		#region Implementation of IDataReader

		public void Close()
		{
			throw new NotImplementedException();
		}

		public DataTable GetSchemaTable()
		{
			throw new NotImplementedException();
		}

		public bool NextResult()
		{
			throw new NotImplementedException();
		}

		public bool Read()
		{
			var b = _enumerator.MoveNext();

			if (b)
				Count++;

			return b;
		}

		public int Depth
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsClosed
		{
			get { return false; }
		}

		public int RecordsAffected
		{
			get { throw new NotImplementedException(); }
		}

		#endregion

		#region Implementation of IDisposable

		public void Dispose()
		{
		}

		#endregion
	}
}
