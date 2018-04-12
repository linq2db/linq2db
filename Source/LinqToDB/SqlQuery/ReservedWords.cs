using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	using LinqToDB.Extensions;

	public static class ReservedWords
	{
		static ReservedWords()
		{
			_reservedWords[string.Empty]               = _reservedWordsAll;
			_reservedWords[ProviderName.PostgreSQL]    = _reservedWordsPostgres;
			_reservedWords[ProviderName.PostgreSQL92]  = _reservedWordsPostgres;
			_reservedWords[ProviderName.PostgreSQL93]  = _reservedWordsPostgres;
			_reservedWords[ProviderName.PostgreSQL95]  = _reservedWordsPostgres;
			_reservedWords[ProviderName.Oracle]        = _reservedWordsOracle;
			_reservedWords[ProviderName.OracleManaged] = _reservedWordsOracle;
			_reservedWords[ProviderName.OracleNative]  = _reservedWordsOracle;


			var assembly = typeof(SelectQuery).AssemblyEx();
			var name = assembly.GetManifestResourceNames().Single(_ => _.EndsWith("ReservedWords.txt"));

			using (var stream = assembly.GetManifestResourceStream(name))
			using (var reader = new StreamReader(stream))
			{
				string s;
				while ((s = reader.ReadLine()) != null)
					_reservedWordsAll.Add(s);
			}

			name = assembly.GetManifestResourceNames().Single(_ => _.EndsWith("ReservedWordsPostgres.txt"));

			using (var stream = assembly.GetManifestResourceStream(name))
			using (var reader = new StreamReader(stream))
			{
				string s;
				while ((s = reader.ReadLine()) != null)
				{
					_reservedWordsPostgres.Add(s);
					_reservedWordsAll     .Add(s);
				}
			}

			name = assembly.GetManifestResourceNames().Single(_ => _.EndsWith("ReservedWordsOracle.txt"));

			using (var stream = assembly.GetManifestResourceStream(name))
			using (var reader = new StreamReader(stream))
			{
				string s;
				while ((s = reader.ReadLine()) != null)
				{
					_reservedWordsOracle.Add(s);
					_reservedWordsAll   .Add(s);
				}
			}
		}

		static readonly HashSet<string> _reservedWordsAll      = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		static readonly HashSet<string> _reservedWordsPostgres = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		static readonly HashSet<string> _reservedWordsOracle   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		static readonly ConcurrentDictionary<string,HashSet<string>> _reservedWords =
			new ConcurrentDictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

		public static bool IsReserved(string word, string providerName = null)
		{
			if (string.IsNullOrEmpty(providerName))
				return _reservedWordsAll.Contains(word);

			if (!_reservedWords.TryGetValue(providerName, out var words))
				words = _reservedWordsAll;

			return words.Contains(word);
		}

		public static void Add(string word, string providerName = null)
		{
			lock (_reservedWordsAll)
				_reservedWordsAll.Add(word);

			if (string.IsNullOrEmpty(providerName))
				return;

			var set = _reservedWords.GetOrAdd(providerName, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

			lock (set)
				set.Add(word);
		}
	}
}
