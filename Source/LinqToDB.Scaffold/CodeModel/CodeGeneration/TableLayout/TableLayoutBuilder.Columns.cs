using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.CodeModel
{
	// column descriptors and column data storages are separate objects as single column descriptor could correspond to
	// multiple column instances in table, when parent groups contain multiple sub-groups.
	// In other words, one column descriptor could be used for multiple columns in resulting table.
	// Example:
	// 1. attribute named parameter is a group of 3 columns: name, assignment operator fixed column and value.
	// 2. we have 2 simple column descriptors: name column descriptor and value column descriptor
	// 3. BUT: in generated table we could have X instances of each column, where X depends on number of named parameters
	//    per attribute and number of attributes in group:
	//
	// [Attr1(named1 = value1, named2 = value2), Attr2(named3 = value3)]
	//
	// Here we see two attribute sub-groups, where one has two named parameter sub-groups and another has one named parameter
	// subgroup. 3 named parameter subgroups in total.
	partial class TableLayoutBuilder
	{
		#region ColumnBase
		/// <summary>
		/// Base column data storage class.
		/// </summary>
		private abstract class ColumnDataBase
		{
			/// <summary>
			/// Check wether column is empty or contains data for specific row.
			/// </summary>
			/// <param name="rowIndex">Table row to check for data.</param>
			/// <returns><c>true</c> if column contains data at specified row, <c>false</c> otherwise.</returns>
			public abstract bool IsEmpty(int rowIndex);
		}

		/// <summary>
		/// Base class for all columns.
		/// </summary>
		/// <typeparam name="TData">Type of data storage for current column type.</typeparam>
		private abstract class ColumnBase<TData> : ColumnBase
			where TData : ColumnDataBase
		{
			protected ColumnBase(string? name)
				: base(name)
			{
			}

			/// <summary>
			/// Returns max length for column between all rows. Zero value means column is empty.
			/// </summary>
			/// <param name="data">Column data.</param>
			/// <returns>Max length of column between all rows.</returns>
			public abstract int GetMaxLength(TData data);

			/// <summary>
			/// Writes column data for each row.
			/// </summary>
			/// <param name="rows">Data row writers for each table row.</param>
			/// <param name="skipRow">Per-row flag to instruct column to not generate any data when row-specific flag is <c>true</c>.</param>
			/// <param name="data">Column data.</param>
			/// <param name="isEmptyGroup">Optional per-row empty flags for all columns within current group. Used by fixed columns to detect wether they should generate value or not.</param>
			/// <param name="groupIndex">Optional index of current column within group. Used with <paramref name="isEmptyGroup"/> parameter.</param>
			/// <param name="prePadding">Per-row padding size, that should be generated before current column if it is not empty.</param>
			/// <returns>Per-row paddings for next column.</returns>
			public abstract int[] WriteColumn(StringBuilder[] rows, bool[] skipRow, TData data, bool[][]? isEmptyGroup, int? groupIndex, int[] prePadding);
		}

		/// <summary>
		/// Base class for all columns with non-generic APIs.
		/// </summary>
		private abstract class ColumnBase
		{
			protected ColumnBase(string? name)
			{
				Name = name;
			}

			/// <summary>
			/// Column name (except fixed columns).
			/// </summary>
			public string? Name       { get; }

			/// <summary>
			/// Indicates that column is fully configured.
			/// </summary>
			internal bool  Configured { get; private set; }

			/// <summary>
			/// Indicates that column data already populated for all rows.
			/// </summary>
			protected bool Populated  { get; private set; }

			/// <summary>
			/// Mark column as configured.
			/// </summary>
			public virtual void FinalizeConfiguration()
			{
				AssertNotConfigured();
				Configured = true;
			}

			/// <summary>
			/// Mark column as having data populated for all rows.
			/// </summary>
			public virtual void Freeze()
			{
				AssertNotPopulated();
				Populated = true;
			}

			/// <summary>
			/// Verify that column could accept more data.
			/// </summary>
			protected void AssertNotPopulated()
			{
				AssertConfigured();
				if (Populated)
					throw new InvalidOperationException("Cannot add more data to already populated table");
			}

			/// <summary>
			/// Verify that column cannot accept more data
			/// </summary>
			protected void AssertPopulated()
			{
				AssertConfigured();
				if (!Populated)
					throw new InvalidOperationException("Table data population is not done yet");
			}

			/// <summary>
			/// Verify that column configuration is not completed yet.
			/// </summary>
			protected void AssertNotConfigured()
			{
				if (Configured)
					throw new InvalidOperationException("Table configuration already completed");
			}

			/// <summary>
			/// Verify that column configuration is already completed.
			/// </summary>
			protected void AssertConfigured()
			{
				if (!Configured)
					throw new InvalidOperationException("Table configuration is not done yet");
			}

			/// <summary>
			/// Creates column data storage instance.
			/// As callers don't need to know exact type for return value, method declared as non-generic to
			/// simplify caller implementation.
			/// </summary>
			/// <returns>Column data storage instance.</returns>
			public abstract ColumnDataBase CreateDataStorage();
		}
		#endregion

		#region FixedColumn
		/// <summary>
		/// Data storage for fixed column. As user cannot specify non-contant data for fixed columns, it exists as
		/// single instance.
		/// </summary>
		private sealed class FixedColumnData : ColumnDataBase
		{
			/// <summary>
			/// Data storage instance.
			/// </summary>
			public static readonly ColumnDataBase Instance = new FixedColumnData();

			// Always report column as empty, as it doesn't allow value specification.
			public override bool IsEmpty(int rowIndex) => true;

			private FixedColumnData() { }
		}

		/// <summary>
		/// Fixed column descriptor.
		/// </summary>
		private sealed class FixedColumn : ColumnBase<FixedColumnData>
		{
			// column value
			private readonly string _value;
			// optional lookup range before current column within column group to identify wether we should generate value for current column
			private readonly int    _requireNonEmptyBefore;
			// optional lookup range after current column within column group to identify wether we should generate value for current column
			private readonly int    _requireNonEmptyAfter;

			public FixedColumn(string value, int requireNonEmptyBefore, int requireNonEmptyAfter)
				: base(null)
			{
				_value                 = value;
				_requireNonEmptyBefore = requireNonEmptyBefore;
				_requireNonEmptyAfter  = requireNonEmptyAfter;
			}

			/// <summary>
			/// Indicates that current column rendering condition depends on other columns within column group.
			/// </summary>
			/// <returns><c>true</c>, if column should be rendered conditionally.</returns>
			public bool IsDependent() => _requireNonEmptyAfter > 0 || _requireNonEmptyBefore > 0;

			// return static instance
			public override ColumnDataBase CreateDataStorage() => FixedColumnData.Instance;

			public override int GetMaxLength(FixedColumnData data)
			{
				AssertPopulated();
				return _value.Length;
			}

			public override int[] WriteColumn(
				StringBuilder[] rows,
				bool[]          skipRow,
				FixedColumnData data,
				bool[][]?       isEmptyGroup,
				int?            groupIndex,
				int[]           prePadding)
			{
				AssertPopulated();

				bool[]? skip = null;

				if (IsDependent())
				{
					skip = new bool[rows.Length];

					// calculate per-row skip generation flags for dependend column
					for (var i = 0; i < rows.Length; i++)
					{
						// by default skip dependent column
						skip[i] = true;

						// check dependencies
						var currentGroupColumnsEmptyFlags = isEmptyGroup![i];

						// if defined, check prevous columns requirement
						if (_requireNonEmptyBefore > 0)
							for (var j = 1; j <= _requireNonEmptyBefore; j++)
								if (!currentGroupColumnsEmptyFlags[groupIndex!.Value - j])
								{
									// if non-empty column found, stop enumeration
									skip[i] = false;
									break;
								}

						// check next columns requirement
						if (_requireNonEmptyAfter > 0 &&
							// if both pre- and post- requirements set, skip those that already failed pre- requirement
							(_requireNonEmptyBefore == 0 || !skip[i]))
						{
							// if both pre- and post- requirements set, reset skip flag
							if (_requireNonEmptyBefore > 0)
								skip[i] = true;

							for (var j = 1; j <= _requireNonEmptyAfter; j++)
								if (!currentGroupColumnsEmptyFlags[groupIndex!.Value + j])
								{
									skip[i] = false;
									break;
								}
						}
					}
				}

				if (skip == null || !skip.All(skip => skip))
				{
					// generate column value per-row
					for (var i = 0; i < rows.Length; i++)
					{
						// column is not skipped
						if (skipRow[i])
							// for skipped row generation - add current column width to padding
							prePadding[i] = prePadding[i] + _value.Length;
						else
						{
							// otherwise generate and reset padding
							if (prePadding[i] > 0)
							{
								rows[i].Append(' ', prePadding[i]);
								prePadding[i] = 0;
							}

							// and then generate column value
							if (skip == null || !skip[i])
								rows[i].Append(_value);
							else
								prePadding[i] = _value.Length;
						}
					}
				}

				return prePadding;
			}
		}
		#endregion

		#region SimpleColumn
		/// <summary>
		/// Data storage for simple column.
		/// </summary>
		private sealed class SimpleColumnData : ColumnDataBase
		{
			// per-row values
			// as we don't know row count, we use list
			// also number of items in list could be less that rows as we don't add values explicitly for empty row columns
			// for such rows we assume empty (null) value
			private List<string?>? _rowValues;

			/// <summary>
			/// Gets max column length.
			/// </summary>
			internal int MaxLength { get; private set; }

			/// <summary>
			/// Gets column value for specific row.
			/// </summary>
			/// <param name="rowIndex">Index of row, for which return column value.</param>
			/// <returns>Column value for specific row or <see langword="null"/> if column is empty at that row.</returns>
			internal string? GetValue(int rowIndex)
			{
				return rowIndex < (_rowValues?.Count ?? 0)
					? _rowValues![rowIndex] // row value known explicitly (also could be null)
					: null; // row value missing
			}

			/// <summary>
			/// Adds column value for specific row.
			/// </summary>
			/// <param name="value">Value to add.</param>
			/// <param name="rowIndex">Index of row.</param>
			public void AddValue(string value, int rowIndex)
			{
				// allocate values collection if it is not created yet
				_rowValues ??= new();

				// fill prevous rows with null, if they are not filled yet
				while (_rowValues.Count < rowIndex)
					_rowValues.Add(null);

				// for empty value we replace it with null, as we use null as empty column indicator
				_rowValues.Add(value.Length == 0 ? null : value);

				// update max column length if needed
				if (value.Length > MaxLength)
					MaxLength = value.Length;
			}

			public override bool IsEmpty(int rowIndex)
			{
				// column value is null for specific row or row value absent
				return (_rowValues?.Count ?? 0) <= rowIndex || _rowValues![rowIndex] == null;
			}
		}

		/// <summary>
		/// Simple column descriptor.
		/// </summary>
		private sealed class SimpleColumn : ColumnBase<SimpleColumnData>
		{
			public SimpleColumn(string columnName)
				: base(columnName)
			{
			}

			public override ColumnDataBase CreateDataStorage() => new SimpleColumnData();

			public override int GetMaxLength(SimpleColumnData data)
			{
				AssertPopulated();
				return data.MaxLength;
			}

			public override int[] WriteColumn(
				StringBuilder[]  rows,
				bool[]           skipRow,
				SimpleColumnData data,
				bool[][]?        isEmptyGroup,
				int?             groupIndex,
				int[]            prePadding)
			{
				AssertPopulated();

				// empty column (for all rows)
				if (data.MaxLength == 0)
					return prePadding;

				// generate column value per-row
				for (var i = 0; i < rows.Length; i++)
				{
					// column skipped for row - apped column size to padding
					if (skipRow[i])
						prePadding[i] = prePadding[i] + data.MaxLength;
					else
					{
						// generate padding (for non-empty column) and then column value
						var value = data.GetValue(i);
						if (value != null)
						{
							if (prePadding[i] > 0)
							{
								rows[i].Append(' ', prePadding[i]);
								prePadding[i] = 0;
							}

							rows[i].Append(value);
						}

						// calculate padding for next column
						prePadding[i] = prePadding[i] + data.MaxLength - (value?.Length ?? 0);
					}
				}

				return prePadding;
			}
		}
		#endregion

		#region ColumnGroup
		/// <summary>
		/// Column group data storage. Stores group data as a sequence of storages for sub-groups.
		/// </summary>
		private sealed class ColumnGroupData : ColumnDataBase, IGroupDataBuilder, IGroupColumnsDataBuilder
		{
			// group descriptor
			private readonly ColumnGroup                         _group;
			// sub-group storage provider
			private readonly Func<IReadOnlyList<ColumnDataBase>> _createGroupData;
			private readonly List<IReadOnlyList<ColumnDataBase>> _data = new();

			// currently populated row
			private int _currentRow;
			// current sub-group index
			private int _currentGroupIndex;

			public ColumnGroupData(ColumnGroup group, Func<IReadOnlyList<ColumnDataBase>> createGroupData)
			{
				_group           = group;
				_createGroupData = createGroupData;
			}

			/// <summary>
			/// Group data in sub-groups.
			/// </summary>
			public IReadOnlyList<IReadOnlyList<ColumnDataBase>> SubGroups => _data;

			/// <summary>
			/// Adds new sub-group storage.
			/// </summary>
			/// <returns>Added sub-group storage instance.</returns>
			private IReadOnlyList<ColumnDataBase> AddSubGroup()
			{
				var group = _createGroupData();
				_data.Add(group);
				return group;
			}

			public override bool IsEmpty(int rowIndex)
			{
				// group is empty if all columns in all subgroups are empty
				foreach (var group in SubGroups)
					foreach (var column in group)
						if (!column.IsEmpty(rowIndex))
							return false;

				return true;
			}

			/// <summary>
			/// Reset state for new row.
			/// </summary>
			/// <param name="rowIndex">New row index.</param>
			internal void SetCurrentRowIndex(int rowIndex)
			{
				_currentRow        = rowIndex;
				_currentGroupIndex = -1;
			}

			IGroupColumnsDataBuilder IGroupDataBuilder.NewGroup()
			{
				_currentGroupIndex++;

				// create subgroup if it is not created already by previous rows
				if (SubGroups.Count <= _currentGroupIndex)
					AddSubGroup();

				return this;
			}

			IGroupDataBuilder IGroupColumnsDataBuilder.Group(string name)
			{
				// find child group column and it's data storage object
				var (column, index) = _group.GetListColumn(name);
				var data            = (ColumnGroupData)SubGroups[_currentGroupIndex][index];

				// set row index for child column
				data.SetCurrentRowIndex(_currentRow);

				return data;
			}

			IGroupColumnsDataBuilder IGroupColumnsDataBuilder.ColumnValue(string name, string value)
			{
				// find simple child column and it's data storage object
				var (column, index) = _group.GetColumn(name);
				var data            = (SimpleColumnData)SubGroups[_currentGroupIndex][index];

				data.AddValue(value, _currentRow);

				return this;
			}
		}

		/// <summary>
		/// Column group descriptor. Column group is a repeatable set of columns.
		/// </summary>
		private sealed class ColumnGroup : ColumnBase<ColumnGroupData>
		{
			// optional column decorators
			private readonly string? _prefix;
			private readonly string? _separator;
			private readonly string? _suffix;

			// child columns descriptors
			private readonly List<ColumnBase> _columns = new ();

			// column lookup by column name
			private readonly Dictionary<string, (ColumnBase column, int index)> _columnsMap = new ();

			// indicates that column group contains at least one fixed child column with dependency on other
			// columns in group
			private bool    _hasDependentColumns;
			// max length for whole group (-1 used to indicate uninitialized length value)
			private int     _maxLength = -1;

			public ColumnGroup(string? name, string? prefix, string? separator, string? suffix)
				: base(name)
			{
				_prefix    = prefix;
				_separator = separator;
				_suffix    = suffix;
			}

			public override ColumnDataBase CreateDataStorage()
			{
				return new ColumnGroupData(this, () =>
				{
					// create storage objects for child columns
					var data = new ColumnDataBase[_columns.Count];

					for (var i = 0; i < data.Length; i++)
						data[i] = _columns[i].CreateDataStorage();

					return data;
				});
			}

			public override int[] WriteColumn(
				StringBuilder[] rows,
				bool[]          skipRow,
				ColumnGroupData data,
				bool[][]?       isEmptyGroup,
				int?            groupIndex,
				int[]           prePadding)
			{
				AssertPopulated();

				// group is empty (no sub-group instances defined)
				if ((data.SubGroups?.Count ?? 0) == 0)
					return prePadding;

				// if prefix specified, render prefix or if rendering should be skipped, append prefix length to padding
				if (_prefix != null)
					AppendFixedValueWithPadding(rows, prePadding, _prefix, skipRow);

				// render sub-groups
				var firstGroup = true;
				for (var g = 0; g < data.SubGroups!.Count; g++)
				{
					var subGroup = data.SubGroups[g];

					// calculate empty flag for each column in group for current subgroup for each row
					// but only if group has dependent fixed columns as only them need this data
					bool[][]? emptySubGroupColumns = null;

					if (_hasDependentColumns)
						emptySubGroupColumns = new bool[rows.Length][];

					// calculate empty sub-group flag per-row
					var subGroupEmptyForRow = new bool[rows.Length];

					for (var i = 0; i < subGroupEmptyForRow.Length; i++)
					{
						subGroupEmptyForRow[i] = IsEmpty(subGroup, i);

						if (emptySubGroupColumns != null)
						{
							emptySubGroupColumns[i] = new bool[_columns.Count];

							for (var j = 0; j < _columns.Count; j++)
								emptySubGroupColumns[i][j] = subGroup[j].IsEmpty(i);
						}
					}

					// generate sub-group separator or reserve space in padding if sub-group rendering should be skipped
					// for row
					if (firstGroup)
						firstGroup = false;
					else if (_separator != null)
						AppendFixedValueWithPadding(rows, prePadding, _separator, subGroupEmptyForRow);

					// render child columns
					for (var i = 0; i < _columns.Count; i++)
					{
						var column = _columns[i];

						     if (column is FixedColumn  fixedColumn ) prePadding = fixedColumn .WriteColumn(rows, subGroupEmptyForRow, (FixedColumnData )subGroup[i], emptySubGroupColumns, i   , prePadding);
						else if (column is SimpleColumn simpleColumn) prePadding = simpleColumn.WriteColumn(rows, subGroupEmptyForRow, (SimpleColumnData)subGroup[i], null,                 null, prePadding);
						else if (column is ColumnGroup  listColumn  ) prePadding = listColumn  .WriteColumn(rows, subGroupEmptyForRow, (ColumnGroupData )subGroup[i], null,                 null, prePadding);
						else
							throw new InvalidOperationException($"Unsupported column type: {column.GetType()}");
					}
				}

				// if prefix specified, render it or if rendering should be skipped, append its length to padding
				if (_suffix != null)
					AppendFixedValueWithPadding(rows, prePadding, _suffix, skipRow);

				return prePadding;
			}

			/// <summary>
			/// Appends fixed-side value to rows with optional padding or increase padding for skipped row.
			/// </summary>
			/// <param name="rows">Row generators.</param>
			/// <param name="prePadding">Per-row paddings.</param>
			/// <param name="value">Value to append.</param>
			/// <param name="skipRow">Row skip flags.</param>
			private void AppendFixedValueWithPadding(StringBuilder[] rows, int[] prePadding, string value, bool[] skipRow)
			{
				for (var i = 0; i < rows.Length; i++)
				{
					if (skipRow[i])
						prePadding[i] += value.Length;
					else
					{
						if (prePadding[i] != 0)
						{
							rows[i].Append(' ', prePadding[i]);
							prePadding[i] = 0;
						}

						rows[i].Append(value);
					}
				}
			}

			/// <summary>
			/// Checks wether specific subgroup is empty for specific row.
			/// </summary>
			/// <param name="subGroup">Sub-group data storage.</param>
			/// <param name="rowIndex">Row index.</param>
			/// <returns><c>true</c> if sub-group is empty for specific row.</returns>
			private bool IsEmpty(IReadOnlyList<ColumnDataBase> subGroup, int rowIndex)
			{
				foreach (var column in subGroup)
				{
					if (!column.IsEmpty(rowIndex))
						return false;
				}

				return true;
			}

			public override int GetMaxLength(ColumnGroupData data)
			{
				AssertPopulated();

				// if length is not calculated yet - calculate it
				if (_maxLength == -1)
				{
					_maxLength = 0;

					if (data.SubGroups?.Count > 0)
					{
						// for non-empty group reserve space for decorators
						_maxLength += _prefix?.Length ?? 0;
						_maxLength += _suffix?.Length ?? 0;
						_maxLength += (_separator?.Length ?? 0) * (data.SubGroups!.Count - 1);

						// for each sub-group add sizes of child columns for specific sub-group instance
						foreach (var group in data.SubGroups!)
						{
							for (var i = 0; i < _columns.Count; i++)
							{
								var column = _columns[i];

								     if (column is FixedColumn  fixedColumn ) _maxLength += fixedColumn.GetMaxLength ((FixedColumnData)group[i] );
								else if (column is SimpleColumn simpleColumn) _maxLength += simpleColumn.GetMaxLength((SimpleColumnData)group[i]);
								else if (column is ColumnGroup  listColumn  ) _maxLength += listColumn.GetMaxLength  ((ColumnGroupData)group[i] );
								else
									throw new InvalidOperationException($"Unsupported column type: {column.GetType()}");
							}
						}
					}
				}

				return _maxLength;
			}

			/// <summary>
			/// Adds child column descriptor to group.
			/// </summary>
			/// <param name="column">Child column descriptor.</param>
			internal void AddColumn(ColumnBase column)
			{
				AssertNotConfigured();

				_columns.Add(column);

				// add named column to lookup
				if (column.Name != null)
					_columnsMap.Add(column.Name, (column, _columns.Count - 1));

				// for fixed dependent column set _hasDependentColumns flag
				if (!_hasDependentColumns && column is FixedColumn fixedColumn && fixedColumn.IsDependent())
					_hasDependentColumns = true;
			}

			public override void Freeze()
			{
				base.Freeze();

				foreach (var column in _columns)
					column.Freeze();
			}

			public override void FinalizeConfiguration()
			{
				base.FinalizeConfiguration();

				foreach (var column in _columns)
					column.FinalizeConfiguration();
			}

			/// <summary>
			/// Returns child column group and it's position (index) in group.
			/// </summary>
			/// <param name="name">Group name.</param>
			/// <returns>Child column group and index.</returns>
			public (ColumnGroup group, int index) GetListColumn(string name)
			{
				if (!_columnsMap.TryGetValue(name, out var column))
					throw new InvalidOperationException($"Column group '{name}' not found");

				if (column.column is not ColumnGroup list)
					throw new InvalidOperationException($"Column '{name}' is not a column group");

				return (list, column.index);
			}

			/// <summary>
			/// Returns child simple column and it's position (index) in group.
			/// </summary>
			/// <param name="name">Simple column name.</param>
			/// <returns>Child column descriptor and index.</returns>
			public (SimpleColumn column, int index) GetColumn(string name)
			{
				if (!_columnsMap.TryGetValue(name, out var column))
					throw new InvalidOperationException($"Column '{name}' not found");
				if (column.column is not SimpleColumn simpleColumn)
					throw new InvalidOperationException($"Column '{name}' is not a simple column");

				return (simpleColumn, column.index);
			}
		}
		#endregion
	}
}
