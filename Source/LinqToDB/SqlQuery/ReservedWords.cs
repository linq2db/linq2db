using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	public static class ReservedWords
	{
		static ReservedWords()
		{
			_reservedWords[string.Empty]               = _reservedWordsAll;
			_reservedWords[ProviderName.PostgreSQL]    = _reservedWordsPostgres;
			_reservedWords[ProviderName.Oracle]        = _reservedWordsOracle;
			_reservedWords[ProviderName.Firebird]      = _reservedWordsFirebird;
			_reservedWords[ProviderName.Informix]      = _reservedWordsInformix;

			var assembly = typeof(SelectQuery).Assembly;
			var name = assembly.GetManifestResourceNames().Single(_ => _.EndsWith("ReservedWords.txt"));

			using (var stream = assembly.GetManifestResourceStream(name)!)
			using (var reader = new StreamReader(stream))
			{
				string? s;
				while ((s = reader.ReadLine()) != null)
				{
					if (!s.StartsWith("#"))
					{
						_reservedWordsAll     .Add(s);
						_reservedWordsInformix.Add(s);
					}
				}
			}

			name = assembly.GetManifestResourceNames().Single(_ => _.EndsWith("ReservedWordsPostgres.txt"));

			using (var stream = assembly.GetManifestResourceStream(name)!)
			using (var reader = new StreamReader(stream))
			{
				string? s;
				while ((s = reader.ReadLine()) != null)
				{
					if (!s.StartsWith("#"))
					{
						_reservedWordsPostgres.Add(s);
						_reservedWordsAll     .Add(s);
					}
				}
			}

			name = assembly.GetManifestResourceNames().Single(_ => _.EndsWith("ReservedWordsOracle.txt"));

			using (var stream = assembly.GetManifestResourceStream(name)!)
			using (var reader = new StreamReader(stream))
			{
				string? s;
				while ((s = reader.ReadLine()) != null)
				{
					if(!s.StartsWith("#"))
					{
						_reservedWordsOracle.Add(s);
						_reservedWordsAll   .Add(s);
					}
				}
			}

			name = assembly.GetManifestResourceNames().Single(_ => _.EndsWith("ReservedWordsFirebird.txt"));

			using (var stream = assembly.GetManifestResourceStream(name)!)
			using (var reader = new StreamReader(stream))
			{
				string? s;
				while ((s = reader.ReadLine()) != null)
				{
					if (!s.StartsWith("#"))
					{
						_reservedWordsFirebird.Add(s);
						_reservedWordsAll     .Add(s);
					}
				}
			}

			_reservedWordsInformix.Add("item");
			_reservedWordsAll     .Add("item");
		}

		static readonly HashSet<string> _reservedWordsAll      = new (StringComparer.OrdinalIgnoreCase);
		static readonly HashSet<string> _reservedWordsPostgres = new (StringComparer.OrdinalIgnoreCase);
		static readonly HashSet<string> _reservedWordsOracle   = new (StringComparer.OrdinalIgnoreCase);
		static readonly HashSet<string> _reservedWordsFirebird = new (StringComparer.OrdinalIgnoreCase);
		static readonly HashSet<string> _reservedWordsInformix = new (StringComparer.OrdinalIgnoreCase);

		static readonly ConcurrentDictionary<string,HashSet<string>> _reservedWords = new (StringComparer.OrdinalIgnoreCase);

		public static bool IsReserved(string word, string? providerName = null)
		{
			if (providerName == null)
				return _reservedWordsAll.Contains(word);

			if (!_reservedWords.TryGetValue(providerName, out var words))
				words = _reservedWordsAll;

			return words.Contains(word);
		}

		public static void Add(string word, string? providerName = null)
		{
			lock (_reservedWordsAll)
				_reservedWordsAll.Add(word);

			if (providerName == null)
				return;

			var set = _reservedWords.GetOrAdd(providerName, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

			lock (set)
				set.Add(word);
		}
	}
}
