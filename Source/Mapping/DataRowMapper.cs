using System;
using System.Data;
using System.Collections;

namespace LinqToDB.Mapping
{
	public class DataRowMapper : MapDataSourceDestinationBase
	{
		bool                    _createColumns;
		readonly DataRowVersion _version;

		public DataRowMapper(DataRow dataRow)
			: this(dataRow, DataRowVersion.Default)
		{
		}

		public DataRowMapper(DataRowView view)
			: this(view.Row, view.RowVersion)
		{
		}

		public DataRowMapper(DataRow dataRow, DataRowVersion version)
		{
			_version = version;

			Init(dataRow);
		}

		private void Init(DataRow dataRow)
		{
			if (_dataRow == null && dataRow != null)
				_createColumns = dataRow.Table.Columns.Count == 0;

			_dataRow = dataRow;
		}

		private DataRow _dataRow;
		public  DataRow  DataRow
		{
			get { return _dataRow; }
			set { Init(value);     }
		}

		#region IMapDataSource Members

		public override int Count
		{
			get { return _dataRow.Table.Columns.Count; }
		}

		public override Type GetFieldType(int index)
		{
			return index < _dataRow.Table.Columns.Count?
				_dataRow.Table.Columns[index].DataType: null;
		}

		public override string GetName(int index)
		{
			return _dataRow.Table.Columns[index].ColumnName;
		}

		public override object GetValue(object o, int index)
		{
			object value = _version == DataRowVersion.Default ? _dataRow[index] : _dataRow[index, _version];
			return value is DBNull? null: value;
		}

		public override object GetValue(object o, string name)
		{
			object value = _version == DataRowVersion.Default ? _dataRow[name] : _dataRow[name, _version];
			return value is DBNull? null: value;
		}

		public override bool IsNull(object o, int index)
		{
			if (_version == DataRowVersion.Default)
				return _dataRow.IsNull(index);

			DataColumn col = _dataRow.Table.Columns[index];

			return _dataRow.IsNull(col, _version);
		}

		#endregion

		#region IMapDataDestination Members

		private ArrayList _nameList;

		public override int GetOrdinal(string name)
		{
			if (_createColumns)
			{
				if (_nameList == null)
					_nameList = new ArrayList();

				for (int i = 0; i < _nameList.Count; i++)
					if (name == _nameList[i].ToString())
						return i;

				return _nameList.Add(name);
			}

			return _dataRow.Table.Columns.IndexOf(name);
		}

		private void CreateColumn(int index, object value)
		{
			if (_dataRow.Table.Rows.Count > 1)
			{
				_createColumns = false;
			}
			else
			{
				DataColumnCollection cc   = _dataRow.Table.Columns;
				string               name = _nameList[index].ToString();

				DataColumn column = 
					value == null || value is DBNull? cc.Add(name): cc.Add(name, value.GetType());

				if (cc.IndexOf(column) != index)
					throw new MappingException(string.Format("Cant create data column '{0}'.", name));
			}
		}

		public override void SetValue(object o, int index, object value)
		{
			if (_createColumns)
				CreateColumn(index, value);

			if (value == null || value is DBNull)
			{
				_dataRow[index] = DBNull.Value;
			}
			else
			{
				DataColumn column = _dataRow.Table.Columns[index];

				if (column.DataType != value.GetType())
				{
					if (column.DataType == typeof(Guid))
					{
						value = new Guid(value.ToString());
					}
					else
					{
						if (column.DataType != typeof(string))
							value = Convert.ChangeType(value, column.DataType);
					}
				}

				_dataRow[index] = value;
			}
		}

		public override void SetValue(object o, string name, object value)
		{
			if (_createColumns)
				CreateColumn(GetOrdinal(name), value);

			if (value == null || value is DBNull)
			{
				_dataRow[name] = DBNull.Value;
			}
			else
			{
				DataColumn dc = _dataRow.Table.Columns[name];

				if (dc.DataType != value.GetType())
				{
					if (dc.DataType == typeof(Guid))
					{
						value = new Guid(value.ToString());
					}
					else
					{
						if (dc.DataType != typeof(string))
							value = Convert.ChangeType(value, dc.DataType);
					}
				}

				_dataRow[name] = value;
			}
		}

		#endregion
	}
}
