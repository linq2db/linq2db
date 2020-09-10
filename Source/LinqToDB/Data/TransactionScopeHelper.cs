using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Data
{
	internal static class TransactionScopeHelper
	{
		static readonly Func<object?> _getCurrentScopeDelegate = GetTransactionScopeFunc();

		public static bool IsInsideTransactionScope
		{
			get
			{
				var ts = _getCurrentScopeDelegate();
				return ts != null;
			}
		}

		static Func<object?> GetTransactionScopeFunc()
		{
			// netfx: "System.Transactions"
			// netcore: "System.Transactions.Local"
			// check for both names as I'm not sure how it will work with netstandard builds
			var assembly = AppDomain.CurrentDomain.GetAssemblies()
				.FirstOrDefault(a => a.GetName().Name == "System.Transactions" || a.GetName().Name == "System.Transactions.Local");

			if (assembly != null)
			{
				var t = assembly.GetType("System.Transactions.Transaction")!;
				var currentDataProperty = t.GetProperty("Current", BindingFlags.Public | BindingFlags.Static);
				if (currentDataProperty != null)
				{
					var body   = Expression.MakeMemberAccess(null, currentDataProperty);
					var lambda = Expression.Lambda<Func<object?>>(body);
					return lambda.Compile;
				}
			}

			return () => null;
		}
	}
}
