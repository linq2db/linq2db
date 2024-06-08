using System;
using System.Collections.Generic;
using System.Text;
using LinqToDB.Common;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Implements text (code) generation using table layout.
	/// </summary>
	internal sealed partial class TableLayoutBuilder
	{
		/// <summary>
		/// Root column group definition (contains full table layout definition).
		/// </summary>
		private ColumnGroup?           _root;
		/// <summary>
		/// Root table data object.
		/// </summary>
		private ColumnGroupData?       _data;
		/// <summary>
		/// Cached generation results.
		/// </summary>
		private IReadOnlyList<string>? _generatedRows;
		/// <summary>
		/// Number of data rows in table.
		/// </summary>
		private int                    _rowsCount;

		/// <summary>
		/// Returns generated text for current table as separate string for each row.
		/// </summary>
		/// <returns>Generated text array.</returns>
		public IReadOnlyList<string> GetRows()
		{
			if (_generatedRows != null)
				return _generatedRows;

			// freeze table to disallow adding of more data after final text generated
			var header = AssertConfigured();
			header.Freeze();

			if (_rowsCount == 0)
				return _generatedRows = [];

			var result = new string[_rowsCount];

			// max width of each row (could be less for some rows) to use as preallocated string size
			var maxRowLength = _data == null ? 0 : header.GetMaxLength(_data);

			if (maxRowLength == 0)
			{
				for (var i = 0; i < _rowsCount; i++)
					result[i] = string.Empty;

				return _generatedRows = result;
			}

			// create row builders
			var rowBuilders = new StringBuilder[_rowsCount];
			for (var i = 0; i < _rowsCount; i++)
				rowBuilders[i] = new StringBuilder(maxRowLength);

			// pre-populate empty row flags for each row
			var emptyRows = new bool[_rowsCount];
			for (var i = 0; i < _rowsCount; i++)
				emptyRows[i] = _data!.IsEmpty(i);

			// generate rows
			header.WriteColumn(rowBuilders, emptyRows, _data!, null, null, new int[_rowsCount]);

			for (var i = 0; i < _rowsCount; i++)
				result[i] = rowBuilders[i].ToString();

			return _generatedRows = result;
		}

		/// <summary>
		/// Adds new row to table.
		/// </summary>
		/// <returns>Row data builder instance.</returns>
		public IGroupColumnsDataBuilder DataRow()
		{
			var header = AssertConfigured();

			_rowsCount++;

			_data ??= (ColumnGroupData)header.CreateDataStorage();

			_data!.SetCurrentRowIndex(_rowsCount - 1);

			// create top-level column group (always single group)
			return ((IGroupDataBuilder)_data).NewGroup();
		}

		/// <summary>
		/// Provides access to table layout definition fluent API.
		/// </summary>
		/// <returns>Table layout definition builder.</returns>
		public IHeaderConfigurator Layout()
		{
			if (_root != null)
				throw new InvalidOperationException("Table header already defined");

			_root = new ColumnGroup(null, null, null, null);

			return new HeaderConfigurator(_root);
		}

		/// <summary>
		/// Gets root column group definition.
		/// </summary>
		/// <returns>Column group definition.</returns>
		private ColumnGroup AssertConfigured()
		{
			if (_root == null)
				throw new InvalidOperationException("Table header not defined");

			if (!_root.Configured)
				throw new InvalidOperationException($"Table header definition not completed (with {nameof(IHeaderConfigurator)}.{nameof(IHeaderConfigurator.End)}() call)");

			return _root;
		}
	}
}
