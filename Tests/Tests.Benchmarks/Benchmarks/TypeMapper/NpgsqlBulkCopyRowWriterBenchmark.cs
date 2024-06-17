using System;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Mapping;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// benchmark shows that mapper could even have better performance (due to unwrapped loop?)
	public class NpgsqlBulkCopyRowWriterBenchmark
	{
		private static readonly MappingSchema MappingSchema = new MappingSchema();

		private Wrapped.NpgsqlBinaryImporter           _wrappedImporter = null!;
		private readonly Original.NpgsqlBinaryImporter _originalImporter = new Original.NpgsqlBinaryImporter();

		private ColumnDescriptor[] _columns = null!;

		[Table]
		public class TestEntity
		{
			[Column] public int     Column1 { get; set; }
			[Column] public string? Column2 { get; set; }
			[Column] public int?    Column3 { get; set; }

			public static TestEntity Instance { get; } = new TestEntity();
		}

		private Action<MappingSchema, Wrapped.NpgsqlBinaryImporter, ColumnDescriptor[], TestEntity> _rowWriter = null!;

		sealed class Original
		{
			public sealed class NpgsqlBinaryImporter
			{
				[MethodImpl(MethodImplOptions.NoInlining)]
				public void StartRow()
				{
				}

				[MethodImpl(MethodImplOptions.NoInlining)]
				public void Write<T>(T value, NpgsqlDbType npgsqlDbType) { }
			}

			public enum NpgsqlDbType
			{
				Test
			}
		}

		sealed class Wrapped
		{
			[Wrapper]
			public sealed class NpgsqlBinaryImporter : TypeWrapper
			{
				private static LambdaExpression[] Wrappers { get; }
					= new LambdaExpression[]
				{
					// [0]: StartRow
					(Expression<Action<NpgsqlBinaryImporter>>)((NpgsqlBinaryImporter this_) => this_.StartRow()),
				};

				public NpgsqlBinaryImporter(object instance, Delegate[] wrappers) : base(instance, wrappers)
				{
				}

				public void StartRow() => ((Action<NpgsqlBinaryImporter>)CompiledWrappers[0])(this);
			}

			[Wrapper]
			public enum NpgsqlDbType
			{
				Test
			}
		}

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper = new TypeMapper();

			typeMapper.RegisterTypeWrapper<Wrapped.NpgsqlBinaryImporter>(typeof(Original.NpgsqlBinaryImporter));
			typeMapper.RegisterTypeWrapper<Wrapped.NpgsqlDbType>(typeof(Original.NpgsqlDbType));

			typeMapper.FinalizeMappings();

			_wrappedImporter = typeMapper.Wrap<Wrapped.NpgsqlBinaryImporter>(_originalImporter);

			var ed        = MappingSchema.GetEntityDescriptor(typeof(TestEntity));
			_columns      = ed.Columns.ToArray();

			var generator = new ExpressionGenerator(typeMapper);

			var pMapping  = Expression.Parameter(typeof(MappingSchema));
			var pWriterIn = Expression.Parameter(typeof(Wrapped.NpgsqlBinaryImporter));
			var pColumns  = Expression.Parameter(typeof(ColumnDescriptor[]));
			var pEntity   = Expression.Parameter(typeof(TestEntity));
			var pWriter   = generator.AddVariable(Expression.Parameter(typeof(Original.NpgsqlBinaryImporter)));

			generator.Assign(pWriter, Expression.Convert(Expression.PropertyOrField(pWriterIn, "instance_"), typeof(Original.NpgsqlBinaryImporter)));
			generator.AddExpression(generator.MapAction((Wrapped.NpgsqlBinaryImporter importer) => importer.StartRow(), pWriter));

			for (var i = 0; i < _columns.Length; i++)
			{
				generator.AddExpression(
					Expression.Call(
						pWriter,
						"Write",
						new[] { typeof(object) },
						Expression.Call(Expression.ArrayIndex(pColumns, Expression.Constant(i)), "GetProviderValue", [], pEntity),
						Expression.Convert(Expression.Constant(Wrapped.NpgsqlDbType.Test), typeof(Original.NpgsqlDbType))));
			}

			_rowWriter = Expression.Lambda<Action<MappingSchema, Wrapped.NpgsqlBinaryImporter, ColumnDescriptor[], TestEntity>>(
					generator.Build(),
					pMapping, pWriterIn, pColumns, pEntity)
				.Compile();
		}

		[Benchmark]
		public void TypeMapper()
		{
			_rowWriter(MappingSchema, _wrappedImporter, _columns, TestEntity.Instance);
		}

		[Benchmark(Baseline = true)]
		public void DirectAccess()
		{
			var importer = _originalImporter;
			importer.StartRow();

			for (var i = 0; i < _columns.Length; i++)
				importer.Write(_columns[i].GetProviderValue(TestEntity.Instance), Original.NpgsqlDbType.Test);
		}
	}
}
