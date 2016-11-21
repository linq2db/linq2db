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
		}

		public static readonly Dictionary<string, object> ReservedWordsDictionary = new Dictionary<string, object>();
	}
}
