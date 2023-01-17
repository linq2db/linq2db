``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host] : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2

Jit=RyuJit  Platform=X64  

```
|    Method |              Runtime | Mean | Ratio | Alloc Ratio |
|---------- |--------------------- |-----:|------:|------------:|
|      Linq |             .NET 6.0 |   NA |     ? |           ? |
|  Compiled |             .NET 6.0 |   NA |     ? |           ? |
| RawAdoNet |             .NET 6.0 |   NA |     ? |           ? |
|      Linq |             .NET 7.0 |   NA |     ? |           ? |
|  Compiled |             .NET 7.0 |   NA |     ? |           ? |
| RawAdoNet |             .NET 7.0 |   NA |     ? |           ? |
|      Linq |        .NET Core 3.1 |   NA |     ? |           ? |
|  Compiled |        .NET Core 3.1 |   NA |     ? |           ? |
| RawAdoNet |        .NET Core 3.1 |   NA |     ? |           ? |
|      Linq | .NET Framework 4.7.2 |   NA |     ? |           ? |
|  Compiled | .NET Framework 4.7.2 |   NA |     ? |           ? |
| RawAdoNet | .NET Framework 4.7.2 |   NA |     ? |           ? |

Benchmarks with issues:
  FetchSetBenchmark.Linq: Job-WUZRIO(Jit=RyuJit, Platform=X64, Runtime=.NET 6.0)
  FetchSetBenchmark.Compiled: Job-WUZRIO(Jit=RyuJit, Platform=X64, Runtime=.NET 6.0)
  FetchSetBenchmark.RawAdoNet: Job-WUZRIO(Jit=RyuJit, Platform=X64, Runtime=.NET 6.0)
  FetchSetBenchmark.Linq: Job-EMBONI(Jit=RyuJit, Platform=X64, Runtime=.NET 7.0)
  FetchSetBenchmark.Compiled: Job-EMBONI(Jit=RyuJit, Platform=X64, Runtime=.NET 7.0)
  FetchSetBenchmark.RawAdoNet: Job-EMBONI(Jit=RyuJit, Platform=X64, Runtime=.NET 7.0)
  FetchSetBenchmark.Linq: Job-HZWTXS(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1)
  FetchSetBenchmark.Compiled: Job-HZWTXS(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1)
  FetchSetBenchmark.RawAdoNet: Job-HZWTXS(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1)
  FetchSetBenchmark.Linq: Job-VIGHHX(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2)
  FetchSetBenchmark.Compiled: Job-VIGHHX(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2)
  FetchSetBenchmark.RawAdoNet: Job-VIGHHX(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2)
