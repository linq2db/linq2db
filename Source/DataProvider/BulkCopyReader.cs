using System;
using System.Collections;
using System.Data;
using System.Linq;

namespace LinqToDB.DataProvider
{
	using Mapping;

	class BulkCopyReader : IDataReader
	{
		public  readonly ColumnDescriptor[] Columns;
		private readonly IEnumerable        _collection;
		private readonly IEnumerator        _enumerator;

		public int Count;

		public BulkCopyReader(EntityDescriptor entityDescriptor, IEnumerable collection)
		{
			Columns     = entityDescriptor.Columns.Where(c => !c.SkipOnInsert).ToArray();
			_collection = collection;
			_enumerator = _collection.GetEnumerator();
		}

		#region Implementation of IDisposable

		public void Dispose()
		{
		}

		#endregion

		#region Implementation of IDataRecord

		public string GetName(int i)
		{
			return Columns[i].ColumnName;
		}

		public Type GetFieldType(int i)
		{
			return Columns[i].MemberType;
		}

		public object GetValue(int i)
		{
			return Columns[i].GetValue(_enumerator.Current);
		}

		public int FieldCount
		{
			get { return Columns.Length; }
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
		public bool        IsDBNull       (int i)           { throw new NotImplementedException(); }

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
			get { throw new NotImplementedException(); }
		}

		public int RecordsAffected
		{
			get { throw new NotImplementedException(); }
		}

		#endregion
	}
}
