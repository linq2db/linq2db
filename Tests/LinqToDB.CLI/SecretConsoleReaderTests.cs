using System;
using System.Collections.Generic;
using System.IO;

using LinqToDB.CommandLine;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	[TestFixture]
	public sealed class SecretConsoleReaderTests
	{
		const string Prompt     = "Password: ";
		const char   EscapeChar = (char)27;
		const char   CtrlUChar  = (char)21;

		[Test]
		public void TypedCharactersEchoOneMaskCharacterEach()
		{
			var (value, echo) = Read(Text("abc"), Enter);

			using (Assert.EnterMultipleScope())
			{
				value.ShouldBe("abc");
				echo. ShouldBe(Prompt + "***\n");
			}
		}

		[Test]
		public void PastedInputEchoesOneMaskCharacterPerCharacter()
		{
			var (value, echo) = Read(Text("P@ssw0rd-1234"), Enter);

			using (Assert.EnterMultipleScope())
			{
				value.ShouldBe("P@ssw0rd-1234");
				echo. ShouldBe(Prompt + new string('*', 13) + "\n");
			}
		}

		[Test]
		public void BackspaceRemovesLastCharacterAndItsMask()
		{
			var (value, echo) = Read(Text("ab"), [Backspace], Text("c"), Enter);

			using (Assert.EnterMultipleScope())
			{
				value.ShouldBe("ac");
				echo. ShouldBe(Prompt + "**\b \b*\n");
			}
		}

		[Test]
		public void BackspaceOnEmptyEntryEchoesNothing()
		{
			var (value, echo) = Read([Backspace, Backspace], Enter);

			using (Assert.EnterMultipleScope())
			{
				value.ShouldBeEmpty();
				echo. ShouldBe(Prompt + "\n");
			}
		}

		[Test]
		public void EscapeClearsEntryAndRepromptsOnANewLine()
		{
			var (value, echo) = Read(Text("abc"), [Key(EscapeChar, ConsoleKey.Escape)], Text("z"), Enter);

			using (Assert.EnterMultipleScope())
			{
				value.ShouldBe("z");
				echo. ShouldBe(Prompt + "*** (cleared)\n" + Prompt + "*\n");
			}
		}

		[TestCase(true,  TestName = "CtrlUClearsEntry(windows spelling)")]
		[TestCase(false, TestName = "CtrlUClearsEntry(control character only)")]
		public void CtrlUClearsEntry(bool withConsoleKey)
		{
			var ctrlU = withConsoleKey
				? new ConsoleKeyInfo(CtrlUChar, ConsoleKey.U,    false, false, true)
				: new ConsoleKeyInfo(CtrlUChar, ConsoleKey.None, false, false, false);

			var (value, echo) = Read(Text("abc"), [ctrlU], Text("z"), Enter);

			using (Assert.EnterMultipleScope())
			{
				value.ShouldBe("z");
				echo. ShouldBe(Prompt + "*** (cleared)\n" + Prompt + "*\n");
			}
		}

		[Test]
		public void ClearOnEmptyEntryDoesNotReprompt()
		{
			var (value, echo) = Read([Key(EscapeChar, ConsoleKey.Escape)], Enter);

			using (Assert.EnterMultipleScope())
			{
				value.ShouldBeEmpty();
				echo. ShouldBe(Prompt + "\n");
			}
		}

		[Test]
		public void PlainLetterUIsAcceptedRatherThanTreatedAsClear()
		{
			var (value, echo) = Read([new ConsoleKeyInfo('u', ConsoleKey.U, false, false, false)], Enter);

			using (Assert.EnterMultipleScope())
			{
				value.ShouldBe("u");
				echo. ShouldBe(Prompt + "*\n");
			}
		}

		[Test]
		public void ControlKeysAreIgnoredAndEchoNoMask()
		{
			var (value, echo) = Read(
				[
					Key('\t',  ConsoleKey.Tab),
					Key('\0',  ConsoleKey.LeftArrow),
					Key('\0',  ConsoleKey.F1),
				],
				Text("a"),
				Enter);

			using (Assert.EnterMultipleScope())
			{
				value.ShouldBe("a");
				echo. ShouldBe(Prompt + "*\n");
			}
		}

		[Test]
		public void MaskDisabledWritesPromptWithoutMaskCharacters()
		{
			var (value, echo) = ReadCore(false, Text("abc"), [Backspace], [Key(EscapeChar, ConsoleKey.Escape)], Text("z"), Enter);

			using (Assert.EnterMultipleScope())
			{
				value.ShouldBe("z");
				echo. ShouldBe(Prompt + "\n");
			}
		}

		static (string Value, string Echo) Read(params ConsoleKeyInfo[][] keys)
		{
			return ReadCore(true, keys);
		}

		static (string Value, string Echo) ReadCore(bool mask, params ConsoleKeyInfo[][] keys)
		{
			var queue = new Queue<ConsoleKeyInfo>();

			foreach (var batch in keys)
				foreach (var key in batch)
					queue.Enqueue(key);

			// Pin the newline so the expected echo is identical on every platform.
			using var writer = new StringWriter { NewLine = "\n" };

			var value = SecretConsoleReader.Read(Prompt, queue.Dequeue, writer, mask);

			return (value, writer.ToString());
		}

		static ConsoleKeyInfo[] Enter     => [Key('\r', ConsoleKey.Enter)];
		static ConsoleKeyInfo   Backspace => Key('\b', ConsoleKey.Backspace);

		static ConsoleKeyInfo Key(char keyChar, ConsoleKey key)
		{
			return new ConsoleKeyInfo(keyChar, key, false, false, false);
		}

		static ConsoleKeyInfo[] Text(string text)
		{
			var keys = new ConsoleKeyInfo[text.Length];

			for (var i = 0; i < text.Length; i++)
				keys[i] = new ConsoleKeyInfo(text[i], ConsoleKey.None, false, false, false);

			return keys;
		}
	}
}
