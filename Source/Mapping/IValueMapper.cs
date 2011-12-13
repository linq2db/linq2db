using System;

namespace LinqToDB.Mapping
{
	[CLSCompliant(false)]
	public interface IValueMapper
	{
		void Map(
			IMapDataSource      source, object sourceObject, int sourceIndex,
			IMapDataDestination dest,   object destObject,   int destIndex);
	}
}
