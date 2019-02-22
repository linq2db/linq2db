using System;
using System.Data;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider
{
	public class DataTools
	{
		static readonly char[] _escapes = { '\x0', '\'' };

		public static void ConvertStringToSql(
			StringBuilder stringBuilder,
			string plusOperator,
			string startPrefix,
			Action<StringBuilder,int> appendConversion,
			string value,
			char[] extraEscapes)
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
	}
}
