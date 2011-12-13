using System;

using LinqToDB.Reflection;

namespace LinqToDB.Mapping
{
	public class SimpleSourceListMapper : IMapDataSourceList
	{
		[CLSCompliant(false)]
		public SimpleSourceListMapper(IMapDataSource mapper)
		{
			_mapper = mapper;
		}

		private readonly IMapDataSource _mapper;

		#region IMapDataSourceList Members

		public virtual void InitMapping(InitContext initContext)
		{
		}

		public bool SetNextDataSource(InitContext initContext)
		{
			initContext.DataSource = _mapper;
			return _mapper.Count > 0;
		}

		public virtual void EndMapping(InitContext initContext)
		{
		}

		#endregion
	}
}
