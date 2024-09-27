using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqToDB.Interceptors
{
	public class ExceptionInterceptor : IExceptionInterceptor
	{
		public virtual void ProcessException(ExceptionEventData eventData, Exception exception) { }
	}
}
