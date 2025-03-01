using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using JetBrains.Annotations;

using LinqToDB.Extensions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Mapping;

using LinqToDB.Reflection;

namespace LinqToDB.Tools
{
	public static class EnumerableExtensions
	{
		sealed class ValueHolder<T>
		{
			[UsedImplicitly] public T Value = default!;
		}

		private static readonly string[] _nullItem = new[] { "<NULL>" };

		/// <summary>
		/// Returns well formatted text.
		/// </summary>
		/// <param name="source">Source to process.</param>
		/// <param name="stringBuilder"><see cref="StringBuilder"/> instance.</param>
		/// <param name="addTableHeader">if true (default), adds table header.</param>
		/// <returns>Formatted text.</returns>
		[Pure]
		public static StringBuilder ToDiagnosticString<T>(
			this IEnumerable<T> source,
			StringBuilder stringBuilder,
			bool addTableHeader = true)
		{
			if (source        == null) throw new ArgumentNullException(nameof(source));
			if (stringBuilder == null) throw new ArgumentNullException(nameof(stringBuilder));

			if (MappingSchema.Default.IsScalarType(typeof(T)))
				return source.Select(value => new ValueHolder<T> { Value = value }).ToDiagnosticString(stringBuilder);

			var ta         = TypeAccessor.GetAccessor<T>();
			var itemValues = new List<string[]>();

			foreach (var item in source)
			{
				if (ta.Members.Count > 0)
				{
					var values = new string[ta.Members.Count];

					for (var i = 0; i < ta.Members.Count; i++)
					{
						if (item == null)
						{
							values[i] = "<NULL RECORD>";
							continue;
						}

						var member = ta.Members[i];
						var value  = member.GetValue(item);
						var type   = ta.Members[i].Type.ToNullableUnderlying();

						if      (value == null)            values[i] = "<NULL>";
						else if (type == typeof(decimal))  values[i] = ((decimal) value).ToString("G", DateTimeFormatInfo.InvariantInfo);
						else if (type == typeof(DateTime)) values[i] = ((DateTime)value).ToString("yyy-MM-dd hh:mm:ss", DateTimeFormatInfo.InvariantInfo);
						else                               values[i] = string.Format(CultureInfo.InvariantCulture, "{0}", value);
					}

					itemValues.Add(values);
				}
				else
				{
					itemValues.Add(item == null ? _nullItem : new[] { item.ToString() ?? string.Empty });
				}
			}

			stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"Count : {itemValues.Count}");

			var lens = ta.Members.Count > 0 ? ta.Members.Select(m => m.Name.Length).ToArray() : new[] { ta.Type.Name.Length };

			foreach (var values in itemValues)
				for (var i = 0; i < lens.Length; i++)
					lens[i] = Math.Max(lens[i], addTableHeader ? values[i].Length : 0);

			void PrintDivider()
			{
				foreach (var len in lens)
					stringBuilder.Append("+-").Append('-', len).Append('-');
				stringBuilder.Append('+').AppendLine();
			}

			if (addTableHeader)
			{
				PrintDivider();

				for (var i = 0; i < lens.Length; i++)
				{
					var member = ta.Members[i];
					stringBuilder.Append("| ").Append(member.Name).Append(' ', lens[i] - member.Name.Length).Append(' ');
				}

				stringBuilder.Append('|').AppendLine();
			}

			PrintDivider();

			foreach (var values in itemValues)
			{
				for (var i = 0; i < lens.Length; i++)
				{
					stringBuilder.Append("| ");

					var type  = ta.Members[i].Type.ToNullableUnderlying();
					var right = false;

					switch (Type.GetTypeCode(type))
					{
						case TypeCode.Byte:
						case TypeCode.DateTime:
						case TypeCode.Decimal:
						case TypeCode.Double:
						case TypeCode.Int16:
						case TypeCode.Int32:
						case TypeCode.Int64:
						case TypeCode.SByte:
						case TypeCode.Single:
						case TypeCode.UInt16:
						case TypeCode.UInt32:
						case TypeCode.UInt64:
							right = true;
							break;
					}

					if (right)
						stringBuilder.Append(' ', lens[i] - values[i].Length).Append(values[i]);
					else
						stringBuilder.Append(values[i]).Append(' ', lens[i] - values[i].Length);

					stringBuilder.Append(' ');
				}

				stringBuilder.Append('|').AppendLine();
			}

			PrintDivider();

			stringBuilder
				.AppendLine()
				;

			return stringBuilder;
		}

		/// <summary>
		/// Returns well formatted text.
		/// </summary>
		/// <param name="source">Source to process.</param>
		/// <param name="header">Optional header text.</param>
		/// <param name="addTableHeader">if true (default), adds table header.</param>
		/// <returns>Formatted text.</returns>
		[Pure]
		public static string ToDiagnosticString<T>(
			this IEnumerable<T> source,
			string? header      = null,
			bool addTableHeader = true)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var sb = new StringBuilder();

			if (header != null)
				sb.AppendLine(header);

			return source.ToDiagnosticString(sb, addTableHeader).ToString();
		}
	}
}
