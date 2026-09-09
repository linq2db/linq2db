using System;
using System.IO;
using System.Text;

namespace LinqToDB.CommandLine
{
	/// <summary>
	/// Reads a secret from a console key source, echoing one mask character per accepted character.
	/// </summary>
	public static class SecretConsoleReader
	{
		const char EscapeChar = (char)27;
		const char CtrlUChar  = (char)21;
		const char MaskChar   = '*';

		/// <summary>
		/// Writes <paramref name="prompt"/> to <paramref name="output"/> and accumulates characters from
		/// <paramref name="readKey"/> until Enter is pressed. Backspace removes the last character, Escape and
		/// Ctrl+U clear the whole entry, and other control keys are ignored.
		/// </summary>
		/// <param name="prompt">Prompt written before the first key is read and again after the entry is cleared.</param>
		/// <param name="readKey">Key source; expected to suppress the console's own echo.</param>
		/// <param name="output">Writer receiving the prompt and, when <paramref name="mask"/> is <c>true</c>, the mask characters.</param>
		/// <param name="mask">Whether to echo a mask character per accepted character. Pass <c>false</c> when the writer is not a terminal.</param>
		/// <returns>The accumulated secret.</returns>
		public static string Read(string prompt, Func<ConsoleKeyInfo> readKey, TextWriter output, bool mask)
		{
			output.Write(prompt);

			var value = new StringBuilder();

			while (true)
			{
				var key = readKey();

				if (key.Key == ConsoleKey.Enter)
				{
					output.WriteLine();
					return value.ToString();
				}

				if (key.Key == ConsoleKey.Backspace)
				{
					if (value.Length > 0)
					{
						value.Length--;

						if (mask)
							output.Write("\b \b");
					}

					continue;
				}

				if (IsClear(key))
				{
					if (value.Length > 0)
					{
						value.Length = 0;

						if (mask)
						{
							// Re-prompt on a fresh line instead of backspacing over the mask characters: a long
							// entry wraps, and "\b" does not cross a line boundary, so an erase run would leave
							// mask characters stranded on the previous row.
							output.Write(" (cleared)");
							output.WriteLine();
							output.Write(prompt);
						}
					}

					continue;
				}

				if (!char.IsControl(key.KeyChar))
				{
					value.Append(key.KeyChar);

					if (mask)
						output.Write(MaskChar);
				}
			}
		}

		// Windows and the Unix terminfo path disagree on which ConsoleKeyInfo fields carry these, so match either spelling.
		static bool IsClear(ConsoleKeyInfo key)
		{
			return key.Key     == ConsoleKey.Escape
				|| key.KeyChar == EscapeChar
				|| key.KeyChar == CtrlUChar
				|| (key.Key == ConsoleKey.U && key.Modifiers.HasFlag(ConsoleModifiers.Control));
		}
	}
}
