using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;

using LinqToDB.Expressions;
using LinqToDB.Expressions.Types;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// shows small performance degradation due to indirect call
	public class OracleReaderExpressionsBenchmark
	{
#pragma warning disable CS0649 // Field is never assigned to...
		private static readonly int             IntParameter;
#pragma warning restore CS0649 // Field is never assigned to...

		private static readonly ITestDataReader DataReaderParameter = new Original.OracleDataReader();
		private const           int             NanosecondsPerTick  = 100;

		private Func<ITestDataReader, int, DateTimeOffset> _readDateTimeOffsetFromOracleTimeStampTZ = null!;
		private Func<ITestDataReader, int, DateTimeOffset> _readDateTimeOffsetFromOracleTimeStampLTZ = null!;
		private Func<ITestDataReader, int, decimal>        _readOracleDecimalToDecimalAdv = null!;
		private Func<ITestDataReader, int, int>            _readOracleDecimalToInt = null!;
		private Func<ITestDataReader, int, long>           _readOracleDecimalToLong = null!;
		private Func<ITestDataReader, int, decimal>        _readOracleDecimalToDecimal = null!;

		interface ITestDataReader
		{ 
		}

		sealed class Original
		{
			internal sealed class OracleTimeStampTZ
			{
				public static OracleTimeStampTZ Instance { get; } = new OracleTimeStampTZ();

				public int Year       => 2000;
				public int Month      => 2;
				public int Day        => 3;
				public int Hour       => 4;
				public int Minute     => 5;
				public int Second     => 6;
				public int Nanosecond => 7;

				public TimeSpan GetTimeZoneOffset() => TimeSpan.Zero;
			}

			internal sealed class OracleTimeStampLTZ
			{
				public static OracleTimeStampLTZ Instance { get; } = new OracleTimeStampLTZ();

				public OracleTimeStampTZ ToOracleTimeStampTZ() => OracleTimeStampTZ.Instance;
			}

			internal sealed class OracleDecimal
			{
				public static OracleDecimal Instance { get; } = new OracleDecimal();

				public static OracleDecimal SetPrecision(OracleDecimal value1, int precision) => Instance;

				public static explicit operator decimal(OracleDecimal value1) => 1m;
			}

			internal sealed class OracleDataReader : ITestDataReader
			{
				[MethodImpl(MethodImplOptions.NoInlining)]
				public OracleTimeStampTZ  GetOracleTimeStampTZ(int i)  => OracleTimeStampTZ.Instance;
				[MethodImpl(MethodImplOptions.NoInlining)]
				public OracleTimeStampLTZ GetOracleTimeStampLTZ(int i) => OracleTimeStampLTZ.Instance;
				[MethodImpl(MethodImplOptions.NoInlining)]
				public OracleDecimal      GetOracleDecimal(int i)      => OracleDecimal.Instance;
			}
		}

		sealed class Wrapped
		{
			[Wrapper]
			internal sealed class OracleDataReader
			{
				public OracleTimeStampTZ  GetOracleTimeStampTZ(int i)  => throw new NotImplementedException();
				public OracleTimeStampLTZ GetOracleTimeStampLTZ(int i) => throw new NotImplementedException();
				public OracleDecimal      GetOracleDecimal(int i)      => throw new NotImplementedException();
			}

			[Wrapper]
			internal sealed class OracleTimeStampLTZ
			{
				public OracleTimeStampTZ ToOracleTimeStampTZ() => throw new NotImplementedException();
			}

			[Wrapper]
			internal sealed class OracleDecimal
			{
				public static OracleDecimal SetPrecision(OracleDecimal value1, int precision) => throw new NotImplementedException();

				public static explicit operator decimal(OracleDecimal value1) => throw new NotImplementedException();
			}

			[Wrapper]
			internal sealed class OracleTimeStampTZ
			{
				public int Year       => throw new NotImplementedException();
				public int Month      => throw new NotImplementedException();
				public int Day        => throw new NotImplementedException();
				public int Hour       => throw new NotImplementedException();
				public int Minute     => throw new NotImplementedException();
				public int Second     => throw new NotImplementedException();
				public int Nanosecond => throw new NotImplementedException();

				public TimeSpan GetTimeZoneOffset() => throw new NotImplementedException();
			}
		}

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper = new TypeMapper();

			typeMapper.RegisterTypeWrapper<Wrapped.OracleDataReader  >(typeof(Original.OracleDataReader));
			typeMapper.RegisterTypeWrapper<Wrapped.OracleTimeStampTZ >(typeof(Original.OracleTimeStampTZ));
			typeMapper.RegisterTypeWrapper<Wrapped.OracleTimeStampLTZ>(typeof(Original.OracleTimeStampLTZ));
			typeMapper.RegisterTypeWrapper<Wrapped.OracleDecimal     >(typeof(Original.OracleDecimal));

			typeMapper.FinalizeMappings();

			// _readDateTimeOffsetFromOracleTimeStampTZ
			var generator    = new ExpressionGenerator(typeMapper);
			var rdParam      = Expression.Parameter(typeof(ITestDataReader), "rd");
			var indexParam   = Expression.Parameter(typeof(int), "i");
			var tstzExpr     = generator.MapExpression((ITestDataReader rd, int i) => ((Wrapped.OracleDataReader)(object)rd).GetOracleTimeStampTZ(i), rdParam, indexParam);
			var tstzVariable = generator.AssignToVariable(tstzExpr, "tstz");
			var expr         = generator.MapExpression((Wrapped.OracleTimeStampTZ tstz) => new DateTimeOffset(
				tstz.Year, tstz.Month, tstz.Day,
				tstz.Hour, tstz.Minute, tstz.Second,
				tstz.GetTimeZoneOffset()).AddTicks(tstz.Nanosecond / NanosecondsPerTick), tstzVariable);
			generator.AddExpression(expr);
			var body         = generator.Build();
			_readDateTimeOffsetFromOracleTimeStampTZ = ((Expression<Func<ITestDataReader, int, DateTimeOffset>>)Expression.Lambda(body, rdParam, indexParam)).Compile();

			// _readDateTimeOffsetFromOracleTimeStampLTZ
			generator    = new ExpressionGenerator(typeMapper);
			tstzExpr     = generator.MapExpression((ITestDataReader rd, int i) => ((Wrapped.OracleDataReader)(object)rd).GetOracleTimeStampLTZ(i).ToOracleTimeStampTZ(), rdParam, indexParam);
			tstzVariable = generator.AssignToVariable(tstzExpr, "tstz");
			expr         = generator.MapExpression((Wrapped.OracleTimeStampTZ tstz) => new DateTimeOffset(
				tstz.Year, tstz.Month, tstz.Day,
				tstz.Hour, tstz.Minute, tstz.Second,
				tstz.GetTimeZoneOffset()).AddTicks(tstz.Nanosecond / NanosecondsPerTick), tstzVariable);
			generator.AddExpression(expr);
			body         = generator.Build();
			_readDateTimeOffsetFromOracleTimeStampLTZ = ((Expression<Func<ITestDataReader, int, DateTimeOffset>>)Expression.Lambda(body, rdParam, indexParam)).Compile();

			// rd.GetOracleDecimal(i) => decimal
			generator            = new ExpressionGenerator(typeMapper);
			var decExpr          = generator.MapExpression((ITestDataReader rd, int i) => ((Wrapped.OracleDataReader)(object)rd).GetOracleDecimal(i), rdParam, indexParam);
			var oracleDecimalVar = generator.AssignToVariable(decExpr, "dec");
			var precision        = generator.AssignToVariable(Expression.Constant(29), "precision");
			var decimalVar       = generator.AddVariable(Expression.Parameter(typeof(decimal), "dec"));
			var label            = Expression.Label(typeof(decimal));

			generator.AddExpression(
				Expression.Loop(
					Expression.TryCatch(
						Expression.Block(
							Expression.Assign(oracleDecimalVar, generator.MapExpression((Wrapped.OracleDecimal d, int p) => Wrapped.OracleDecimal.SetPrecision(d, p), oracleDecimalVar, precision)),
							Expression.Assign(decimalVar, Expression.Convert(oracleDecimalVar, typeof(decimal))),
							Expression.Break(label, decimalVar)),
						Expression.Catch(
							typeof(OverflowException),
							Expression.Block(
								Expression.IfThen(
									Expression.LessThanOrEqual(Expression.SubtractAssign(precision, Expression.Constant(1)), Expression.Constant(26)),
									Expression.Rethrow())))),
					label));

			body = generator.Build();

			var readOracleDecimalToDecimalAdv = (Expression<Func<ITestDataReader, int, decimal>>)Expression.Lambda(body, rdParam, indexParam);
			// workaround for mapper issue with complex reader expressions handling
			// https://github.com/linq2db/linq2db/issues/2032
			var compiledReader                = readOracleDecimalToDecimalAdv.Compile();
			_readOracleDecimalToDecimalAdv    = ((Expression<Func<ITestDataReader, int, decimal>>)Expression.Lambda(
				Expression.Invoke(Expression.Constant(compiledReader), rdParam, indexParam),
				rdParam,
				indexParam)).Compile();

			_readOracleDecimalToInt     = ((Expression<Func<ITestDataReader, int, int>>)typeMapper.MapLambda<ITestDataReader, int, int>((rd, i) => (int)(decimal)Wrapped.OracleDecimal.SetPrecision(((Wrapped.OracleDataReader)(object)rd).GetOracleDecimal(i), 27))).Compile();
			_readOracleDecimalToLong    = ((Expression<Func<ITestDataReader, int, long>>)typeMapper.MapLambda<ITestDataReader, int, long>((rd, i) => (long)(decimal)Wrapped.OracleDecimal.SetPrecision(((Wrapped.OracleDataReader)(object)rd).GetOracleDecimal(i), 27))).Compile();
			_readOracleDecimalToDecimal = ((Expression<Func<ITestDataReader, int, decimal>>)typeMapper.MapLambda<ITestDataReader, int, decimal>((rd, i) => (decimal)Wrapped.OracleDecimal.SetPrecision(((Wrapped.OracleDataReader)(object)rd).GetOracleDecimal(i), 27))).Compile();
		}

		[Benchmark]
		public DateTimeOffset TypeMapperReadOracleTimeStampTZ()
		{
			return _readDateTimeOffsetFromOracleTimeStampTZ(DataReaderParameter, IntParameter);
		}

		[Benchmark(Baseline = true)]
		public DateTimeOffset DirectAccessReadOracleTimeStampTZ()
		{
			var tstz = ((Original.OracleDataReader)DataReaderParameter).GetOracleTimeStampTZ(IntParameter);

			return new DateTimeOffset(
				tstz.Year, tstz.Month, tstz.Day,
				tstz.Hour, tstz.Minute, tstz.Second,
				tstz.GetTimeZoneOffset())
				.AddTicks(tstz.Nanosecond / NanosecondsPerTick);
		}

		[Benchmark]
		public DateTimeOffset TypeMapperReadOracleTimeStampLTZ()
		{
			return _readDateTimeOffsetFromOracleTimeStampLTZ(DataReaderParameter, IntParameter);
		}

		[Benchmark]
		public DateTimeOffset DirectAccessReadOracleTimeStampLTZ()
		{
			var tstz = ((Original.OracleDataReader)DataReaderParameter).GetOracleTimeStampLTZ(IntParameter).ToOracleTimeStampTZ();

			return new DateTimeOffset(
				tstz.Year, tstz.Month, tstz.Day,
				tstz.Hour, tstz.Minute, tstz.Second,
				tstz.GetTimeZoneOffset()).AddTicks(tstz.Nanosecond / NanosecondsPerTick);
		}

		[Benchmark]
		public decimal TypeMapperReadOracleDecimalAdv()
		{
			return _readOracleDecimalToDecimalAdv(DataReaderParameter, IntParameter);
		}

		[Benchmark]
		public decimal DirectAccessReadOracleDecimalAdv()
		{
			var odec = ((Original.OracleDataReader)DataReaderParameter).GetOracleDecimal(IntParameter);
			var precision = 29;

			while (true)
			{
				try
				{
					return (decimal)Original.OracleDecimal.SetPrecision(odec, precision);
				}
				catch (OverflowException)
				{
					precision--;
					if (precision <= 26)
					{
						throw;
					}
				}
			}
		}

		[Benchmark]
		public decimal TypeMapperReadOracleDecimalAsDecimal()
		{
			return _readOracleDecimalToDecimal(DataReaderParameter, IntParameter);
		}

		[Benchmark]
		public decimal DirectAccessReadOracleDecimalAsDecimal()
		{
			return (decimal)Original.OracleDecimal.SetPrecision(
				((Original.OracleDataReader)DataReaderParameter).GetOracleDecimal(IntParameter),
				27);
		}

		[Benchmark]
		public int TypeMapperReadOracleDecimalAsInt()
		{
			return _readOracleDecimalToInt(DataReaderParameter, IntParameter);
		}

		[Benchmark]
		public int DirectAccessReadOracleDecimalAsInt()
		{
			return (int)(decimal)Original.OracleDecimal.SetPrecision(
				((Original.OracleDataReader)DataReaderParameter).GetOracleDecimal(IntParameter),
				27);
		}

		[Benchmark]
		public long TypeMapperReadOracleDecimalAsLong()
		{
			return _readOracleDecimalToLong(DataReaderParameter, IntParameter);
		}

		[Benchmark]
		public long DirectAccessReadOracleDecimalAsLong()
		{
			return (long)(decimal)Original.OracleDecimal.SetPrecision(
				((Original.OracleDataReader)DataReaderParameter).GetOracleDecimal(IntParameter),
				27);
		}
	}
}
