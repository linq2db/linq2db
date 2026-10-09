namespace LinqToDB.Remote.SignalR
{
	/// <summary>
	/// Upper bound of the size of a Signal/R invocation message, in bytes, for both hub protocols.
	/// </summary>
	static class RequestSize
	{
		// Message type, invocation id, "target"/"arguments"/"streamIds" names, brackets and separators of a JSON
		// invocation frame (MessagePack frames are smaller), with room to spare.
		const int FrameOverhead = 256;

		/// <summary>
		/// Returns a size no smaller than the real message. The JSON protocol escapes <c>"</c> and <c>\</c> as two
		/// bytes and, with the default encoder, control, non-ASCII and HTML-sensitive characters as six (<c>\uXXXX</c>);
		/// MessagePack writes UTF-8, at most three bytes per character. So six bytes are counted for every character
		/// that may be escaped and one for the rest.
		/// </summary>
		/// <param name="methodName">Hub method name.</param>
		/// <param name="configuration">First argument.</param>
		/// <param name="queryData">Second argument.</param>
		/// <param name="limit">
		/// When even one byte per character exceeds it, that lower bound is returned without counting.
		/// </param>
		public static long Estimate(string methodName, string? configuration, string queryData, long limit = long.MaxValue)
		{
			var size = FrameOverhead + (long)methodName.Length + Estimate(configuration);

			if (size + queryData.Length > limit)
				return size + queryData.Length;

			return size + Estimate(queryData);
		}

		static long Estimate(string? value)
		{
			if (value == null)
				return 4;

			var size = 2L;

			foreach (var c in value)
			{
				size += c switch
				{
					'"' or '\\'                             => 2,
					'<' or '>' or '&' or '\'' or '+' or '`' => 6,
					>= ' ' and < (char)0x7F                 => 1,
					_                                       => 6,
				};
			}

			return size;
		}
	}
}
