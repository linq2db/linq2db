using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Async;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;

namespace LinqToDB.DataProvider
{
	public class BulkCopyReader<T> : BulkCopyReader, IAsyncDisposable
	{
		readonly IEnumerator<T>?      _enumerator;
		readonly IAsyncEnumerator<T>? _asyncEnumerator;

		public BulkCopyReader(DataConnection dataConnection, List<ColumnDescriptor> columns, IEnumerable<T> collection)
			: base(dataConnection, columns)
		{
			_enumerator = collection.GetEnumerator();
		}

		public BulkCopyReader(DataConnection dataConnection, List<ColumnDescriptor> columns, IAsyncEnumerable<T> collection, CancellationToken cancellationToken)
			: base(dataConnection, columns)
		{
			_asyncEnumerator = collection.GetAsyncEnumerator(cancellationToken);
		}

		protected override bool MoveNext()
		{
			if (_enumerator != null)
				return _enumerator.MoveNext();
			
			return SafeAwaiter.Run(() => _asyncEnumerator!.MoveNextAsync());
		}

		protected override object Current
			=> (_enumerator != null ? _enumerator.Current : _asyncEnumerator!.Current)!;

		protected override ValueTask<bool> MoveNextAsync()
			=> _enumerator != null ? new ValueTask<bool>(_enumerator.MoveNext()) : _asyncEnumerator!.MoveNextAsync();

		#region Implementation of IDisposable

#pragma warning disable CA2215 // CA2215: Dispose methods should call base class dispose
		protected override void Dispose(bool disposing)
#pragma warning restore CA2215 // CA2215: Dispose methods should call base class dispose
		{
			if (disposing)
			{
				_enumerator?.Dispose();
				if (_asyncEnumerator != null)
				{
					SafeAwaiter.Run(_asyncEnumerator.DisposeAsync);
				}
			}
		}

#if NET8_0_OR_GREATER
#pragma warning disable CA2215 // CA2215: Dispose methods should call base class dispose
		public override ValueTask DisposeAsync()
#pragma warning restore CA2215 // CA2215: Dispose methods should call base class dispose
#else
		public ValueTask DisposeAsync()
#endif
		{
			_enumerator?.Dispose();
			return _asyncEnumerator?.DisposeAsync() ?? default;
		}

#endregion

	}

	public abstract class BulkCopyReader : DbDataReader
	{
		public int Count;

		readonly DataConnection                   _dataConnection;
		readonly DbDataType[]                     _columnTypes;
		readonly List<ColumnDescriptor>           _columns;
		readonly Parameter                        _valueConverter = new ();
		readonly IReadOnlyDictionary<string, int> _ordinals;

		protected abstract bool MoveNext();
		protected abstract ValueTask<bool> MoveNextAsync();

		protected abstract object Current { get; }

		protected BulkCopyReader(DataConnection dataConnection, List<ColumnDescriptor> columns)
		{
			_dataConnection = dataConnection;
			_columns        = columns;
			_columnTypes    = _columns.Select(c => c.GetConvertedDbDataType()).ToArray();
			_ordinals       = _columns.Select((c, i) => new { c, i }).ToDictionary(_ => _.c.ColumnName, _ => _.i);
		}

		public class Parameter : DbParameter
		{
			public override DbType             DbType                  { get; set; }
			public override ParameterDirection Direction               { get; set; }
			public override bool               IsNullable              { get => Value == null || Value is DBNull; set { } }
			[AllowNull]
			public override string             ParameterName           { get; set; }
			public override int                Size                    { get; set; }
			[AllowNull]
			public override string             SourceColumn            { get; set; }
			public override DataRowVersion     SourceVersion           { get; set; }
			public override bool               SourceColumnNullMapping { get; set; }
			public override object?            Value                   { get; set; }

			public override void ResetDbType() { }
		}

#region Implementation of IDataRecord

		public override string GetName(int ordinal)
		{
			return _columns[ordinal].ColumnName;
		}

		public override Type GetFieldType(int ordinal)
		{
			return _dataConnection.DataProvider.ConvertParameterType(_columns[ordinal].MemberType, _columnTypes[ordinal]);
		}

		public override object GetValue(int ordinal) => GetValueInternal(ordinal) ?? throw new InvalidOperationException("Value is NULL");

