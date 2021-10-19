``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417994 Hz, Resolution=292.5693 ns, Timer=TSC
.NET SDK=5.0.402
  [Host]     : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-UPKRSS : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                    Method |              Runtime | DataProvider |       Mean | Ratio | Allocated |
|-------------------------- |--------------------- |------------- |-----------:|------:|----------:|
|             **VwSalesByYear** |             **.NET 5.0** |       **Access** |   **299.4 μs** |     **?** |  **71,308 B** |
|             VwSalesByYear |        .NET Core 3.1 |       Access |         NA |     ? |         - |
|             VwSalesByYear | .NET Framework 4.7.2 |       Access |         NA |     ? |         - |
|                           |                      |              |            |       |           |
|     VwSalesByYearMutation |             .NET 5.0 |       Access |   558.4 μs |     ? | 124,403 B |
|     VwSalesByYearMutation |        .NET Core 3.1 |       Access |         NA |     ? |         - |
|     VwSalesByYearMutation | .NET Framework 4.7.2 |       Access |         NA |     ? |         - |
|                           |                      |              |            |       |           |
| VwSalesByCategoryContains |             .NET 5.0 |       Access | 1,721.7 μs |     ? | 333,612 B |
| VwSalesByCategoryContains |        .NET Core 3.1 |       Access |         NA |     ? |         - |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |       Access |         NA |     ? |         - |
|                           |                      |              |            |       |           |
|             **VwSalesByYear** |             **.NET 5.0** |     **Firebird** |   **305.5 μs** |     **?** |  **71,933 B** |
|             VwSalesByYear |        .NET Core 3.1 |     Firebird |         NA |     ? |         - |
|             VwSalesByYear | .NET Framework 4.7.2 |     Firebird |         NA |     ? |         - |
|                           |                      |              |            |       |           |
|     VwSalesByYearMutation |             .NET 5.0 |     Firebird |   585.1 μs |     ? | 127,274 B |
|     VwSalesByYearMutation |        .NET Core 3.1 |     Firebird |         NA |     ? |         - |
|     VwSalesByYearMutation | .NET Framework 4.7.2 |     Firebird |         NA |     ? |         - |
|                           |                      |              |            |       |           |
| VwSalesByCategoryContains |             .NET 5.0 |     Firebird |   961.7 μs |     ? | 193,318 B |
| VwSalesByCategoryContains |        .NET Core 3.1 |     Firebird |         NA |     ? |         - |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |     Firebird |         NA |     ? |         - |

Benchmarks with issues:
  QueryGenerationBenchmark.VwSalesByYear: Job-WKCEYB(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1) [DataProvider=Access]
  QueryGenerationBenchmark.VwSalesByYear: Job-CQMIKR(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2) [DataProvider=Access]
  QueryGenerationBenchmark.VwSalesByYearMutation: Job-WKCEYB(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1) [DataProvider=Access]
  QueryGenerationBenchmark.VwSalesByYearMutation: Job-CQMIKR(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2) [DataProvider=Access]
  QueryGenerationBenchmark.VwSalesByCategoryContains: Job-WKCEYB(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1) [DataProvider=Access]
  QueryGenerationBenchmark.VwSalesByCategoryContains: Job-CQMIKR(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2) [DataProvider=Access]
  QueryGenerationBenchmark.VwSalesByYear: Job-WKCEYB(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1) [DataProvider=Firebird]
  QueryGenerationBenchmark.VwSalesByYear: Job-CQMIKR(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2) [DataProvider=Firebird]
  QueryGenerationBenchmark.VwSalesByYearMutation: Job-WKCEYB(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1) [DataProvider=Firebird]
  QueryGenerationBenchmark.VwSalesByYearMutation: Job-CQMIKR(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2) [DataProvider=Firebird]
  QueryGenerationBenchmark.VwSalesByCategoryContains: Job-WKCEYB(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1) [DataProvider=Firebird]
  QueryGenerationBenchmark.VwSalesByCategoryContains: Job-CQMIKR(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2) [DataProvider=Firebird]
