using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.ServiceModel
{
	using Linq;

	class ServiceModelDataReaderAsync : IDataReaderAsync
	{
		public ServiceModelDataReader Reader;

		public Func<int> SkipAction;
		public Func<int> TakeAction;

		public void Dispose()
		{
			if (Reader != null)
				Reader.Dispose();
		}

		public async Task QueryForEachAsync<T>(Func<IDataReader,T> objectReader, Action<T> action, CancellationToken cancellationToken)
		{
			await Task.Run(() =>
			{
				var skip = SkipAction == null ? 0 : SkipAction();

				while (skip-- > 0 && Reader.Read())
					if (cancellationToken.IsCancellationRequested)
						return;

				var take = TakeAction == null ? int.MaxValue : TakeAction();
				
				while (take-- > 0 && Reader.Read())
					if (cancellationToken.IsCancellationRequested)
						return;
					else
						action(objectReader(Reader));
			},
			cancellationToken);
		}
	}
}
