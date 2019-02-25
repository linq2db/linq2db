using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using JetBrains.Annotations;

using LinqToDB.Extensions;
using LinqToDB.Mapping;
using LinqToDB.Reflection;

namespace LinqToDB.Tools
{
	public static class EnumerableExtensions
	{
		class ValueHolder<T>
		{
			public T Value;
		}

		/// <summary>
		/// Returns detailed exception text.
		/// </summary>
		/// <param name="source">Source to process.</param>
		/// <param name="stringBuilder"><see cref="StringBuilder"/> instance.</param>
		/// <returns>Detailed exception text.</returns>
		[JetBrains.Annotations.NotNull, Pure]
		public static StringBuilder ToDiagnosticString<T>(
			[JetBrains.Annotations.NotNull] this IEnumerable<T> source,
			[JetBrains.Annotations.NotNull] StringBuilder stringBuilder)
		{
			if (source        == null) throw new ArgumentNullException(nameof(source));
			if (stringBuilder == null) throw new ArgumentNullException(nameof(stringBuilder));

			if (MappingSchema.Default.IsScalarType(typeof(T)))
				return source.Select(value => new ValueHolder<T> { Value = value }).ToDiagnosticString(stringBuilder);

			var ta         = TypeAccessor.GetAccessor<T>();
			var itemValues = new List<string[]>();

			foreach (var item in source)
			{
				var values = new string[ta.Members.Count];

				for (var i = 0; i < ta.Members.Count; i++)
				{
					var member = ta.Members[i];
					var value  = member.GetValue(item);
					var type   = ta.Members[i].Type.ToNullableUnderlying();

					if      (value == null)            values[i] = "<NULL>";
					else if (type == typeof(decimal))  values[i] = ((decimal) value).ToString("G");
					else if (type == typeof(DateTime)) values[i] = ((DateTime)value).ToString("yyy-MM-dd hh:mm:ss");
					else                               values[i] = value.ToString();
				}

				itemValues.Add(values);
			}

			stringBuilder
				.Append("Count : ").Append(itemValues.Count).AppendLine()
				;

			var lens = ta.Members.Select(m => m.Name.Length).ToArray();

			foreach (var values in itemValues)
				for (var i = 0; i < lens.Length; i++)
					lens[i] = Math.Max(lens[i], values[i].Length);

			void PrintDivider()
			{
				foreach (var len in lens)
					stringBuilder.Append("+-").Append('-', len).Append("-");
				stringBuilder.Append("+").AppendLine();
			}

			PrintDivider();

			for (var i = 0; i < lens.Length; i++)
			{
				var member = ta.Members[i];
				stringBuilder.Append("| ").Append(member.Name).Append(' ', lens[i] - member.Name.Length).Append(" ");
			}

			stringBuilder.Append("|").AppendLine();

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

					stringBuilder.Append(" ");
				}

				stringBuilder.Append("|").AppendLine();
			}

			PrintDivider();

			stringBuilder
				.AppendLine()
				;

			return stringBuilder;
		}

		/// <summary>
		/// Returns detailed exception text.
		/// </summary>
		/// <param name="source">Source to process.</param>
		/// <returns>Detailed exception text.</returns>
		[JetBrains.Annotations.NotNull, Pure]
		public static string ToDiagnosticString<T>([JetBrains.Annotations.NotNull] this IEnumerable<T> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			return source.ToDiagnosticString(new StringBuilder()).ToString();
		}
	}
}
