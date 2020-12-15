using System;
using System.Collections;
using System.Data;
using System.Data.Common;

namespace LinqToDB.Benchmarks.TestProvider
{
	public class MockDbDataReader : DbDataReader
	{
		private readonly QueryResult _result;

		private int _row = -1;

		public MockDbDataReader(QueryResult result)
		{
			_result = result;
		}

		public override DataTable GetSchemaTable()
		{
			return _result.Schema!;
		}

		public override object this[int ordinal] => throw new NotImplementedException();

		public override object this[string name] => throw new NotImplementedException();

		public override int Depth => throw new NotImplementedException();

		public override int FieldCount => _result.Names!.Length;

		public override bool HasRows => throw new NotImplementedException();

		public override bool IsClosed => throw new NotImplementedException();

		public override int RecordsAffected => throw new NotImplementedException();

		public override bool GetBoolean(int ordinal)
		{
			return (bool)_result.Data![_row][ordinal]!;
		}

		public override byte GetByte(int ordinal)
		{
			return (byte)_result.Data![_row][ordinal]!;
		}

		public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
		{
			throw new NotImplementedException();
		}

		public override char GetChar(int ordinal)
		{
			throw new NotImplementedException();
		}

		public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
		{
			throw new NotImplementedException();
		}

		public override string GetDataTypeName(int ordinal)
		{
			return _result.DbTypes![ordinal];
		}

		public override DateTime GetDateTime(int ordinal)
		{
			return (DateTime)_result.Data![_row][ordinal]!;
		}

		public override decimal GetDecimal(int ordinal)
		{
			return (decimal)_result.Data![_row][ordinal]!;
		}

		public override double GetDouble(int ordinal)
		{
			throw new NotImplementedException();
		}

		public override IEnumerator GetEnumerator()
		{
			throw new NotImplementedException();
		}

		public override Type GetFieldType(int ordinal)
		{
			return _result.FieldTypes![ordinal];
		}

		public override float GetFloat(int ordinal)
		{
			throw new NotImplementedException();
		}

		public override Guid GetGuid(int ordinal)
		{
			return (Guid)_result.Data![_row][ordinal]!;
		}

		public override short GetInt16(int ordinal)
		{
			return (short)_result.Data![_row][ordinal]!;
		}

		public override int GetInt32(int ordinal)
		{
			return (int)_result.Data![_row][ordinal]!;
		}

		public override long GetInt64(int ordinal)
		{
			return (long)_result.Data![_row][ordinal]!;
		}

		public override string GetName(int ordinal)
		{
			return _result.Names![ordinal];
		}

		public override int GetOrdinal(string name)
		{
			throw new NotImplementedException();
		}

		public override string GetString(int ordinal)
		{
			return (string)_result.Data![_row][ordinal]!;
		}

		public override object GetValue(int ordinal)
		{
			throw new NotImplementedException();
		}

		public override int GetValues(object?[] values)
		{
			for (var i = 0; i < FieldCount; i++)
			{
				values[i] = _result.Data![_row];
			}

			return FieldCount;
		}

		public override bool IsDBNull(int ordinal)
		{
			return _result.Data![_row][ordinal] == null;
		}

		public override bool NextResult()
		{
			throw new NotImplementedException();
		}

		public override bool Read()
		{
			_row++;
			return _row < _result.Data!.Length;
		}
	}
}
