﻿using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider
{
	public class DataTools
	{
		/// <summary>
		/// Improved version of <c>Replace("[", "[[]")</c> code, used before.
		/// </summary>
		[return: NotNullIfNotNull("str")]
		public static string? EscapeUnterminatedBracket(string? str)
		{
			if (str == null)
				return str;

			var nextIndex = str.IndexOf('[');
			if (nextIndex < 0)
				return str;

			var lastIndex = 0;
			var newStr = new StringBuilder(str.Length + 10);

			while (nextIndex >= 0)
			{
				if (nextIndex != 0)
					newStr.Append(str.Substring(lastIndex, nextIndex - lastIndex));

				lastIndex = nextIndex;

				var closeBracket = str.IndexOf(']', nextIndex + 1);
				nextIndex        = str.IndexOf('[', nextIndex + 1);

				if ((closeBracket > 0 && (closeBracket < nextIndex || nextIndex < 0))
				    ||
				    (closeBracket - lastIndex == 2 && closeBracket - nextIndex == 1))
				{
					if (nextIndex < 0)
						newStr.Append("[");
				}
				else
					newStr.Append("[[]");

			}

			newStr.Append(str.Substring(lastIndex + 1));
			return newStr.ToString();
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
									.Append("'");
							}

							if (i != 0)
								stringBuilder
									.Append(" ")
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
										.Append(" ")
										.Append(plusOperator)
										.Append(' ')
										;

								stringBuilder.Append(startPrefix).Append("'");
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
										.Append("'");
								}

								if (i != 0)
									stringBuilder
										.Append(" ")
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
										.Append(" ")
										.Append(plusOperator)
										.Append(' ')
										;

								stringBuilder.Append(startPrefix).Append("'");
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
					.Append("'")
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

		public static Func<IDataReader, int, string> GetChar = (dr, i) =>
		{
			var str = dr.GetString(i);

			if (str.Length > 0)
				return str[0].ToString();

			return string.Empty;
		};

		#region Create/Drop Database

		internal static void CreateFileDatabase(
			string databaseName,
			bool deleteIfExists,
			string extension,
			Action<string> createDatabase)
		{
			databaseName = databaseName.Trim();

			if (!databaseName.ToLower().EndsWith(extension))
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
				if (!databaseName.ToLower().EndsWith(extension))
				{
					databaseName += extension;

					if (File.Exists(databaseName))
						File.Delete(databaseName);
				}
			}
		}
		#endregion

		internal static DateTime AdjustPrecision(DateTime value, byte precision)
		{
			if (precision > 6)
				return value;

			var delta = value.Ticks % 10000000;

			switch (precision)
			{
				case 1: delta %= 1000000; break;
				case 2: delta %= 100000 ; break;
				case 3: delta %= 10000  ; break;
				case 4: delta %= 1000   ; break;
				case 5: delta %= 100    ; break;
				case 6: delta %= 10     ; break;
			}

			return delta != 0 ? value.AddTicks(-delta) : value;
		}

		internal static DateTimeOffset AdjustPrecision(DateTimeOffset value, byte precision)
		{
			if (precision > 6)
				return value;

			var delta = value.Ticks % 10000000;

			switch (precision)
			{
				case 1: delta %= 1000000; break;
				case 2: delta %= 100000 ; break;
				case 3: delta %= 10000  ; break;
				case 4: delta %= 1000   ; break;
				case 5: delta %= 100    ; break;
				case 6: delta %= 10     ; break;
			}

			return delta != 0 ? value.AddTicks(-delta) : value;
		}
	}
}
