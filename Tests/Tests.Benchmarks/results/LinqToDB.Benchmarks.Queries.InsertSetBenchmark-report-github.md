``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host] : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2

Jit=RyuJit  Platform=X64  

```
| Method |              Runtime | Mean | Ratio | Alloc Ratio |
|------- |--------------------- |-----:|------:|------------:|
|   Test |             .NET 6.0 |   NA |     ? |           ? |
|   Test |             .NET 7.0 |   NA |     ? |           ? |
|   Test |        .NET Core 3.1 |   NA |     ? |           ? |
|   Test | .NET Framework 4.7.2 |   NA |     ? |           ? |

Benchmarks with issues:
  InsertSetBenchmark.Test: Job-WUZRIO(Jit=RyuJit, Platform=X64, Runtime=.NET 6.0)
  InsertSetBenchmark.Test: Job-EMBONI(Jit=RyuJit, Platform=X64, Runtime=.NET 7.0)
  InsertSetBenchmark.Test: Job-HZWTXS(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1)
  InsertSetBenchmark.Test: Job-VIGHHX(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2)
