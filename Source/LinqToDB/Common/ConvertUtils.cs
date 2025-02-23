using System;
using System.Collections.Generic;
using System.Globalization;

using LinqToDB.Extensions;
using LinqToDB.Internal.Common;

namespace LinqToDB.Common
{
	internal static class ConvertUtils
	{
		private static readonly IDictionary<Type, ISet<Type>> _alwaysConvert = new Dictionary<Type, ISet<Type>>()
		{
			{ typeof(byte)  , new HashSet<Type>() { typeof(short), typeof(ushort), typeof(int) , typeof(uint), typeof(long), typeof(ulong) } },
			{ typeof(sbyte) , new HashSet<Type>() { typeof(short), typeof(ushort), typeof(int) , typeof(uint), typeof(long), typeof(ulong) } },
			{ typeof(short) , new HashSet<Type>() { typeof(int)  , typeof(uint)  , typeof(long), typeof(ulong) } },
			{ typeof(ushort), new HashSet<Type>() { typeof(int)  , typeof(uint)  , typeof(long), typeof(ulong) } },
			{ typeof(int)   , new HashSet<Type>() { typeof(long) , typeof(ulong) } },
			{ typeof(uint)  , new HashSet<Type>() { typeof(long) , typeof(ulong) } },
			{ typeof(long)  , new HashSet<Type>() { } },
			{ typeof(ulong) , new HashSet<Type>() { } },
		};

		private static readonly IDictionary<Type, IDictionary<Type, Tuple<IComparable, IComparable>>> _rangedConvert = new Dictionary<Type, IDictionary<Type, Tuple<IComparable, IComparable>>>()
		{
			{ typeof(byte)  , new Dictionary<Type, Tuple<IComparable, IComparable>>()
				{
					{ typeof(sbyte), Tuple.Create((IComparable)(byte)0, (IComparable)(byte)sbyte.MaxValue) }
				}
			},
			{ typeof(sbyte) , new Dictionary<Type, Tuple<IComparable, IComparable>>()
				{
					{ typeof(byte), Tuple.Create((IComparable)(sbyte)0, (IComparable)sbyte.MaxValue) }
				}
			},
			{ typeof(short) , new Dictionary<Type, Tuple<IComparable, IComparable>>()
				{
					{ typeof(sbyte) , Tuple.Create((IComparable)(short)sbyte.MinValue, (IComparable)(short)sbyte.MaxValue) },
					{ typeof(byte ) , Tuple.Create((IComparable)(short)byte .MinValue, (IComparable)(short)byte .MaxValue) },
					{ typeof(ushort), Tuple.Create((IComparable)(short)0             , (IComparable)       short.MaxValue) },
				}
			},
			{ typeof(ushort) , new Dictionary<Type, Tuple<IComparable, IComparable>>()
				{
					{ typeof(sbyte) , Tuple.Create((IComparable)(ushort)0, (IComparable)(ushort)sbyte.MaxValue) },
					{ typeof(byte ) , Tuple.Create((IComparable)(ushort)0, (IComparable)(ushort)byte .MaxValue) },
					{ typeof(short) , Tuple.Create((IComparable)(ushort)0, (IComparable)(ushort)short.MaxValue) },
				}
			},
			{ typeof(int) , new Dictionary<Type, Tuple<IComparable, IComparable>>()
				{
					{ typeof(sbyte) , Tuple.Create((IComparable)(int)sbyte .MinValue, (IComparable)(int)sbyte    .MaxValue) },
					{ typeof(byte ) , Tuple.Create((IComparable)(int)byte  .MinValue, (IComparable)(int)byte     .MaxValue) },
					{ typeof(short) , Tuple.Create((IComparable)(int)short .MinValue, (IComparable)(int)short    .MaxValue) },
					{ typeof(ushort), Tuple.Create((IComparable)(int)ushort.MinValue, (IComparable)(int)ushort   .MaxValue) },
					{ typeof(uint)  , Tuple.Create((IComparable)     0              , (IComparable)     int      .MaxValue) },
				}
			},
			{ typeof(uint) , new Dictionary<Type, Tuple<IComparable, IComparable>>()
				{
					{ typeof(sbyte ), Tuple.Create((IComparable)(uint)0, (IComparable)(uint)sbyte .MaxValue) },
					{ typeof(byte  ), Tuple.Create((IComparable)(uint)0, (IComparable)(uint)byte  .MaxValue) },
					{ typeof(short ), Tuple.Create((IComparable)(uint)0, (IComparable)(uint)short .MaxValue) },
					{ typeof(ushort), Tuple.Create((IComparable)(uint)0, (IComparable)(uint)ushort.MaxValue) },
					{ typeof(int   ), Tuple.Create((IComparable)(uint)0, (IComparable)(uint)int   .MaxValue) },
				}
			},
			{ typeof(long) , new Dictionary<Type, Tuple<IComparable, IComparable>>()
				{
					{ typeof(sbyte ), Tuple.Create((IComparable)(long)sbyte .MinValue, (IComparable)(long)sbyte .MaxValue) },
					{ typeof(byte  ), Tuple.Create((IComparable)(long)byte  .MinValue, (IComparable)(long)byte  .MaxValue) },
					{ typeof(short ), Tuple.Create((IComparable)(long)short .MinValue, (IComparable)(long)short .MaxValue) },
					{ typeof(ushort), Tuple.Create((IComparable)(long)ushort.MinValue, (IComparable)(long)ushort.MaxValue) },
					{ typeof(int   ), Tuple.Create((IComparable)(long)int   .MinValue, (IComparable)(long)int   .MaxValue) },
					{ typeof(uint  ), Tuple.Create((IComparable)(long)uint  .MinValue, (IComparable)(long)uint  .MaxValue) },
					{ typeof(ulong ), Tuple.Create((IComparable)(long)0              , (IComparable)      long  .MaxValue) },
				}
			},
			{ typeof(ulong) , new Dictionary<Type, Tuple<IComparable, IComparable>>()
				{
					{ typeof(sbyte ), Tuple.Create((IComparable)(ulong)0, (IComparable)(ulong)sbyte .MaxValue) },
					{ typeof(byte  ), Tuple.Create((IComparable)(ulong)0, (IComparable)(ulong)byte  .MaxValue) },
					{ typeof(short ), Tuple.Create((IComparable)(ulong)0, (IComparable)(ulong)short .MaxValue) },
					{ typeof(ushort), Tuple.Create((IComparable)(ulong)0, (IComparable)(ulong)ushort.MaxValue) },
					{ typeof(int   ), Tuple.Create((IComparable)(ulong)0, (IComparable)(ulong)int   .MaxValue) },
					{ typeof(uint  ), Tuple.Create((IComparable)(ulong)0, (IComparable)(ulong)uint  .MaxValue) },
					{ typeof(long  ), Tuple.Create((IComparable)(ulong)0, (IComparable)(ulong)long  .MaxValue) },
				}
			},
		};

		public static bool TryConvert(object? value, Type toType, out object? convertedValue)
		{
			convertedValue = null;

			if (value == null)
				return  toType.IsNullableType();

			var from = value.GetType().ToNullableUnderlying();
			var to   = toType         .ToNullableUnderlying();

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

			if (   ranges.Item1.CompareTo(value) <= 0
				&& ranges.Item2.CompareTo(value) >= 0)
			{
				convertedValue = Convert.ChangeType(value, to, CultureInfo.InvariantCulture);
				return true;
			}

			return false;
		}
	}
}
