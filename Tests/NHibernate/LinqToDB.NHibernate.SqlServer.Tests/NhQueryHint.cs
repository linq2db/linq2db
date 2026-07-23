using System;
using System.Threading;
using NHibernate;

namespace LinqToDB.NHibernateExtension.SqlServer.Tests
{
	public class NhQueryHint
	{
		private static AsyncLocal<string> _hint = new AsyncLocal<string>();

		public struct QueryHintScope : IDisposable
		{
			private string _prevHint;

			internal QueryHintScope(string hint)
			{
				_prevHint = _hint.Value;
				_hint.Value = hint;
			}

			public void Dispose()
			{
				_hint.Value = _prevHint;
			}
		}

		public static QueryHintScope Recompile()
		{
			return new QueryHintScope("OPTION(RECOMPILE)");
		}

		public static string CurrentHint => _hint.Value;

		[Serializable]
		public class NhSqlAppenderInterceptor : EmptyInterceptor
		{
			public override NHibernate.SqlCommand.SqlString OnPrepareStatement(NHibernate.SqlCommand.SqlString sql)
			{
				var hintValue = NhQueryHint.CurrentHint;

				if (!string.IsNullOrEmpty(hintValue))
					return sql.Insert(sql.Length, (" " + hintValue));

				return base.OnPrepareStatement(sql);
			}
		}
	}
}

