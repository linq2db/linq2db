using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using System.Text;

using LinqToDB.Common;
using LinqToDB.Internal.Expressions.Types;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.DuckDB
{
	public sealed class DuckDBProviderAdapter : IDynamicProviderAdapter
	{
		public const string AssemblyName      = "DuckDB.NET.Data";
		       const string TypesAssemblyName = "DuckDB.NET.Bindings";
		       const string ClientNamespace   = "DuckDB.NET.Data";
		       const string TypesNamespace    = "DuckDB.NET.Native";

		DuckDBProviderAdapter()
		{
			var assembly      = Common.Tools.TryLoadAssembly(AssemblyName, null)
				?? throw new InvalidOperationException($"Cannot load assembly {AssemblyName}.");
			var typesAssembly = Common.Tools.TryLoadAssembly(TypesAssemblyName, null)
				?? throw new InvalidOperationException($"Cannot load assembly {TypesAssemblyName}.");

			ConnectionType  = assembly.GetType($"{ClientNamespace}.DuckDBConnection" , true)!;
			DataReaderType  = assembly.GetType($"{ClientNamespace}.DuckDBDataReader" , true)!;
			ParameterType   = assembly.GetType($"{ClientNamespace}.DuckDBParameter"  , true)!;
			CommandType     = assembly.GetType($"{ClientNamespace}.DuckDBCommand"    , true)!;
			TransactionType = assembly.GetType($"{ClientNamespace}.DuckDBTransaction", true)!;

			var appenderType    = assembly.GetType($"{ClientNamespace}.DuckDBAppender"    , true)!;
			var appenderRowType = assembly.GetType($"{ClientNamespace}.IDuckDBAppenderRow", true)!;

			// provider types is a big mess. In same namespace with same naming schema there are
			// - types for internal mappings (public!)
			// - types, supported only on read
			// - types, supported on read and write (but not always in appender)
			// without any doc sign how to distinguish them.

			// roundtrip types, could be used for read and write
			DuckDBDateOnly  = AddType("DuckDBDateOnly",  DataType.Date);
			DuckDBTimeOnly  = AddType("DuckDBTimeOnly",  DataType.Time);
			// with some help from our side also roundtrip
			DuckDBTimestamp = AddType("DuckDBTimestamp", DataType.DateTime);
			DuckDBInterval  = AddType("DuckDBInterval",  DataType.Interval);

			var typeMapper = new TypeMapper();

			typeMapper.RegisterTypeWrapper<Wrappers.DuckDBConnection>(ConnectionType);

			typeMapper.RegisterTypeWrapper<Wrappers.DuckDBAppender>    (appenderType);
			typeMapper.RegisterTypeWrapper<Wrappers.IDuckDBAppenderRow>(appenderRowType);

			typeMapper.RegisterTypeWrapper<Wrappers.DuckDBDateOnly >(DuckDBDateOnly);
			typeMapper.RegisterTypeWrapper<Wrappers.DuckDBTimeOnly >(DuckDBTimeOnly);
			typeMapper.RegisterTypeWrapper<Wrappers.DuckDBTimestamp>(DuckDBTimestamp);
			typeMapper.RegisterTypeWrapper<Wrappers.DuckDBInterval >(DuckDBInterval);

			typeMapper.FinalizeMappings();

			_connectionFactory = typeMapper.BuildTypedFactory<string, Wrappers.DuckDBConnection, DbConnection>(connectionString => new Wrappers.DuckDBConnection(connectionString));

			// literals support for provider types
			MappingSchema.SetValueToSqlConverter(DuckDBTimeOnly,
				typeMapper.BuildAction<StringBuilder, SqlDataType, DataOptions, object>(
					typeMapper.MapActionLambda((StringBuilder sb, SqlDataType type, DataOptions options, object value) => BuildTimeOnlyLiteral(sb, ((Wrappers.DuckDBTimeOnly)value).Hour, ((Wrappers.DuckDBTimeOnly)value).Min, ((Wrappers.DuckDBTimeOnly)value).Sec, ((Wrappers.DuckDBTimeOnly)value).Microsecond))));

			MappingSchema.SetValueToSqlConverter(DuckDBDateOnly,
				typeMapper.BuildAction<StringBuilder, SqlDataType, DataOptions, object>(
					typeMapper.MapActionLambda((StringBuilder sb, SqlDataType type, DataOptions options, object value) => BuildDateOnlyLiteral(sb, ((Wrappers.DuckDBDateOnly)value).Year, ((Wrappers.DuckDBDateOnly)value).Month, ((Wrappers.DuckDBDateOnly)value).Day))));

			MappingSchema.SetValueToSqlConverter(DuckDBTimestamp,
				typeMapper.BuildAction<StringBuilder, SqlDataType, DataOptions, object>(
					typeMapper.MapActionLambda((StringBuilder sb, SqlDataType type, DataOptions options, object value) => BuildTimeSpanLiteral(sb, type.Type, ((Wrappers.DuckDBTimestamp)value).Date.Year, ((Wrappers.DuckDBTimestamp)value).Date.Month, ((Wrappers.DuckDBTimestamp)value).Date.Day, ((Wrappers.DuckDBTimestamp)value).Time.Hour, ((Wrappers.DuckDBTimestamp)value).Time.Min, ((Wrappers.DuckDBTimestamp)value).Time.Sec, ((Wrappers.DuckDBTimestamp)value).Time.Microsecond))));

			MappingSchema.SetValueToSqlConverter(DuckDBInterval,
				typeMapper.BuildAction<StringBuilder, SqlDataType, DataOptions, object>(
					typeMapper.MapActionLambda((StringBuilder sb, SqlDataType type, DataOptions options, object value) => BuildIntervalLiteral(sb, ((Wrappers.DuckDBInterval)value).Months, ((Wrappers.DuckDBInterval)value).Days, ((Wrappers.DuckDBInterval)value).Micros))));

			var pConnection = Expression.Parameter(typeof(DbConnection));
			var pCatalog    = Expression.Parameter(typeof(string));
			var pSchema     = Expression.Parameter(typeof(string));
			var pTable      = Expression.Parameter(typeof(string));
			CreateAppender  = Expression
					.Lambda<Func<DbConnection, string?, string?, string, Wrappers.DuckDBAppender>>(
						typeMapper.MapExpression((DbConnection conn, string? catalog, string? schema, string table) => typeMapper.Wrap<Wrappers.DuckDBAppender>(((Wrappers.DuckDBConnection)(object)conn).CreateAppender(catalog, schema, table)), pConnection, pCatalog, pSchema, pTable),
						pConnection, pCatalog, pSchema, pTable
					)
					.CompileExpression();

			Type AddType(string typeName, DataType dataType)
			{
				var type = typesAssembly.GetType($"{TypesNamespace}.{typeName}", true)!;
				MappingSchema.AddScalarType(type, new SqlDataType(new DbDataType(type, dataType)));
				return type;
			}
		}

		internal Func<DbConnection, string?, string?, string, Wrappers.DuckDBAppender> CreateAppender { get; }

		private static void BuildTimeOnlyLiteral(StringBuilder sb, byte hour, byte min, byte sec, int microsec)
		{
			sb
				.Append('\'')
				.Append(hour.ToString("00", CultureInfo.InvariantCulture))
				.Append(':')
				.Append(min.ToString("00", CultureInfo.InvariantCulture))
				.Append(':')
				.Append(sec.ToString("00", CultureInfo.InvariantCulture))
				.Append('.')
				.Append(microsec.ToString("000000", CultureInfo.InvariantCulture))
				.Append("'::TIME");
		}

		private static void BuildDateOnlyLiteral(StringBuilder sb, int year, byte month, byte day)
		{
			sb.Append('\'');

			if ((year, month, day) is (5881580, 7, 11))
			{
				sb.Append("infinity");
			}
			else if ((year, month, day) is (-5877641, 6, 24))
			{
				sb.Append("-infinity");
			}
			else
			{
				sb
					.Append(year.ToString(CultureInfo.InvariantCulture))
					.Append('-')
					.Append(month.ToString("00", CultureInfo.InvariantCulture))
					.Append('-')
					.Append(day.ToString("00", CultureInfo.InvariantCulture));
			}

			sb.Append("'::DATE");
		}

		private static void BuildTimeSpanLiteral(StringBuilder sb, DbDataType type, int year, byte month, byte day, byte hour, byte min, byte sec, int microsec)
		{
			sb.Append('\'');

			var typeName = type.Precision switch
			{
				> 6         => "TIMESTAMP_NS",
				null or > 3 => "TIMESTAMP",
				> 0         => "TIMESTAMP_MS",
				0           => "TIMESTAMP_S",
				_           => "TIMESTAMP",
			};

			// TIMESTAMP_NS has own range and infinity values
			if (type.Precision > 6 && DuckDBMappingSchema.IsPositiveInfinityTsNs(year, month, day, hour, min, sec, microsec * 1000))
			{
				sb.Append("infinity");
			}
			else if (type.Precision > 6 && DuckDBMappingSchema.IsNegativeInfinityTsNs(year, month, day, hour, min, sec, microsec * 1000))
			{
				sb.Append("-infinity");
			}
			else if (type.Precision is null or <= 6 && (year, month, day, hour, min, sec, microsec) is (294247, 1, 10, 4, 0, 54, 775807))
			{
				sb.Append("infinity");
			}
			else if (type.Precision is null or <= 6 && (year, month, day, hour, min, sec, microsec) is (-290308, 12, 21, 23, 59, 59, 999999))
			{
				sb.Append("-infinity");
			}
			else
			{
				sb
					.Append(year.ToString(CultureInfo.InvariantCulture))
					.Append('-')
					.Append(month.ToString("00", CultureInfo.InvariantCulture))
					.Append('-')
					.Append(day.ToString("00", CultureInfo.InvariantCulture))
					.Append(' ')
					.Append(hour.ToString("00", CultureInfo.InvariantCulture))
					.Append(':')
					.Append(min.ToString("00", CultureInfo.InvariantCulture))
					.Append(':')
					.Append(sec.ToString("00", CultureInfo.InvariantCulture))
					.Append('.')
					.Append(microsec.ToString("000000", CultureInfo.InvariantCulture));
			}

			sb.Append("'::").Append(typeName);
		}

		private static void BuildIntervalLiteral(StringBuilder sb, int months, int days, ulong microSeconds)
		{
			sb.Append('\'');
			if (months > 0)
			{
				sb
					.Append(months.ToString(CultureInfo.InvariantCulture))
					.Append(months is 1 or -1 ? " month" : " months");
			}

			if (days > 0)
			{
				if (months != 0)
					sb.Append(' ');
				sb
					.Append(days.ToString(CultureInfo.InvariantCulture))
					.Append(days is 1 or -1 ? " day" : " days");
			}

			if (microSeconds > 0)
			{
				if (months != 0 || days != 0)
					sb.Append(' ');
				sb
					.Append(microSeconds.ToString(CultureInfo.InvariantCulture))
					.Append(microSeconds is 1 ? " microsecond" : " microseconds");
			}

			if (months == 0 && days == 0 && microSeconds == 0)
				sb.Append("0 microseconds");

			sb.Append("'::INTERVAL");
		}

		static readonly Lazy<DuckDBProviderAdapter> _lazy = new (() => new ());
		internal static DuckDBProviderAdapter Instance => _lazy.Value;

		#region IDynamicProviderAdapter

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		public MappingSchema MappingSchema { get; } = new();

		public Type DuckDBDateOnly  { get; }
		public Type DuckDBTimeOnly  { get; }
		public Type DuckDBTimestamp { get; }
		public Type DuckDBInterval  { get; }

		readonly Func<string, DbConnection> _connectionFactory;
		public DbConnection CreateConnection(string connectionString) => _connectionFactory(connectionString);

		#endregion

		#region Wrappers
		internal static class Wrappers
		{
			[Wrapper]
			internal struct DuckDBDateOnly
			{
				public int  Year  => throw new NotSupportedException();
				public byte Month => throw new NotSupportedException();
				public byte Day   => throw new NotSupportedException();

				public static DuckDBDateOnly FromDateOnly(DateOnly dateOnly) => throw new NotSupportedException();
			}

			[Wrapper]
			internal struct DuckDBTimeOnly
			{
				public byte Hour        => throw new NotSupportedException();
				public byte Min         => throw new NotSupportedException();
				public byte Sec         => throw new NotSupportedException();
				public int  Microsecond => throw new NotSupportedException();

				public static implicit operator DuckDBTimeOnly(TimeOnly time) => throw new NotSupportedException();
			}

			[Wrapper]
			internal struct DuckDBTimestamp
			{
				public DuckDBDateOnly Date => throw new NotSupportedException();
				public DuckDBTimeOnly Time => throw new NotSupportedException();

				public readonly DateTime ToDateTime() => throw new NotSupportedException();
			}

			[Wrapper]
			internal struct DuckDBInterval
			{
				public int   Months => throw new NotSupportedException();
				public int   Days   => throw new NotSupportedException();
				public ulong Micros => throw new NotSupportedException();

				public static explicit operator TimeSpan(DuckDBInterval interval) => throw new NotSupportedException();
			}

			[Wrapper]
			internal sealed class DuckDBConnection
			{
				public DuckDBConnection(string connectionString) => throw new NotSupportedException();

				[TypeWrapperGenericArgs(0)]
				public DuckDBAppender CreateAppender(string? catalog, string? schema, string table) => throw new NotSupportedException();
			}

			[Wrapper]
			internal sealed class DuckDBAppender : TypeWrapper, IDisposable
			{
				[SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "Used from reflection")]
				private static object[] Wrappers { get; }
					= new object[]
					{
						// [0]: CreateRow
						(Expression<Func<DuckDBAppender, IDuckDBAppenderRow>>)(this_ => this_.CreateRow()),
						// [1]: Dispose
						(Expression<Action<DuckDBAppender>>                  )(this_ => this_.Dispose()),
					};

				public DuckDBAppender(object instance, Delegate[] wrappers) : base(instance, wrappers)
				{
				}

				public IDuckDBAppenderRow CreateRow() => ((Func<DuckDBAppender, IDuckDBAppenderRow>)CompiledWrappers[0])(this);
				public void Dispose() => ((Action<DuckDBAppender>)CompiledWrappers[1])(this);
			}

			[Wrapper]
			internal sealed class IDuckDBAppenderRow : TypeWrapper
			{
				[SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "Used from reflection")]
				private static object[] Wrappers { get; }
					= new object[]
					{
						// [0]: EndRow
						(Expression<Action<IDuckDBAppenderRow>>                  )(this_ => this_.EndRow()),

						// [1]: AppendNullValue
						(Expression<Func<IDuckDBAppenderRow, IDuckDBAppenderRow>>)(this_ => this_.AppendNullValue()),
						// [2]: AppendDefault
						(Expression<Func<IDuckDBAppenderRow, IDuckDBAppenderRow>>)(this_ => this_.AppendDefault()),

						// [3-21]: AppendValue
						(Expression<Func<IDuckDBAppenderRow, bool?,           IDuckDBAppenderRow>>)((this_, value) => this_.AppendValue(value)),
						(Expression<Func<IDuckDBAppenderRow, byte[]?,         IDuckDBAppenderRow>>)((this_, value) => this_.AppendValue(value)),
						(Expression<Func<IDuckDBAppenderRow, string?,         IDuckDBAppenderRow>>)((this_, value) => this_.AppendValue(value)),
						(Expression<Func<IDuckDBAppenderRow, decimal?,        IDuckDBAppenderRow>>)((this_, value) => this_.AppendValue(value)),
						(Expression<Func<IDuckDBAppenderRow, Guid?,           IDuckDBAppenderRow>>)((this_, value) => this_.AppendValue(value)),
						(Expression<Func<IDuckDBAppenderRow, BigInteger?,     IDuckDBAppenderRow>>)((this_, value) => this_.AppendValue(value)),
						(Expression<Func<IDuckDBAppenderRow, sbyte?,          IDuckDBAppenderRow>>)((this_, value) => this_.AppendValue(value)),
						(Expression<Func<IDuckDBAppenderRow, short?,          IDuckDBAppenderRow>>)((this_, value) => this_.AppendValue(value)),
						(Expression<Func<IDuckDBAppenderRow, int?,            IDuckDBAppenderRow>>)((this_, value) => this_.AppendValue(value)),
						(Expression<Func<IDuckDBAppenderRow, long?,           IDuckDBAppenderRow>>)((this_, value) => this_.AppendValue(value)),
						(Expression<Func<IDuckDBAppenderRow, byte?,           IDuckDBAppenderRow>>)((this_, value) => this_.AppendValue(value)),
						(Expression<Func<IDuckDBAppenderRow, ushort?,         IDuckDBAppenderRow>>)((this_, value) => this_.AppendValue(value)),
						(Expression<Func<IDuckDBAppenderRow, uint?,           IDuckDBAppenderRow>>)((this_, value) => this_.AppendValue(value)),
						(Expression<Func<IDuckDBAppenderRow, ulong?,          IDuckDBAppenderRow>>)((this_, value) => this_.AppendValue(value)),
						(Expression<Func<IDuckDBAppenderRow, float?,          IDuckDBAppenderRow>>)((this_, value) => this_.AppendValue(value)),
						(Expression<Func<IDuckDBAppenderRow, double?,         IDuckDBAppenderRow>>)((this_, value) => this_.AppendValue(value)),
						(Expression<Func<IDuckDBAppenderRow, DateTime?,       IDuckDBAppenderRow>>)((this_, value) => this_.AppendValue(value)),
						(Expression<Func<IDuckDBAppenderRow, DateTimeOffset?, IDuckDBAppenderRow>>)((this_, value) => this_.AppendValue(value)),
						(Expression<Func<IDuckDBAppenderRow, TimeSpan?,       IDuckDBAppenderRow>>)((this_, value) => this_.AppendValue(value)),

						// [22-25]: AppendValue with mapped type
						(Expression<Func<IDuckDBAppenderRow, object?, IDuckDBAppenderRow>>)((this_, value) => this_.AppendValue((DuckDBDateOnly?)value)),
						(Expression<Func<IDuckDBAppenderRow, object?, IDuckDBAppenderRow>>)((this_, value) => this_.AppendValue((DuckDBTimeOnly?)value)),
						(Expression<Func<IDuckDBAppenderRow, object?, IDuckDBAppenderRow>>)((this_, value) => this_.AppendValue((TimeSpan?)(DuckDBInterval?)value)),
						(Expression<Func<IDuckDBAppenderRow, object,  IDuckDBAppenderRow>>)((this_, value) => this_.AppendValue(((DuckDBTimestamp)value).ToDateTime())),

#if NET8_0_OR_GREATER
						// [26-27]: DateOnly/TimeOnly hacks
						(Expression<Func<IDuckDBAppenderRow, DateOnly, IDuckDBAppenderRow>>)((this_, value) => this_.AppendValue(DuckDBDateOnly.FromDateOnly(value))),
						(Expression<Func<IDuckDBAppenderRow, TimeOnly, IDuckDBAppenderRow>>)((this_, value) => this_.AppendValue((DuckDBTimeOnly)value)),
#endif
					};

				public IDuckDBAppenderRow(object instance, Delegate[] wrappers) : base(instance, wrappers)
				{
				}

				public void EndRow() => ((Action<IDuckDBAppenderRow>)CompiledWrappers[0])(this);

				public IDuckDBAppenderRow AppendNullValue() => ((Func<IDuckDBAppenderRow, IDuckDBAppenderRow>)CompiledWrappers[1])(this);
				public IDuckDBAppenderRow AppendDefault  () => ((Func<IDuckDBAppenderRow, IDuckDBAppenderRow>)CompiledWrappers[2])(this);

				public IDuckDBAppenderRow AppendValue(bool? value)           => ((Func<IDuckDBAppenderRow, bool?          , IDuckDBAppenderRow>)CompiledWrappers[3])(this, value);
				public IDuckDBAppenderRow AppendValue(byte[]? value)         => ((Func<IDuckDBAppenderRow, byte[]?        , IDuckDBAppenderRow>)CompiledWrappers[4])(this, value);
				public IDuckDBAppenderRow AppendValue(string? value)         => ((Func<IDuckDBAppenderRow, string?        , IDuckDBAppenderRow>)CompiledWrappers[5])(this, value);
				public IDuckDBAppenderRow AppendValue(decimal? value)        => ((Func<IDuckDBAppenderRow, decimal?       , IDuckDBAppenderRow>)CompiledWrappers[6])(this, value);
				public IDuckDBAppenderRow AppendValue(Guid? value)           => ((Func<IDuckDBAppenderRow, Guid?          , IDuckDBAppenderRow>)CompiledWrappers[7])(this, value);
				public IDuckDBAppenderRow AppendValue(BigInteger? value)     => ((Func<IDuckDBAppenderRow, BigInteger?    , IDuckDBAppenderRow>)CompiledWrappers[8])(this, value);
				public IDuckDBAppenderRow AppendValue(sbyte? value)          => ((Func<IDuckDBAppenderRow, sbyte?         , IDuckDBAppenderRow>)CompiledWrappers[9])(this, value);
				public IDuckDBAppenderRow AppendValue(short? value)          => ((Func<IDuckDBAppenderRow, short?         , IDuckDBAppenderRow>)CompiledWrappers[10])(this, value);
				public IDuckDBAppenderRow AppendValue(int? value)            => ((Func<IDuckDBAppenderRow, int?           , IDuckDBAppenderRow>)CompiledWrappers[11])(this, value);
				public IDuckDBAppenderRow AppendValue(long? value)           => ((Func<IDuckDBAppenderRow, long?          , IDuckDBAppenderRow>)CompiledWrappers[12])(this, value);
				public IDuckDBAppenderRow AppendValue(byte? value)           => ((Func<IDuckDBAppenderRow, byte?          , IDuckDBAppenderRow>)CompiledWrappers[13])(this, value);
				public IDuckDBAppenderRow AppendValue(ushort? value)         => ((Func<IDuckDBAppenderRow, ushort?        , IDuckDBAppenderRow>)CompiledWrappers[14])(this, value);
				public IDuckDBAppenderRow AppendValue(uint? value)           => ((Func<IDuckDBAppenderRow, uint?          , IDuckDBAppenderRow>)CompiledWrappers[15])(this, value);
				public IDuckDBAppenderRow AppendValue(ulong? value)          => ((Func<IDuckDBAppenderRow, ulong?         , IDuckDBAppenderRow>)CompiledWrappers[16])(this, value);
				public IDuckDBAppenderRow AppendValue(float? value)          => ((Func<IDuckDBAppenderRow, float?         , IDuckDBAppenderRow>)CompiledWrappers[17])(this, value);
				public IDuckDBAppenderRow AppendValue(double? value)         => ((Func<IDuckDBAppenderRow, double?        , IDuckDBAppenderRow>)CompiledWrappers[18])(this, value);
				public IDuckDBAppenderRow AppendValue(DateTime? value)       => ((Func<IDuckDBAppenderRow, DateTime?      , IDuckDBAppenderRow>)CompiledWrappers[19])(this, value);
				public IDuckDBAppenderRow AppendValue(DateTimeOffset? value) => ((Func<IDuckDBAppenderRow, DateTimeOffset?, IDuckDBAppenderRow>)CompiledWrappers[20])(this, value);
				public IDuckDBAppenderRow AppendValue(TimeSpan? value)       => ((Func<IDuckDBAppenderRow, TimeSpan?      , IDuckDBAppenderRow>)CompiledWrappers[21])(this, value);

				public IDuckDBAppenderRow AppendDuckDBDateOnly (object? value) => ((Func<IDuckDBAppenderRow, object?, IDuckDBAppenderRow>)CompiledWrappers[22])(this, value);
				public IDuckDBAppenderRow AppendDuckDBTimeOnly (object? value) => ((Func<IDuckDBAppenderRow, object?, IDuckDBAppenderRow>)CompiledWrappers[23])(this, value);
				public IDuckDBAppenderRow AppendDuckDBInterval (object? value) => ((Func<IDuckDBAppenderRow, object?, IDuckDBAppenderRow>)CompiledWrappers[24])(this, value);
				public IDuckDBAppenderRow AppendDuckDBTimestamp(object  value) => ((Func<IDuckDBAppenderRow, object , IDuckDBAppenderRow>)CompiledWrappers[25])(this, value);

				public IDuckDBAppenderRow AppendValue(DuckDBDateOnly? value) => throw new InvalidOperationException("For mapping only");
				public IDuckDBAppenderRow AppendValue(DuckDBTimeOnly? value) => throw new InvalidOperationException("For mapping only");
#if NET8_0_OR_GREATER
				public IDuckDBAppenderRow AppendValue(DateOnly value) => ((Func<IDuckDBAppenderRow, DateOnly, IDuckDBAppenderRow>)CompiledWrappers[26])(this, value);
				public IDuckDBAppenderRow AppendValue(TimeOnly value) => ((Func<IDuckDBAppenderRow, TimeOnly, IDuckDBAppenderRow>)CompiledWrappers[27])(this, value);
#endif
				// unmapped
				//IDuckDBAppenderRow AppendValue(Span<byte> value)                       => ((Func<IDuckDBAppenderRow, Span<byte>     , IDuckDBAppenderRow>)CompiledWrappers[5])(this, value);
				//IDuckDBAppenderRow AppendValue<TEnum>(TEnum? value) where TEnum : Enum => ((Func<IDuckDBAppenderRow, TEnum?         , IDuckDBAppenderRow>)CompiledWrappers[25])(this, value);
				//IDuckDBAppenderRow AppendValue<T>(IEnumerable<T>? value)               => ((Func<IDuckDBAppenderRow, IEnumerable<T>?, IDuckDBAppenderRow>)CompiledWrappers[26])(this, value);
			}
		}

		#endregion
	}
}
