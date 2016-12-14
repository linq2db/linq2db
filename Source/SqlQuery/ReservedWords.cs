using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinqToDB.Extensions;

namespace LinqToDB.SqlQuery
{
	public static class ReservedWords
	{
		static ReservedWords()
		{
			var assembly = typeof(SelectQuery).AssemblyEx();
			var name = assembly.GetManifestResourceNames().Single(_ => _.EndsWith("ReservedWords.txt"));
#if NETFX_CORE
			using (var stream = assembly.GetManifestResourceStream(name))
#else
			using (var stream = assembly.GetManifestResourceStream(name))
#endif
			using (var reader = new StreamReader(stream))
			{
				string s;
				while ((s = reader.ReadLine()) != null)
					ReservedWords.ReservedWordsDictionary.Add(s, s);
			}

			name = assembly.GetManifestResourceNames().Single(_ => _.EndsWith("ReservedWordsPostgres.txt"));
#if NETFX_CORE
			using (var stream = assembly.GetManifestResourceStream(name))
#else
			using (var stream = assembly.GetManifestResourceStream(name))
#endif
			using (var reader = new StreamReader(stream))
			{
				string s;
				while ((s = reader.ReadLine()) != null)
					ReservedWords.ReservedWordsDictionaryPostgres.Add(s, s);
			}

			name = assembly.GetManifestResourceNames().Single(_ => _.EndsWith("ReservedWordsOracle.txt"));
#if NETFX_CORE
			using (var stream = assembly.GetManifestResourceStream(name))
#else
			using (var stream = assembly.GetManifestResourceStream(name))
#endif
			using (var reader = new StreamReader(stream))
			{
				string s;
				while ((s = reader.ReadLine()) != null)
					ReservedWords.ReservedWordsDictionaryOracle.Add(s, s);
			}
		}

		public static readonly Dictionary<string, object> ReservedWordsDictionary = new Dictionary<string, object>();

		public static readonly Dictionary<string, object> ReservedWordsDictionaryPostgres = new Dictionary<string, object>();

		public static readonly Dictionary<string, object> ReservedWordsDictionaryOracle = new Dictionary<string, object>();
	}
}
