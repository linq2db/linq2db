using System;
using System.Buffers;
#if NET9_0_OR_GREATER
using System.Diagnostics;
using System.Runtime.CompilerServices;
#endif

using Microsoft.AspNetCore.SignalR.Protocol;

namespace LinqToDB.Remote.SignalR
{
	/// <summary>
	/// Size of a Signal/R invocation message, in bytes: a cheap upper bound for both hub protocols, and the exact
	/// size of the JSON frame when the bound is not good enough.
	/// </summary>
	static class RequestSize
	{
		// Longer than any invocation id the client generates (a counter).
		const string InvocationIdPlaceholder = "2147483647";

		static readonly JsonHubProtocol _jsonProtocol = new();

		/// <summary>
		/// Returns the size of the request when it is larger than <paramref name="limit"/>, or <see langword="null"/>
		/// when it fits. The upper bound settles most requests without serializing them; a request above it is
		/// measured as the JSON protocol writes it (the default protocol, and never smaller than MessagePack's frame,
		/// which writes the same strings as UTF-8 without property names). Both leave room for the trace headers the
		/// client may add.
		/// </summary>
		public static long? Exceeds(string methodName, string? configuration, string queryData, long limit)
		{
			var headers  = EstimateHeaders();
			var estimate = Estimate(methodName, configuration, queryData, limit) + headers;

			if (estimate <= limit)
				return null;

			// Every character takes a byte at least, so this request cannot fit whatever the encoding.
			if (queryData.Length > limit)
				return estimate;

			var counter = new CountingBufferWriter();

			_jsonProtocol.WriteMessage(new StreamInvocationMessage(InvocationIdPlaceholder, methodName, [configuration, queryData]), counter);

			var size = counter.Count + headers;

			return size > limit ? size : null;
		}

#if NET9_0_OR_GREATER
		// "headers":{"traceparent":"00-<trace id>-<span id>-<flags>"} of an invocation activity without a parent, with
		// room for the id of a child activity being longer than its parent's (hierarchical ids).
		const int TraceHeadersOverhead = 128;
#endif

		/// <summary>
		/// The .NET 9+ client starts an activity for every invocation when its activity source is listened to
		/// (OpenTelemetry, for example), and puts the activity's trace context into the message headers: traceparent,
		/// tracestate and baggage, which the invocation activity inherits from <c>Activity.Current</c>. The
		/// server counts them against its message size limit. Returns no less than they take, without knowing whether
		/// the client will add them at all.
		/// </summary>
		static long EstimateHeaders()
		{
#if NET9_0_OR_GREATER
			var size = new StrongBox<long>(TraceHeadersOverhead);

			if (Activity.Current is { } parent)
			{
				DistributedContextPropagator.Current.Inject(parent, size, static (carrier, name, value) =>
				{
					// Name, value, a colon and a comma.
					((StrongBox<long>)carrier!).Value += Estimate(name) + Estimate(value) + 2;
				});
			}

			return size.Value;
#else
			return 0;
#endif
		}

		// Counts the bytes written through one reused buffer instead of keeping them.
		sealed class CountingBufferWriter : IBufferWriter<byte>
		{
			byte[] _buffer = new byte[4096];

			public long Count { get; private set; }

			public void Advance(int count)
			{
				Count += count;
			}

			public Memory<byte> GetMemory(int sizeHint = 0)
			{
				if (sizeHint > _buffer.Length)
					_buffer = new byte[sizeHint];

				return _buffer;
			}

			public Span<byte> GetSpan(int sizeHint = 0)
			{
				return GetMemory(sizeHint).Span;
			}
		}
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
