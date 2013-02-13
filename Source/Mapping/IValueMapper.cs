using System;

namespace LinqToDB.Mapping
{
	[CLSCompliant(false)]
	[Obsolete]
	public interface IValueMapper
	{
		void Map(
			IMapDataSource      source, object sourceObject, int sourceIndex,
			IMapDataDestination dest,   object destObject,   int destIndex);
	}
}
