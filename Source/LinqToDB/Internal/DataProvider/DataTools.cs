using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.DataProvider
{
	public static class DataTools
	{
		/// <summary>
		/// Improved version of <c>Replace("[", "[[]")</c> code, used before.
		/// </summary>
		[return: NotNullIfNotNull(nameof(str))]
		public static string? EscapeUnterminatedBracket(string? str)
		{
			if (str == null)
				return str;

			var nextIndex = str.IndexOf('[');
			if (nextIndex < 0)
				return str;

			var lastIndex = 0;
			using var newStr = Pools.StringBuilder.Allocate();

			while (nextIndex >= 0)
			{
				if (nextIndex != 0)
					newStr.Value.Append(str.AsSpan(lastIndex, nextIndex - lastIndex));

				lastIndex = nextIndex;

				var closeBracket = str.IndexOf(']', nextIndex + 1);
				nextIndex        = str.IndexOf('[', nextIndex + 1);

				if ((closeBracket > 0 && (closeBracket < nextIndex || nextIndex < 0))
				    ||
				    (closeBracket - lastIndex == 2 && closeBracket - nextIndex == 1))
				{
					if (nextIndex < 0)
						newStr.Value.Append('[');
				}
				else
					newStr.Value.Append("[[]");

			}

			newStr.Value.Append(str.AsSpan(lastIndex + 1));
			return newStr.Value.ToString();
		}

		static readonly char[] _escapes = { '\x0', '\'' };

		public static void ConvertStringToSql(
			StringBuilder stringBuilder,
			string plusOperator,
			string? startPrefix,
			Action<StringBuilder,int> appendConversion,
			string value,
			char[]? extraEscapes)
		{
			if (value.Length > 0
				&& (value.IndexOfAny(_escapes) >= 0 || (extraEscapes != null && value.IndexOfAny(extraEscapes) >= 0)))
			{
				var isInString = false;

				for (var i = 0; i < value.Length; i++)
				{
					var c = value[i];

					switch (c)
					{
						case '\x0' :
							if (isInString)
							{
								isInString = false;
								stringBuilder
									.Append('\'');
							}

							if (i != 0)
								stringBuilder
									.Append(' ')
									.Append(plusOperator)
									.Append(' ')
									;

							appendConversion(stringBuilder, c);

							break;

						case '\''  :
							if (!isInString)
							{
								isInString = true;

								if (i != 0)
									stringBuilder
										.Append(' ')
										.Append(plusOperator)
										.Append(' ')
										;

								stringBuilder.Append(startPrefix).Append('\'');
							}

							stringBuilder.Append("''");

							break;

						default   :
							if (extraEscapes != null && extraEscapes.Any(e => e == c))
							{
								if (isInString)
								{
									isInString = false;
									stringBuilder
										.Append('\'');
								}

								if (i != 0)
									stringBuilder
										.Append(' ')
										.Append(plusOperator)
										.Append(' ')
										;

								appendConversion(stringBuilder, c);
								break;
							}

							if (!isInString)
							{
								isInString = true;

								if (i != 0)
									stringBuilder
										.Append(' ')
										.Append(plusOperator)
										.Append(' ')
										;

								stringBuilder.Append(startPrefix).Append('\'');
							}

							stringBuilder.Append(c);

							break;
					}
				}

				if (isInString)
					stringBuilder.Append('\'');
			}
			else
			{
				stringBuilder
					.Append(startPrefix)
					.Append('\'')
					.Append(value)
					.Append('\'')
					;
			}
		}

		public static void ConvertCharToSql(StringBuilder stringBuilder, string startString, Action<StringBuilder,int> appendConversion, char value)
		{
			switch (value)
			{
				case '\x0' :
					appendConversion(stringBuilder,value);
					break;

				case '\''  :
					stringBuilder
						.Append(startString)
						.Append("''")
						.Append('\'')
						;
					break;

				default    :
					stringBuilder
						.Append(startString)
						.Append(value)
						.Append('\'')
						;
					break;
			}
		}

		public static Expression<Func<DbDataReader, int, string>> GetCharExpression = (dr, i) => GetCharFromString(dr.GetString(i));

		private static string GetCharFromString(string str)
		{
			if (str.Length > 0)
				return str[0].ToString();

			return string.Empty;
		}

		#region Create/Drop Database

		internal static void CreateFileDatabase(
			string databaseName,
			bool deleteIfExists,
			string extension,
			Action<string> createDatabase)
		{
			databaseName = databaseName.Trim();

			if (!databaseName.ToLowerInvariant().EndsWith(extension))
				databaseName += extension;

			if (File.Exists(databaseName))
			{
				if (!deleteIfExists)
					return;
				File.Delete(databaseName);
			}

			createDatabase(databaseName);
		}

		internal static void DropFileDatabase(string databaseName, string extension)
		{
			databaseName = databaseName.Trim();

			if (File.Exists(databaseName))
			{
				File.Delete(databaseName);
			}
			else
			{
				if (!databaseName.ToLowerInvariant().EndsWith(extension))
				{
					databaseName += extension;

					if (File.Exists(databaseName))
						File.Delete(databaseName);
				}
			}
		}
		#endregion

		public static void ConvertToIso8601Interval(StringBuilder stringBuilder, TimeSpan interval)
		{
			var addTicks = 0;

			if (interval < TimeSpan.Zero)
			{
				_ = stringBuilder.Append('-');
				if (interval == TimeSpan.MinValue)
				{
					interval = TimeSpan.MaxValue;
					addTicks = 1;
				}
				else
				{
					interval = interval.Negate();
				}
			}

			_ = stringBuilder.Append('P');

			var iv = CultureInfo.InvariantCulture;

			var ticks = interval.Ticks - new TimeSpan(interval.Days, interval.Hours, interval.Minutes, interval.Seconds, 0).Ticks + addTicks;

			if (interval.Days != 0 || interval == TimeSpan.Zero)
			{
				_ = stringBuilder.AppendFormat(iv, "{0}D", interval.Days);
			}

			if (interval.Hours != 0 || interval.Minutes != 0 || interval.Seconds != 0 || ticks != 0)
			{
				_ = stringBuilder.Append('T');
			}

			if (interval.Hours != 0)
			{
				_ = stringBuilder.AppendFormat(iv, "{0}H", interval.Hours);
			}

			if (interval.Minutes != 0
				|| (interval.Hours != 0 && (interval.Seconds != 0 || ticks != 0)))
			{
				_ = stringBuilder.AppendFormat(iv, "{0}M", interval.Minutes);
			}

			if (interval.Seconds != 0 || ticks != 0)
			{
				_ = stringBuilder.AppendFormat(iv, "{0}", interval.Seconds);
				if (ticks > 0)
				{
					_ = stringBuilder.Append('.');
					var ticksStr = ticks.ToString(iv);
					if (ticksStr.Length < 7)
					{
						ticksStr = new string('0', 7 - ticksStr.Length) + ticksStr;
					}

					_ = stringBuilder.Append(ticksStr.TrimEnd('0'));
				}

				_ = stringBuilder.Append('S');
			}
		}

		public static void BuildHexString(StringBuilder stringBuilder, byte[] value)
		{
			foreach (var @byte in value)
				stringBuilder
					.Append("\\x")
					.AppendByteAsHexViaLookup32(@byte);
		}
	}
}
