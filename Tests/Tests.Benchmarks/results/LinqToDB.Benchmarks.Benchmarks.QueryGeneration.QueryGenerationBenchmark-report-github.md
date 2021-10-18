``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417994 Hz, Resolution=292.5693 ns, Timer=TSC
.NET SDK=5.0.402
  [Host]     : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-VSZIFU : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                    Method |              Runtime | DataProvider |       Mean |     Median | Ratio | Allocated |
|-------------------------- |--------------------- |------------- |-----------:|-----------:|------:|----------:|
|             **VwSalesByYear** |             **.NET 5.0** |       **Access** |   **420.5 μs** |   **417.2 μs** |     **?** | **103,541 B** |
|             VwSalesByYear |        .NET Core 3.1 |       Access |         NA |         NA |     ? |         - |
|             VwSalesByYear | .NET Framework 4.7.2 |       Access |         NA |         NA |     ? |         - |
|                           |                      |              |            |            |       |           |
|     VwSalesByYearMutation |             .NET 5.0 |       Access |   723.1 μs |   714.4 μs |     ? | 162,104 B |
|     VwSalesByYearMutation |        .NET Core 3.1 |       Access |         NA |         NA |     ? |         - |
|     VwSalesByYearMutation | .NET Framework 4.7.2 |       Access |         NA |         NA |     ? |         - |
|                           |                      |              |            |            |       |           |
| VwSalesByCategoryContains |             .NET 5.0 |       Access | 3,481.0 μs | 3,230.0 μs |     ? | 391,440 B |
| VwSalesByCategoryContains |        .NET Core 3.1 |       Access |         NA |         NA |     ? |         - |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |       Access |         NA |         NA |     ? |         - |
|                           |                      |              |            |            |       |           |
|             **VwSalesByYear** |             **.NET 5.0** |     **Firebird** |   **429.5 μs** |   **427.5 μs** |     **?** | **104,166 B** |
|             VwSalesByYear |        .NET Core 3.1 |     Firebird |         NA |         NA |     ? |         - |
|             VwSalesByYear | .NET Framework 4.7.2 |     Firebird |         NA |         NA |     ? |         - |
|                           |                      |              |            |            |       |           |
|     VwSalesByYearMutation |             .NET 5.0 |     Firebird |   706.9 μs |   704.8 μs |     ? | 165,326 B |
|     VwSalesByYearMutation |        .NET Core 3.1 |     Firebird |         NA |         NA |     ? |         - |
|     VwSalesByYearMutation | .NET Framework 4.7.2 |     Firebird |         NA |         NA |     ? |         - |
|                           |                      |              |            |            |       |           |
| VwSalesByCategoryContains |             .NET 5.0 |     Firebird | 1,109.7 μs | 1,110.4 μs |     ? | 244,324 B |
| VwSalesByCategoryContains |        .NET Core 3.1 |     Firebird |         NA |         NA |     ? |         - |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |     Firebird |         NA |         NA |     ? |         - |

Benchmarks with issues:
  QueryGenerationBenchmark.VwSalesByYear: Job-PNRPSQ(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1) [DataProvider=Access]
  QueryGenerationBenchmark.VwSalesByYear: Job-APHWVV(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2) [DataProvider=Access]
  QueryGenerationBenchmark.VwSalesByYearMutation: Job-PNRPSQ(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1) [DataProvider=Access]
  QueryGenerationBenchmark.VwSalesByYearMutation: Job-APHWVV(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2) [DataProvider=Access]
  QueryGenerationBenchmark.VwSalesByCategoryContains: Job-PNRPSQ(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1) [DataProvider=Access]
  QueryGenerationBenchmark.VwSalesByCategoryContains: Job-APHWVV(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2) [DataProvider=Access]
  QueryGenerationBenchmark.VwSalesByYear: Job-PNRPSQ(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1) [DataProvider=Firebird]
  QueryGenerationBenchmark.VwSalesByYear: Job-APHWVV(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2) [DataProvider=Firebird]
  QueryGenerationBenchmark.VwSalesByYearMutation: Job-PNRPSQ(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1) [DataProvider=Firebird]
  QueryGenerationBenchmark.VwSalesByYearMutation: Job-APHWVV(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2) [DataProvider=Firebird]
  QueryGenerationBenchmark.VwSalesByCategoryContains: Job-PNRPSQ(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1) [DataProvider=Firebird]
  QueryGenerationBenchmark.VwSalesByCategoryContains: Job-APHWVV(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2) [DataProvider=Firebird]
