``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417994 Hz, Resolution=292.5693 ns, Timer=TSC
.NET SDK=5.0.402
  [Host]     : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-GDLOGG : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                    Method |              Runtime | DataProvider |       Mean |     Median | Ratio | Allocated |
|-------------------------- |--------------------- |------------- |-----------:|-----------:|------:|----------:|
|             **VwSalesByYear** |             **.NET 5.0** |       **Access** |   **411.9 μs** |   **408.9 μs** |     **?** | **103,541 B** |
|             VwSalesByYear |        .NET Core 3.1 |       Access |         NA |         NA |     ? |         - |
|             VwSalesByYear | .NET Framework 4.7.2 |       Access |         NA |         NA |     ? |         - |
|                           |                      |              |            |            |       |           |
|     VwSalesByYearMutation |             .NET 5.0 |       Access |   680.7 μs |   678.1 μs |     ? | 159,249 B |
|     VwSalesByYearMutation |        .NET Core 3.1 |       Access |         NA |         NA |     ? |         - |
|     VwSalesByYearMutation | .NET Framework 4.7.2 |       Access |         NA |         NA |     ? |         - |
|                           |                      |              |            |            |       |           |
| VwSalesByCategoryContains |             .NET 5.0 |       Access | 1,907.1 μs | 1,877.5 μs |     ? | 381,684 B |
| VwSalesByCategoryContains |        .NET Core 3.1 |       Access |         NA |         NA |     ? |         - |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |       Access |         NA |         NA |     ? |         - |
|                           |                      |              |            |            |       |           |
|             **VwSalesByYear** |             **.NET 5.0** |     **Firebird** |   **410.0 μs** |   **410.1 μs** |     **?** | **104,166 B** |
|             VwSalesByYear |        .NET Core 3.1 |     Firebird |         NA |         NA |     ? |         - |
|             VwSalesByYear | .NET Framework 4.7.2 |     Firebird |         NA |         NA |     ? |         - |
|                           |                      |              |            |            |       |           |
|     VwSalesByYearMutation |             .NET 5.0 |     Firebird |   690.1 μs |   688.2 μs |     ? | 162,824 B |
|     VwSalesByYearMutation |        .NET Core 3.1 |     Firebird |         NA |         NA |     ? |         - |
|     VwSalesByYearMutation | .NET Framework 4.7.2 |     Firebird |         NA |         NA |     ? |         - |
|                           |                      |              |            |            |       |           |
| VwSalesByCategoryContains |             .NET 5.0 |     Firebird | 1,113.3 μs | 1,099.6 μs |     ? | 240,996 B |
| VwSalesByCategoryContains |        .NET Core 3.1 |     Firebird |         NA |         NA |     ? |         - |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |     Firebird |         NA |         NA |     ? |         - |

Benchmarks with issues:
  QueryGenerationBenchmark.VwSalesByYear: Job-UMNLDC(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1) [DataProvider=Access]
  QueryGenerationBenchmark.VwSalesByYear: Job-WPXKHN(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2) [DataProvider=Access]
  QueryGenerationBenchmark.VwSalesByYearMutation: Job-UMNLDC(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1) [DataProvider=Access]
  QueryGenerationBenchmark.VwSalesByYearMutation: Job-WPXKHN(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2) [DataProvider=Access]
  QueryGenerationBenchmark.VwSalesByCategoryContains: Job-UMNLDC(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1) [DataProvider=Access]
  QueryGenerationBenchmark.VwSalesByCategoryContains: Job-WPXKHN(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2) [DataProvider=Access]
  QueryGenerationBenchmark.VwSalesByYear: Job-UMNLDC(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1) [DataProvider=Firebird]
  QueryGenerationBenchmark.VwSalesByYear: Job-WPXKHN(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2) [DataProvider=Firebird]
  QueryGenerationBenchmark.VwSalesByYearMutation: Job-UMNLDC(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1) [DataProvider=Firebird]
  QueryGenerationBenchmark.VwSalesByYearMutation: Job-WPXKHN(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2) [DataProvider=Firebird]
  QueryGenerationBenchmark.VwSalesByCategoryContains: Job-UMNLDC(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1) [DataProvider=Firebird]
  QueryGenerationBenchmark.VwSalesByCategoryContains: Job-WPXKHN(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2) [DataProvider=Firebird]
