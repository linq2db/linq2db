using System;
using System.Text;
using LinqToDB.CodeGen.CodeGeneration;

namespace LinqToDB.CodeGen.CodeModel
{
	public class IndentedWriter
	{
		private bool _newLine = true;
		private int _currentIndent;

		private readonly CodeGenerationSettings _settings;
		private readonly StringBuilder _text = new ();

		public IndentedWriter(CodeGenerationSettings settings)
		{
			_settings = settings;
		}

		public string GetText() => _text.ToString();

		public void Write(string text)
		{
			WriteIndent();
			_text.Append(text);
		}

		public void Write(char chr)
		{
			WriteIndent();
			_text.Append(chr);
		}

		public void WriteLine(string text)
		{
			WriteIndent();
			_text.Append(text);
			_text.Append(_settings.NewLine);
			_newLine = true;
		}

		public void WriteLine(char chr)
		{
			WriteIndent();
			_text.Append(chr);
			_text.Append(_settings.NewLine);
			_newLine = true;
		}

		public void WriteUnindentedLine(string text)
		{
			_text.Append(text);
			_text.Append(_settings.NewLine);
			_newLine = true;
		}

		public void WriteUnindented(string text)
		{
			_text.Append(text);
			_newLine = false;
		}

		public void WriteLine()
		{
			_text.Append(_settings.NewLine);
			_newLine = true;
		}

		private void WriteIndent()
		{
			if (_newLine && _settings.Indent != null)
				for (var i = 0; i < _currentIndent; i++)
					_text.Append(_settings.Indent);
			_newLine = false;
		}

		public void IncreaseIdent()
		{
			_currentIndent++;
		}

		public void DecreaseIdent()
		{
			if (_currentIndent == 0)
				throw new InvalidOperationException();

			_currentIndent--;
		}
	}
}
