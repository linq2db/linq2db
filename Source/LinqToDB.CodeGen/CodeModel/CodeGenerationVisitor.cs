using System;
using System.Collections.Generic;
using LinqToDB.CodeGen.CodeGeneration;

namespace LinqToDB.CodeGen.CodeModel
{
	public abstract class CodeGenerationVisitor : CodeModelVisitor
	{
		protected readonly CodeGenerationSettings Settings;
		private IndentedWriter _writer;

		public string GetResult() => _writer.GetText();

		protected CodeGenerationVisitor(CodeGenerationSettings settings)
		{
			Settings = settings;
			_writer = new IndentedWriter(settings);
		}

		protected abstract string[] NewLineSequences { get; }

		protected string[] SplitByNewLine(string text)
		{
			return text.Split(NewLineSequences, StringSplitOptions.None);
		}

		protected void Write              (string text) => _writer.Write(text);
		protected void Write              (char   chr ) => _writer.Write(chr);
		protected void WriteLine          (string text) => _writer.WriteLine(text);
		protected void WriteLine          (char   chr ) => _writer.WriteLine(chr);
		protected void WriteUnindentedLine(string text) => _writer.WriteUnindentedLine(text);
		protected void WriteUnindented    (string text) => _writer.WriteUnindented(text);
		protected void WriteLine          (           ) => _writer.WriteLine();
		protected void IncreaseIdent      (           ) => _writer.IncreaseIdent();
		protected void DecreaseIdent      (           ) => _writer.DecreaseIdent();

		protected void WriteDelimitedList<T>(IEnumerable<T> items, string delimiter, bool newLine)
			where T: ICodeElement
		{
			var first = true;
			foreach (var item in items)
			{
				if (!first)
				{
					Write(delimiter);
					if (newLine)
						WriteLine();
				}
				else
					first = false;

				Visit(item);
			}

			if (newLine && !first)
				WriteLine();
		}

		protected void WriteDelimitedList<T>(IEnumerable<T> items, Action<T> writer, string delimiter, bool newLine)
		{
			var first = true;
			foreach (var item in items)
			{
				if (!first)
				{
					Write(delimiter);
					if (newLine)
						WriteLine();
				}
				else
					first = false;

				writer(item);
			}

			if (newLine && !first)
				WriteLine();
		}

		protected void WriteNewLineDelimitedList<T>(IEnumerable<T> items)
			where T : ICodeElement
		{
			var first = true;
			foreach (var item in items)
			{
				if (!first)
					WriteLine();
				else
					first = false;

				Visit(item);
			}
		}

		protected string BuildFragment<TBuilder>(Action<TBuilder> fragmenBuilder)
			where TBuilder : CodeGenerationVisitor
		{
			var mainWriter = _writer;
			var writer = _writer = new IndentedWriter(Settings);
			fragmenBuilder((TBuilder)this);
			_writer = mainWriter;
			return writer.GetText();
		}

		protected void WriteWithPadding(string text, int fullLength)
		{
			Write(text);
			while (fullLength > text.Length)
			{
				Write(" ");
				fullLength--;
			}
		}

		protected void WriteXmlText(string line)
		{
			foreach (var chr in line)
			{
				switch (chr)
				{
					case '&': Write("&amp;"); break;
					case '\'': Write("&apos;"); break;
					case '"': Write("&quot;"); break;
					case '>': Write("&gt;"); break;
					case '<': Write("&lt;"); break;
					default: Write(chr); break;
				}
			}
		}


	}
}
