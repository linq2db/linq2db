using System.Data;

using LinqToDB.Reflection;

namespace LinqToDB.Mapping
{
	public class DataTableMapper : IMapDataSourceList, IMapDataDestinationList
	{
		public DataTableMapper(DataTable dataTable, DataRowMapper mapper)
		{
			_table  = dataTable;
			_mapper = mapper;
		}

		private readonly DataTable     _table;
		private readonly DataRowMapper _mapper;
		private int                    _currentRow;

		#region IMapDataSourceList Members

		void IMapDataSourceList.InitMapping(InitContext initContext)
		{
			initContext.DataSource = _mapper;
		}

		bool IMapDataSourceList.SetNextDataSource(InitContext initContext)
		{
			if (_currentRow >= _table.Rows.Count)
				return false;

			DataRow row = _table.Rows[_currentRow++];

			if (row.RowState == DataRowState.Deleted)
				return ((IMapDataSourceList)this).SetNextDataSource(initContext);

			_mapper.DataRow          = row;
			initContext.SourceObject = row;

			return true;
		}

		void IMapDataSourceList.EndMapping(InitContext initContext)
		{
		}

		#endregion

		#region IMapDataDestinationList Members

		void IMapDataDestinationList.InitMapping(InitContext initContext)
		{
		}

		IMapDataDestination IMapDataDestinationList.GetDataDestination(InitContext initContext)
		{
			return _mapper;
		}

		object IMapDataDestinationList.GetNextObject(InitContext initContext)
		{
			DataRow row = _table.NewRow();

			_mapper.DataRow = row;
			_table.Rows.Add(row);

			return row;
		}

		void IMapDataDestinationList.EndMapping(InitContext initContext)
		{
		}

		#endregion
	}
}
