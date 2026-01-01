using System;
using System.Collections.Generic;
using System.Globalization;

using LinqToDB.Internal.Extensions;

namespace LinqToDB.Internal.Conversion
{
	internal static class ConvertUtils
	{
		private static readonly Dictionary<Type, ISet<Type>> _alwaysConvert = new()
		{
			[typeof(byte)  ] = new HashSet<Type>() { typeof(short), typeof(ushort), typeof(int) , typeof(uint), typeof(long), typeof(ulong) },
			[typeof(sbyte) ] = new HashSet<Type>() { typeof(short), typeof(ushort), typeof(int) , typeof(uint), typeof(long), typeof(ulong) },
			[typeof(short) ] = new HashSet<Type>() { typeof(int)  , typeof(uint)  , typeof(long), typeof(ulong) },
			[typeof(ushort)] = new HashSet<Type>() { typeof(int)  , typeof(uint)  , typeof(long), typeof(ulong) },
			[typeof(int)   ] = new HashSet<Type>() { typeof(long) , typeof(ulong) },
			[typeof(uint)  ] = new HashSet<Type>() { typeof(long) , typeof(ulong) },
			[typeof(long)  ] = new HashSet<Type>() { },
			[typeof(ulong) ] = new HashSet<Type>() { },
		};

		private static readonly Dictionary<Type, Dictionary<Type, (IComparable From, IComparable To)>> _rangedConvert =
			new()
			{
				[typeof(byte)] = new()
					{
						[typeof(sbyte)] = ((IComparable)(byte)0, (IComparable)(byte)sbyte.MaxValue),
					},
				[typeof(sbyte)] = new()
					{
						[typeof(byte)] = ((IComparable)(sbyte)0, (IComparable)sbyte.MaxValue),
					},
				[typeof(short)] = new()
					{
						[typeof(sbyte) ] = ((IComparable)(short)sbyte.MinValue, (IComparable)(short)sbyte.MaxValue),
						[typeof(byte ) ] = ((IComparable)(short)byte .MinValue, (IComparable)(short)byte .MaxValue),
						[typeof(ushort)] = ((IComparable)(short)0             , (IComparable)       short.MaxValue),
					},
				[typeof(ushort)] = new()
					{
						[typeof(sbyte) ] = ((IComparable)(ushort)0, (IComparable)(ushort)sbyte.MaxValue),
						[typeof(byte ) ] = ((IComparable)(ushort)0, (IComparable)(ushort)byte .MaxValue),
						[typeof(short) ] = ((IComparable)(ushort)0, (IComparable)(ushort)short.MaxValue),
					},
				[typeof(int)] = new()
					{
						[typeof(sbyte) ] = ((IComparable)(int)sbyte .MinValue, (IComparable)(int)sbyte    .MaxValue),
						[typeof(byte ) ] = ((IComparable)(int)byte  .MinValue, (IComparable)(int)byte     .MaxValue),
						[typeof(short) ] = ((IComparable)(int)short .MinValue, (IComparable)(int)short    .MaxValue),
						[typeof(ushort)] = ((IComparable)(int)ushort.MinValue, (IComparable)(int)ushort   .MaxValue),
						[typeof(uint)  ] = ((IComparable)     0              , (IComparable)     int      .MaxValue),
					},
				[typeof(uint)] = new()
					{
						[typeof(sbyte )] = ((IComparable)(uint)0, (IComparable)(uint)sbyte .MaxValue),
						[typeof(byte  )] = ((IComparable)(uint)0, (IComparable)(uint)byte  .MaxValue),
						[typeof(short )] = ((IComparable)(uint)0, (IComparable)(uint)short .MaxValue),
						[typeof(ushort)] = ((IComparable)(uint)0, (IComparable)(uint)ushort.MaxValue),
						[typeof(int   )] = ((IComparable)(uint)0, (IComparable)(uint)int   .MaxValue),
					},
				[typeof(long)] = new()
					{
						[typeof(sbyte )] = ((IComparable)(long)sbyte .MinValue, (IComparable)(long)sbyte .MaxValue),
						[typeof(byte  )] = ((IComparable)(long)byte  .MinValue, (IComparable)(long)byte  .MaxValue),
						[typeof(short )] = ((IComparable)(long)short .MinValue, (IComparable)(long)short .MaxValue),
						[typeof(ushort)] = ((IComparable)(long)ushort.MinValue, (IComparable)(long)ushort.MaxValue),
						[typeof(int   )] = ((IComparable)(long)int   .MinValue, (IComparable)(long)int   .MaxValue),
						[typeof(uint  )] = ((IComparable)(long)uint  .MinValue, (IComparable)(long)uint  .MaxValue),
						[typeof(ulong )] = ((IComparable)(long)0              , (IComparable)      long  .MaxValue),
					},
				[typeof(ulong)] = new()
					{
						[typeof(sbyte )] = ((IComparable)(ulong)0, (IComparable)(ulong)sbyte .MaxValue),
						[typeof(byte  )] = ((IComparable)(ulong)0, (IComparable)(ulong)byte  .MaxValue),
						[typeof(short )] = ((IComparable)(ulong)0, (IComparable)(ulong)short .MaxValue),
						[typeof(ushort)] = ((IComparable)(ulong)0, (IComparable)(ulong)ushort.MaxValue),
						[typeof(int   )] = ((IComparable)(ulong)0, (IComparable)(ulong)int   .MaxValue),
						[typeof(uint  )] = ((IComparable)(ulong)0, (IComparable)(ulong)uint  .MaxValue),
						[typeof(long  )] = ((IComparable)(ulong)0, (IComparable)(ulong)long  .MaxValue),
					},
			};

		public static bool TryConvert(object? value, Type toType, out object? convertedValue)
		{
			convertedValue = null;

			if (value == null)
				return  toType.IsNullableOrReferenceType();

			var from = value.GetType().UnwrapNullableType();
			var to   = toType         .UnwrapNullableType();

			if (from == to)
			{
				convertedValue = value;
				return true;
			}

			if (_alwaysConvert.TryGetValue(from, out var types) && types.Contains(to))
			{
				convertedValue = Convert.ChangeType(value, to, CultureInfo.InvariantCulture);
				return true;
			}

			if (!_rangedConvert.TryGetValue(from, out var rangedTypes)
				|| !rangedTypes.TryGetValue(to, out var ranges))
				return false;

			if (   ranges.From.CompareTo(value) <= 0
				&& ranges.To  .CompareTo(value) >= 0)
			{
				convertedValue = Convert.ChangeType(value, to, CultureInfo.InvariantCulture);
				return true;
			}

			return false;
		}
	}
}