		private object? GetValueInternal(int ordinal)
		{
			var value = _columns[ordinal].GetProviderValue(Current);

			_dataConnection.DataProvider.SetParameter(_dataConnection, _valueConverter, string.Empty, _columnTypes[ordinal], value);

			return _valueConverter.Value;
		}

		public override int GetValues(object?[] values)
		{
			var count = _columns.Count;
			var obj   = Current;

			for (var it = 0; it < count; ++it)
			{
				var value = _columns[it].GetProviderValue(obj);
				_dataConnection.DataProvider.SetParameter(_dataConnection, _valueConverter, string.Empty, _columnTypes[it], value);
				values[it] = _valueConverter.Value;
			}

			return count;
		}

		public override int FieldCount => _columns.Count;

		public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
		{
			throw new NotImplementedException();
		}

		public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
		{
			throw new NotImplementedException();
		}

		public override string      GetDataTypeName(int ordinal) => throw new NotImplementedException();
		public override int         GetOrdinal     (string name) => _ordinals[name];
		public override bool        GetBoolean     (int ordinal) => throw new NotImplementedException();
		public override byte        GetByte        (int ordinal) => throw new NotImplementedException();
		public override char        GetChar        (int ordinal) => throw new NotImplementedException();
		public override Guid        GetGuid        (int ordinal) => throw new NotImplementedException();
		public override short       GetInt16       (int ordinal) => throw new NotImplementedException();
		public override int         GetInt32       (int ordinal) => throw new NotImplementedException();
		public override long        GetInt64       (int ordinal) => throw new NotImplementedException();
		public override float       GetFloat       (int ordinal) => throw new NotImplementedException();
		public override double      GetDouble      (int ordinal) => throw new NotImplementedException();
		public override string      GetString      (int ordinal) => throw new NotImplementedException();
		public override decimal     GetDecimal     (int ordinal) => throw new NotImplementedException();
		public override DateTime    GetDateTime    (int ordinal) => throw new NotImplementedException();
		public override bool        IsDBNull       (int ordinal) => GetValueInternal(ordinal) == null;

		public override object this[int i]       => throw new NotImplementedException();
		public override object this[string name] => throw new NotImplementedException();

#endregion

#region Implementation of IDataReader

		public override void Close()
		{
			//do nothing
		}

		public override DataTable GetSchemaTable()
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
				var convertedType    = columnDescriptor.GetConvertedDbDataType();
				var row              = table.NewRow();

				row[SchemaTableColumn.ColumnName]              = columnDescriptor.ColumnName;
				row[SchemaTableColumn.DataType]                = _dataConnection.DataProvider.ConvertParameterType(convertedType.SystemType, _columnTypes[i]);
				row[SchemaTableColumn.IsKey]                   = columnDescriptor.IsPrimaryKey;
				row[SchemaTableOptionalColumn.IsAutoIncrement] = columnDescriptor.IsIdentity;
				row[SchemaTableColumn.AllowDBNull]             = columnDescriptor.CanBeNull;
				//length cannot be null(DBNull) or 0
				row[SchemaTableColumn.ColumnSize]              =
					columnDescriptor.Length.HasValue && columnDescriptor.Length > 0 ?
						columnDescriptor.Length.Value : 0x7FFFFFFF;

				if (columnDescriptor.Precision != null)
					row[SchemaTableColumn.NumericPrecision] = (short)columnDescriptor.Precision.Value;

				if (columnDescriptor.Scale != null)
					row[SchemaTableColumn.NumericScale]     = (short)columnDescriptor.Scale.Value;

				table.Rows.Add(row);
			}

			return table;
		}

		public override bool NextResult()   => false;

		public override bool Read()
		{
			var b = MoveNext();

			if (b)
				Count++;

			return b;
		}

		public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
		{
			var b = await MoveNextAsync().ConfigureAwait(false);

			if (b)
				Count++;

			return b;
		}

		public override int Depth           => throw new NotImplementedException();

		public override bool IsClosed       => false;

		public override int RecordsAffected => throw new NotImplementedException();

#endregion

		public override IEnumerator GetEnumerator() => throw new NotImplementedException();

		public override bool HasRows => throw new NotImplementedException();
	}
}
