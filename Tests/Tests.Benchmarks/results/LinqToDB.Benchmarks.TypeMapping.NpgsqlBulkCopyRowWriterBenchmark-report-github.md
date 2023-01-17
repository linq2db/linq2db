``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WUZRIO : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-EMBONI : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-HZWTXS : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-VIGHHX : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|       Method |              Runtime | Mean | Ratio | Alloc Ratio |
|------------- |--------------------- |-----:|------:|------------:|
|   TypeMapper |             .NET 6.0 |   NA |     ? |           ? |
| DirectAccess |             .NET 6.0 |   NA |     ? |           ? |
|   TypeMapper |             .NET 7.0 |   NA |     ? |           ? |
| DirectAccess |             .NET 7.0 |   NA |     ? |           ? |
|   TypeMapper |        .NET Core 3.1 |   NA |     ? |           ? |
| DirectAccess |        .NET Core 3.1 |   NA |     ? |           ? |
|   TypeMapper | .NET Framework 4.7.2 |   NA |     ? |           ? |
| DirectAccess | .NET Framework 4.7.2 |   NA |     ? |           ? |

Benchmarks with issues:
  NpgsqlBulkCopyRowWriterBenchmark.TypeMapper: Job-WUZRIO(Jit=RyuJit, Platform=X64, Runtime=.NET 6.0)
  NpgsqlBulkCopyRowWriterBenchmark.DirectAccess: Job-WUZRIO(Jit=RyuJit, Platform=X64, Runtime=.NET 6.0)
  NpgsqlBulkCopyRowWriterBenchmark.TypeMapper: Job-EMBONI(Jit=RyuJit, Platform=X64, Runtime=.NET 7.0)
  NpgsqlBulkCopyRowWriterBenchmark.DirectAccess: Job-EMBONI(Jit=RyuJit, Platform=X64, Runtime=.NET 7.0)
  NpgsqlBulkCopyRowWriterBenchmark.TypeMapper: Job-HZWTXS(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1)
  NpgsqlBulkCopyRowWriterBenchmark.DirectAccess: Job-HZWTXS(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1)
  NpgsqlBulkCopyRowWriterBenchmark.TypeMapper: Job-VIGHHX(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2)
  NpgsqlBulkCopyRowWriterBenchmark.DirectAccess: Job-VIGHHX(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2)
