``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417994 Hz, Resolution=292.5693 ns, Timer=TSC
.NET SDK=5.0.402
  [Host]     : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-GDYLNW : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                    Method |              Runtime | DataProvider |       Mean |     Median | Ratio | Allocated |
|-------------------------- |--------------------- |------------- |-----------:|-----------:|------:|----------:|
|             **VwSalesByYear** |             **.NET 5.0** |       **Access** |   **429.2 μs** |   **427.1 μs** |     **?** | **106,472 B** |
|             VwSalesByYear |        .NET Core 3.1 |       Access |         NA |         NA |     ? |         - |
|             VwSalesByYear | .NET Framework 4.7.2 |       Access |         NA |         NA |     ? |         - |
|                           |                      |              |            |            |       |           |
|     VwSalesByYearMutation |             .NET 5.0 |       Access |   698.8 μs |   693.2 μs |     ? | 188,086 B |
|     VwSalesByYearMutation |        .NET Core 3.1 |       Access |         NA |         NA |     ? |         - |
|     VwSalesByYearMutation | .NET Framework 4.7.2 |       Access |         NA |         NA |     ? |         - |
|                           |                      |              |            |            |       |           |
| VwSalesByCategoryContains |             .NET 5.0 |       Access | 2,067.1 μs | 2,030.2 μs |     ? | 450,123 B |
| VwSalesByCategoryContains |        .NET Core 3.1 |       Access |         NA |         NA |     ? |         - |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |       Access |         NA |         NA |     ? |         - |
|                           |                      |              |            |            |       |           |
|             **VwSalesByYear** |             **.NET 5.0** |     **Firebird** |   **406.1 μs** |   **405.2 μs** |     **?** | **107,095 B** |
|             VwSalesByYear |        .NET Core 3.1 |     Firebird |         NA |         NA |     ? |         - |
|             VwSalesByYear | .NET Framework 4.7.2 |     Firebird |         NA |         NA |     ? |         - |
|                           |                      |              |            |            |       |           |
|     VwSalesByYearMutation |             .NET 5.0 |     Firebird |   707.4 μs |   697.7 μs |     ? | 191,350 B |
|     VwSalesByYearMutation |        .NET Core 3.1 |     Firebird |         NA |         NA |     ? |         - |
|     VwSalesByYearMutation | .NET Framework 4.7.2 |     Firebird |         NA |         NA |     ? |         - |
|                           |                      |              |            |            |       |           |
| VwSalesByCategoryContains |             .NET 5.0 |     Firebird | 1,184.5 μs | 1,180.8 μs |     ? | 283,957 B |
| VwSalesByCategoryContains |        .NET Core 3.1 |     Firebird |         NA |         NA |     ? |         - |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |     Firebird |         NA |         NA |     ? |         - |

Benchmarks with issues:
  QueryGenerationBenchmark.VwSalesByYear: Job-DHBZYJ(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1) [DataProvider=Access]
  QueryGenerationBenchmark.VwSalesByYear: Job-DBARKL(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2) [DataProvider=Access]
  QueryGenerationBenchmark.VwSalesByYearMutation: Job-DHBZYJ(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1) [DataProvider=Access]
  QueryGenerationBenchmark.VwSalesByYearMutation: Job-DBARKL(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2) [DataProvider=Access]
  QueryGenerationBenchmark.VwSalesByCategoryContains: Job-DHBZYJ(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1) [DataProvider=Access]
  QueryGenerationBenchmark.VwSalesByCategoryContains: Job-DBARKL(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2) [DataProvider=Access]
  QueryGenerationBenchmark.VwSalesByYear: Job-DHBZYJ(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1) [DataProvider=Firebird]
  QueryGenerationBenchmark.VwSalesByYear: Job-DBARKL(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2) [DataProvider=Firebird]
  QueryGenerationBenchmark.VwSalesByYearMutation: Job-DHBZYJ(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1) [DataProvider=Firebird]
  QueryGenerationBenchmark.VwSalesByYearMutation: Job-DBARKL(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2) [DataProvider=Firebird]
  QueryGenerationBenchmark.VwSalesByCategoryContains: Job-DHBZYJ(Jit=RyuJit, Platform=X64, Runtime=.NET Core 3.1) [DataProvider=Firebird]
  QueryGenerationBenchmark.VwSalesByCategoryContains: Job-DBARKL(Jit=RyuJit, Platform=X64, Runtime=.NET Framework 4.7.2) [DataProvider=Firebird]
