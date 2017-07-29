#region Copyright Simple Injector Contributors
/* The Simple Injector is an easy-to-use Inversion of Control library for .NET
 * 
 * Copyright (c) 2016 Simple Injector Contributors
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
 * associated documentation files (the "Software"), to deal in the Software without restriction, including 
 * without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
 * copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the 
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial 
 * portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT 
 * LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO 
 * EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER 
 * IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE 
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion

namespace System.Threading
{
#if FW4
	using System.Security;
	using System.Threading;
	using System.Runtime.Remoting.Messaging;

	internal sealed class AsyncLocal<T>
	{
		private readonly string key = Guid.NewGuid().ToString("N").Substring(0, 12);

		public T Value
		{
		   [SecuritySafeCritical]
			get
			{
				var wrapper = (AsyncScopeWrapper)CallContext.LogicalGetData(this.key);

				return wrapper != null ? wrapper.Value : default(T);
			}
			[SecuritySafeCritical]
			set
			{
				var wrapper = value == null ? null : new AsyncScopeWrapper(value);

				CallContext.LogicalSetData(this.key, wrapper);

			}
		}

		[Serializable]
		internal sealed class AsyncScopeWrapper : MarshalByRefObject
		{
			[NonSerialized]
			internal readonly T Value;

			internal AsyncScopeWrapper(T value)
			{
				this.Value = value;
			}
		}
	}
#endif
}