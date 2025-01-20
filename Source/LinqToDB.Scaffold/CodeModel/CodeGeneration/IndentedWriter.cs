using System;
using System.Text;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Code generation writer with indentation support.
	/// </summary>
	internal sealed class IndentedWriter
	{
		// current line status
		// true : writer position is on new empty line without anything, even padding
		// false: writer position is on line with padding and optionally with some other text
		private          bool          _newLine = true;
		// number of indent levels used for current writer position
		private          int           _currentIndent;
		// character sequence used as newline
		private readonly string        _newLineSequence;
		// character sequence used as one identation element
		private readonly string        _indentValue;

		private readonly StringBuilder _text = new ();

		public IndentedWriter(string newLine, string indent)
		{
			_newLineSequence = newLine;
			_indentValue     = indent;
		}

		/// <summary>
		/// Returns current text in writer.
		/// </summary>
		public string GetText() => _text.ToString();

		/// <summary>
		/// Write provided text to writer.
		/// </summary>
		/// <param name="text">Text to write.</param>
		public void Write(string text)
		{
			WriteIndent();
			_text.Append(text);
		}

		/// <summary>
		/// Write provided character to writer.
		/// </summary>
		/// <param name="chr">Character to write.</param>
		public void Write(char chr)
		{
			WriteIndent();
			_text.Append(chr);
		}

		/// <summary>
		/// Write provided text and append new line to it.
		/// </summary>
		/// <param name="text">Text to write.</param>
		public void WriteLine(string text)
		{
			WriteIndent();
			_text.Append(text);
			_text.Append(_newLineSequence);
			_newLine = true;
		}

		/// <summary>
		/// Write provided character and append new line to it.
		/// </summary>
		/// <param name="chr">Character to write.</param>
		public void WriteLine(char chr)
		{
			WriteIndent();
			_text.Append(chr);
			_text.Append(_newLineSequence);
			_newLine = true;
		}

		/// <summary>
		/// Write provided text without prepending it with indent and append new line to it.
		/// </summary>
		/// <param name="text">Text to write.</param>
		public void WriteUnindentedLine(string text)
		{
			_text.Append(text);
			_text.Append(_newLineSequence);
			_newLine = true;
		}

		/// <summary>
		/// Write provided text without prepending it with indent.
		/// </summary>
		/// <param name="text">Text to write.</param>
		public void WriteUnindented(string text)
		{
			_text.Append(text);
			_newLine = false;
		}

		/// <summary>
		/// Write new line.
		/// </summary>
		public void WriteLine()
		{
			_text.Append(_newLineSequence);
			_newLine = true;
		}

		/// <summary>
		/// Write indent value if we are at new line.
		/// </summary>
		private void WriteIndent()
		{
			if (_newLine)
			{
				for (var i = 0; i < _currentIndent; i++)
					_text.Append(_indentValue);

				_newLine = false;
			}
		}

		/// <summary>
		/// Increase ident size by one.
		/// </summary>
		public void IncreaseIdent() => _currentIndent++;

		/// <summary>
		/// Decrease ident size by one.
		/// </summary>
		public void DecreaseIdent()
		{
			if (_currentIndent == 0)
				throw new InvalidOperationException();

			_currentIndent--;
		}
	}
}
