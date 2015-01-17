using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
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
				.Select(c => c.DataType == DataType.Undefined ? dataProvider.MappingSchema.GetDataType(c.MemberType).DataType : c.DataType)
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

		public int GetValues(object[] values)
		{
			var count = _columns.Count;
			var obj   = _enumerator.Current;

			for (var it = 0; it < count; ++it)
			{
				var value = _columns[it].GetValue(obj);
				_dataProvider.SetParameter(_valueConverter, string.Empty, _columnTypes[it], value);
				values[it] = _valueConverter.Value;
			}

			return count;
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

		public string      GetDataTypeName(int i)       { throw new NotImplementedException(); }
		public int         GetOrdinal     (string name) { throw new NotImplementedException(); }
		public bool        GetBoolean     (int i)       { throw new NotImplementedException(); }
		public byte        GetByte        (int i)       { throw new NotImplementedException(); }
		public char        GetChar        (int i)       { throw new NotImplementedException(); }
		public Guid        GetGuid        (int i)       { throw new NotImplementedException(); }
		public short       GetInt16       (int i)       { throw new NotImplementedException(); }
		public int         GetInt32       (int i)       { throw new NotImplementedException(); }
		public long        GetInt64       (int i)       { throw new NotImplementedException(); }
		public float       GetFloat       (int i)       { throw new NotImplementedException(); }
		public double      GetDouble      (int i)       { throw new NotImplementedException(); }
		public string      GetString      (int i)       { throw new NotImplementedException(); }
		public decimal     GetDecimal     (int i)       { throw new NotImplementedException(); }
		public DateTime    GetDateTime    (int i)       { throw new NotImplementedException(); }
		public IDataReader GetData        (int i)       { throw new NotImplementedException(); }
		public bool        IsDBNull       (int i)       { return GetValue(i) == null;          }

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
			//do nothing
		}

		public DataTable GetSchemaTable()
		{
			var table = new DataTable("SchemaTable")
			{
				Locale = CultureInfo.InvariantCulture
			};

			table.Columns.Add(new DataColumn(SchemaTableColumn.ColumnName,                       typeof(string)));
			table.Columns.Add(new DataColumn(SchemaTableColumn.ColumnOrdinal,                    typeof(int)));
			table.Columns.Add(new DataColumn(SchemaTableColumn.ColumnSize,                       typeof(int)));
			table.Columns.Add(new DataColumn(SchemaTableColumn.NumericPrecision,                 typeof(short)));
			table.Columns.Add(new DataColumn(SchemaTableColumn.NumericScale,                     typeof(short)));
			table.Columns.Add(new DataColumn(SchemaTableColumn.DataType,                         typeof(Type)));
			table.Columns.Add(new DataColumn(SchemaTableOptionalColumn.ProviderSpecificDataType, typeof(Type)));
			table.Columns.Add(new DataColumn(SchemaTableColumn.NonVersionedProviderType,         typeof(int)));
			table.Columns.Add(new DataColumn(SchemaTableColumn.ProviderType,                     typeof(int)));
			table.Columns.Add(new DataColumn(SchemaTableColumn.IsLong,                           typeof(bool)));
			table.Columns.Add(new DataColumn(SchemaTableColumn.AllowDBNull,                      typeof(bool)));
			table.Columns.Add(new DataColumn(SchemaTableOptionalColumn.IsReadOnly,               typeof(bool)));
			table.Columns.Add(new DataColumn(SchemaTableOptionalColumn.IsRowVersion,             typeof(bool)));
			table.Columns.Add(new DataColumn(SchemaTableColumn.IsUnique,                         typeof(bool)));
			table.Columns.Add(new DataColumn(SchemaTableColumn.IsKey,                            typeof(bool)));
			table.Columns.Add(new DataColumn(SchemaTableOptionalColumn.IsAutoIncrement,          typeof(bool)));
			table.Columns.Add(new DataColumn(SchemaTableOptionalColumn.IsHidden,                 typeof(bool)));
			table.Columns.Add(new DataColumn(SchemaTableOptionalColumn.BaseCatalogName,          typeof(string)));
			table.Columns.Add(new DataColumn(SchemaTableColumn.BaseSchemaName,                   typeof(string)));
			table.Columns.Add(new DataColumn(SchemaTableColumn.BaseTableName,                    typeof(string)));
			table.Columns.Add(new DataColumn(SchemaTableColumn.BaseColumnName,                   typeof(string)));
			table.Columns.Add(new DataColumn(SchemaTableOptionalColumn.BaseServerName,           typeof(string)));
			table.Columns.Add(new DataColumn(SchemaTableColumn.IsAliased,                        typeof(bool)));
			table.Columns.Add(new DataColumn(SchemaTableColumn.IsExpression,                     typeof(bool)));

			for (var i = 0; i < _columns.Count; ++i)
			{
				var columnDescriptor = _columns[i];
				var row = table.NewRow();

				row[SchemaTableColumn.ColumnName]              = columnDescriptor.ColumnName;
				row[SchemaTableColumn.DataType]                = _dataProvider.ConvertParameterType(columnDescriptor.MemberType, _columnTypes[i]);
				row[SchemaTableColumn.IsKey]                   = columnDescriptor.IsPrimaryKey;
				row[SchemaTableOptionalColumn.IsAutoIncrement] = columnDescriptor.IsIdentity;
				row[SchemaTableColumn.AllowDBNull]             = columnDescriptor.CanBeNull;
				row[SchemaTableColumn.ColumnSize]              =
					columnDescriptor.Length.HasValue ?
						columnDescriptor.Length == 0 ? 0x7FFFFFFF : columnDescriptor.Length :
						null;
				row[SchemaTableColumn.NumericPrecision]        = columnDescriptor.Precision.HasValue ? (short?)columnDescriptor.Precision.Value : null;
				row[SchemaTableColumn.NumericScale]            = columnDescriptor.Scale.    HasValue ? (short?)columnDescriptor.Scale.    Value : null;

				table.Rows.Add(row);
			}

			return table;
		}

		public bool NextResult()
		{
			return false;
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
