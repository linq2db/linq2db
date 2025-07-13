using System;
using System.Diagnostics;

namespace LinqToDB.Internal.Linq
{
	public class QueryDebugView
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly Func<string> _toExpressionString;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly Func<string> _toQueryString;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly Func<string> _toQueryStringNoParams;

		public QueryDebugView(
			Func<string> toExpressionString,
			Func<string> toQueryString,
			Func<string> toQueryStringNoParams
			)
		{
			_toExpressionString    = toExpressionString;
			_toQueryString         = toQueryString;
			_toQueryStringNoParams = toQueryStringNoParams;
		}

		public virtual string Expression
		{
			get
			{
				try
				{
					return _toExpressionString();
				}
				catch (Exception exception)
				{
					return "Error creating query expression: " + exception;
				}
			}
		}

		public virtual string Query
		{
			get
			{
				try
				{
					return _toQueryString();
				}
				catch (Exception exception)
				{
					return "Error creating query string: " + exception;
				}
			}
		}

		public virtual string QueryNoParams
		{
			get
			{
				try
				{
					return _toQueryStringNoParams();
				}
				catch (Exception exception)
				{
					return "Error creating query string: " + exception;
				}
			}
		}
	}
}
