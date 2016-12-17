using System;
using System.Collections.Concurrent;
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
			_reservedWords[string.Empty]               = ReservedWordsAll;
			_reservedWords[ProviderName.PostgreSQL]    = ReservedWordsPostgres;
			_reservedWords[ProviderName.PostgreSQL92]  = ReservedWordsPostgres;
			_reservedWords[ProviderName.PostgreSQL93]  = ReservedWordsPostgres;
			_reservedWords[ProviderName.Oracle]        = ReservedWordsOracle;
			_reservedWords[ProviderName.OracleManaged] = ReservedWordsOracle;
			_reservedWords[ProviderName.OracleNative]  = ReservedWordsOracle;


			var assembly = typeof(SelectQuery).AssemblyEx();
			var name = assembly.GetManifestResourceNames().Single(_ => _.EndsWith("ReservedWords.txt"));

			using (var stream = assembly.GetManifestResourceStream(name))
			using (var reader = new StreamReader(stream))
			{
				string s;
				while ((s = reader.ReadLine()) != null)
					ReservedWordsAll.Add(s);
			}

			name = assembly.GetManifestResourceNames().Single(_ => _.EndsWith("ReservedWordsPostgres.txt"));

			using (var stream = assembly.GetManifestResourceStream(name))
			using (var reader = new StreamReader(stream))
			{
				string s;
				while ((s = reader.ReadLine()) != null)
				{
					ReservedWordsPostgres.Add(s);
					ReservedWordsAll     .Add(s);
				}
			}

			name = assembly.GetManifestResourceNames().Single(_ => _.EndsWith("ReservedWordsOracle.txt"));

			using (var stream = assembly.GetManifestResourceStream(name))
			using (var reader = new StreamReader(stream))
			{
				string s;
				while ((s = reader.ReadLine()) != null)
				{
					ReservedWordsOracle.Add(s);
					ReservedWordsAll   .Add(s);
				}
			}
		}

		private static readonly HashSet<string> ReservedWordsAll      = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private static readonly HashSet<string> ReservedWordsPostgres = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private static readonly HashSet<string> ReservedWordsOracle   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		private static ConcurrentDictionary<string, HashSet<string>> _reservedWords =
			new ConcurrentDictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

		public static bool IsReserved(string word, string providerName = null)
		{
			if (string.IsNullOrEmpty(providerName))
				return ReservedWordsAll.Contains(word);

			HashSet<string> words;
			if (!_reservedWords.TryGetValue(providerName, out words))
				words = ReservedWordsAll;

			return words.Contains(word);
		}

		public static void Add(string word, string providerName = null)
		{
			lock (ReservedWordsAll)
				ReservedWordsAll.Add(word);

			if(string.IsNullOrEmpty(providerName))
				return;

			var set = _reservedWords.GetOrAdd(providerName, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

			lock (set)
				set.Add(word);
		}
	}
}
