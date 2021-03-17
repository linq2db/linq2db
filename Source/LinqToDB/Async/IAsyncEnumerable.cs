#if !NATIVE_ASYNC
using System.Threading;

namespace LinqToDB.Async
{
	///// <summary>
	///// Asynchronous version of the IEnumerable&lt;T&gt; interface, allowing elements of the
	///// enumerable sequence to be retrieved asynchronously.
	///// </summary>
	///// <typeparam name="T">Element type.</typeparam>
	//[PublicAPI]

	/// <summary>
	/// This API supports the LinqToDB infrastructure and is not intended to be used  directly from your code.
	/// This API may change or be removed in future releases.
	/// </summary>
	public interface IAsyncEnumerable<out T>
	{
		///// <summary>
		///// Gets an asynchronous enumerator over the sequence.
		///// </summary>
		///// <returns>Enumerator for asynchronous enumeration over the sequence.</returns>

		/// <summary>
		/// This API supports the LinqToDB infrastructure and is not intended to be used  directly from your code.
		/// This API may change or be removed in future releases.
		/// </summary>
		IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default);
	}
}
#endif
