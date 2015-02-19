using System;
using System.Text;

namespace LinqToDB.DataProvider
{
	class DataTools
	{
		static readonly char[] _escapes = { '\x0', '\'' };

		public static void ConvertStringToSql(StringBuilder stringBuilder, string startString, string func, string value)
		{
			if (value.Length > 0 && value.IndexOfAny(_escapes) >= 0)
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
								stringBuilder.Append("' + ");
							}

							stringBuilder
								.Append(func)
								.Append('(')
								.Append((int)c)
								.Append(')')
								;

							break;

						case '\''  :
							if (!isInString)
							{
								isInString = true;

								if (i != 0)
									stringBuilder.Append(" + ");

								stringBuilder.Append(startString);
							}

							stringBuilder.Append("''");

							break;

						default   :
							if (!isInString)
							{
								isInString = true;

								if (i != 0)
									stringBuilder.Append(" + ");

								stringBuilder.Append(startString);
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
					.Append(startString)
					.Append(value)
					.Append('\'')
					;
			}
		}

		public static void ConvertCharToSql(StringBuilder stringBuilder, string startString, string func, char value)
		{
			switch (value)
			{
				case '\x0' :
					stringBuilder
						.Append(func)
						.Append('(')
						.Append((int)value)
						.Append(')')
						;
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
	}
}
