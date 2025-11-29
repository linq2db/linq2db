using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace LinqToDB.Internal.Common
{
	public sealed class SqlTextWriter
	{
		public readonly struct IndentScope : IDisposable
		{
			readonly SqlTextWriter _writer;

			public IndentScope(SqlTextWriter writer)
			{
				_writer = writer;
				writer.IncrementIndent();
			}

			public void Dispose()
			{
				_writer.DecrementIndent();
			}
		}

		public StringBuilder StringBuilder { get; }

		int  _indentValue;
		bool _isNewLine;

		public SqlTextWriter(int capacity) : this(new StringBuilder(capacity))
		{
		}

		public SqlTextWriter() : this(new StringBuilder())
		{
		}

		public SqlTextWriter(StringBuilder stringBuilder)
		{
			StringBuilder = stringBuilder;
		}

		public int Length
		{
			get => StringBuilder.Length;
			set => StringBuilder.Length = value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public string ToString(int startIndex, int length)
		{
			return StringBuilder.ToString(startIndex, length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString()
		{
			return StringBuilder.ToString();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IndentScope Indent()
		{
			return new IndentScope(this);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int IncrementIndent()
		{
			return ++_indentValue;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int DecrementIndent()
		{
			return --_indentValue;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter Append(string? value)
		{
			AppendIndentIfNeeded();
			StringBuilder.Append(value);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter Append(object? value)
		{
			AppendIndentIfNeeded();
			StringBuilder.Append(CultureInfo.InvariantCulture, $"{value}");
			return this;
		}

		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter Append(sbyte value)
		{
			AppendIndentIfNeeded();
			StringBuilder.Append(value);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter Append(byte value)
		{
			AppendIndentIfNeeded();
			StringBuilder.Append(value);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter Append(short value)
		{
			AppendIndentIfNeeded();
			StringBuilder.Append(value);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter Append(int value)
		{
			AppendIndentIfNeeded();
			StringBuilder.Append(value);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter Append(long value)
		{
			AppendIndentIfNeeded();
			StringBuilder.Append(value);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter Append(float value)
		{
			AppendIndentIfNeeded();
			StringBuilder.Append(CultureInfo.InvariantCulture, $"{value}");
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter Append(double value)
		{
			AppendIndentIfNeeded();
			StringBuilder.Append(CultureInfo.InvariantCulture, $"{value}");
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter Append(decimal value)
		{
			AppendIndentIfNeeded();
			StringBuilder.Append(CultureInfo.InvariantCulture, $"{value}");
			return this;
		}

		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter Append(char value)
		{
			AppendIndentIfNeeded();
			StringBuilder.Append(value);
			return this;
		}

		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter Append(ushort value)
		{
			AppendIndentIfNeeded();
			StringBuilder.Append(value);
			return this;
		}

		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter Append(uint value)
		{
			AppendIndentIfNeeded();
			StringBuilder.Append(value);
			return this;
		}

		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter Append(ulong value)
		{
			AppendIndentIfNeeded();
			StringBuilder.Append(value);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter Append(StringBuilder? value)
		{
			if (value != null)
				StringBuilder.AppendBuilder(value);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter Append(char value, int repeatCount)
		{
			if (_isNewLine)
				AppendIndent();

			StringBuilder.Append(value, repeatCount);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter AppendLine()
		{
			StringBuilder.AppendLine();
			_isNewLine = true;

			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter AppendLine(string str)
		{
			Append(str);
			return AppendLine();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter AppendLine(char value)
		{
			Append(value);
			return AppendLine();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter AppendIndentIfNeeded()
		{
			if (_isNewLine)
				AppendIndent();
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter AppendIndent()
		{
			if (_indentValue > 0)
				StringBuilder.Append('\t', _indentValue);
			_isNewLine = false;
			return this;
		}

		public SqlTextWriter AppendIdentCheck(string str)
		{
			if (str.Contains(Environment.NewLine))
			{
				var split = str.Split('\n');
				for (var index = 0; index < split.Length; index++)
				{
					var line = split[index];
					if (line.EndsWith('\r'))
						AppendLine(line.Substring(0, line.Length - 1));
					else if (index == split.Length - 1)
						Append(line);
					else
						AppendLine(line);
				}
			}
			else
				Append(str);

			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter AppendFormat(string format, object arg0)
		{
			AppendIndentIfNeeded();
			StringBuilder.AppendFormat(CultureInfo.InvariantCulture, format, arg0);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter AppendFormat(string format, params object[] args)
		{
			AppendIndentIfNeeded();
			StringBuilder.AppendFormat(CultureInfo.InvariantCulture, format, args);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter Replace(string oldValue, string newValue, int startIndex, int count)
		{
			StringBuilder.Replace(oldValue, newValue, startIndex, count);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Clear()
		{
			StringBuilder.Clear();
		}

	}
}
