using System.Diagnostics;

namespace LinqToDB.Linq.Builder
{
	static class KDH
	{
		public static KDH<TKey, TData> Create<TKey, TData>(TKey key, TData data)
		{
			return new KDH<TKey, TData>(key, data);
		}
	}

	[DebuggerDisplay("Key: {Key}, Data: {Data}")]
	sealed class KDH<TKey, TData>
	{
		public KDH()
		{
		}

		public KDH(TKey key, TData data)
		{
			Key  = key;
			Data = data;
		}

		public TKey  Key  { get; set; } = default!;
		public TData Data { get; set; } = default!;
	}

	[DebuggerDisplay("Key: {Key}, Data: {Data}")]
	sealed class FKDH<TKey, TData>
	{
		public FKDH()
		{
		}

		public FKDH(TKey key, TData data)
		{
			Key  = key;
			Data = data;
		}

		public TKey  Key  { get; set; } = default!;
		public TData Data { get; set; } = default!;
	}
}
