using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

using LinqToDB.Common;
using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.SqlQuery
{
	public class QueryElementTextWriter
	{
		HashSet<IQueryElement>? _visited;
		readonly SqlTextWriter  _writer = new();

		public NullabilityContext Nullability { get; }

#if DEBUG
		// character added to visually detect end of string
		public string DebugString => _writer + "¶";
#endif

		public QueryElementTextWriter() : this(NullabilityContext.NonQuery)
		{
		}

		public QueryElementTextWriter(NullabilityContext nullability)
		{
			Nullability = nullability;
		}

		public int Length
		{
			get => _writer.Length;
			set => _writer.Length = value;
		}

		public bool AddVisited(IQueryElement element)
		{
			_visited ??= new();
			return _visited.Add(element);
		}

		public void RemoveVisited(IQueryElement element)
		{
			_visited?.Remove(element);
		}

		public QueryElementTextWriter AppendIdentCheck(string str)
		{
			_writer.AppendIdentCheck(str);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public string ToString(int startIndex, int length)
		{
			return _writer.ToString(startIndex, length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString()
		{
			return _writer.ToString();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter.IndentScope IndentScope()
		{
			return _writer.Indent();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Indent()
		{
			return _writer.IncrementIndent();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int UnIndent()
		{
			return _writer.DecrementIndent();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(object? value)
		{
			_writer.Append(value);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(string? value)
		{
			_writer.Append(value);
			return this;
		}

		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(sbyte value)
		{
			_writer.Append(value);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(byte value)
		{
			_writer.Append(value);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(short value)
		{
			_writer.Append(value);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(int value)
		{
			_writer.Append(value);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(long value)
		{
			_writer.Append(value);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(float value)
		{
			_writer.Append(value);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(double value)
		{
			_writer.Append(value);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(decimal value)
		{
			_writer.Append(value);
			return this;
		}

		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(char value)
		{
			_writer.Append(value);
			return this;
		}

		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(ushort value)
		{
			_writer.Append(value);
			return this;
		}

		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(uint value)
		{
			_writer.Append(value);
			return this;
		}

		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(ulong value)
		{
			_writer.Append(value);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(StringBuilder? value)
		{
			if (value != null)
				Append(value.ToString());
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(char value, int repeatCount)
		{
			_writer.Append(value, repeatCount);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter AppendLine()
		{
			_writer.AppendLine();
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter AppendLine(string str)
		{
			Append(str);
			return AppendLine();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter AppendLine(char value)
		{
			Append(value);
			return AppendLine();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter AppendFormat(string format, object arg0)
		{
			_writer.AppendFormat(format, arg0);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter AppendFormat(string format, params object[] args)
		{
			_writer.AppendFormat(format, args);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Replace(string oldValue, string newValue, int startIndex, int count)
		{
			_writer.Replace(oldValue, newValue, startIndex, count);
			return this;
		}
	}
}
