using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
	public class ThrowsForProviderAttribute : ThrowsWhenAttribute
	{
		public ThrowsForProviderAttribute(Type exceptionType, params string[] providers) : base("context", exceptionType, providers)
		{
			var allProviders =
				from p in providers
				from sp in p.Split(',')
				from n in new[] { sp, sp + TestBase.LinqServiceSuffix }
				select n;

			Providers = new HashSet<string>(allProviders);
		}

		public HashSet<string> Providers { get; set; }

		public override bool ExpectsException(object parameterValue)
		{
			if (parameterValue is string strValue)
			{
				return Providers.Count == 0 || Providers.Contains(strValue);
			}

			return false;
		}

	}
}
