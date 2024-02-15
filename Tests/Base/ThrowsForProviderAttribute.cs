using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
	public class ThrowsForProviderAttribute<TException> : ThrowsWhenAttribute
	where TException: Exception
	{
		public ThrowsForProviderAttribute(params string[] providers) : base("context", typeof(TException), providers)
		{
			var allProviders =
				from p in providers
				from sp in p.Split(',')
				from n in new[] { sp, sp + ".LinqService" }
				select n;

			Providers = new HashSet<string>(allProviders);
		}

		public HashSet<string> Providers { get; set; }

		public override bool ExpectsException(object parameterValue)
		{
			if (parameterValue is string strValue)
			{
				return Providers.Contains(strValue);
			}

			return false;
		}

	}
}
