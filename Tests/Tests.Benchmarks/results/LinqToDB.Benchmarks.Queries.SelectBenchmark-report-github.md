``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-FSMYUH : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-TSQXSD : .NET Core 2.1.17 (CoreCLR 4.6.28619.01, CoreFX 4.6.28619.01), X64 RyuJIT
  Job-OUTKHJ : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT

Jit=RyuJit  Platform=X64  MaxIterationCount=5  
MinIterationCount=3  WarmupCount=2  

```
|    Method |       Runtime |         Mean |    Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------- |-------------- |-------------:|---------:|-------:|------:|------:|----------:|
|      Linq |    .NET 4.6.2 | 142,656.7 ns |   692.89 |      - |     - |     - |         - |
|  Compiled |    .NET 4.6.2 |  45,933.4 ns |   226.33 |      - |     - |     - |         - |
|   FromSql |    .NET 4.6.2 |   1,034.1 ns |     5.08 | 0.1163 |     - |     - |     489 B |
| RawAdoNet |    .NET 4.6.2 |     206.1 ns |     1.00 | 0.0439 |     - |     - |     185 B |
|      Linq | .NET Core 2.1 | 139,848.1 ns |   687.13 |      - |     - |     - |         - |
|  Compiled | .NET Core 2.1 |  42,568.8 ns |   208.58 |      - |     - |     - |         - |
|   FromSql | .NET Core 2.1 |     917.9 ns |     4.46 | 0.1040 |     - |     - |     440 B |
| RawAdoNet | .NET Core 2.1 |     132.7 ns |     0.64 | 0.0379 |     - |     - |     160 B |
|      Linq | .NET Core 3.1 | 212,931.8 ns | 1,040.62 |      - |     - |     - |         - |
|  Compiled | .NET Core 3.1 |  43,446.5 ns |   213.28 |      - |     - |     - |         - |
|   FromSql | .NET Core 3.1 |     894.8 ns |     4.34 | 0.1049 |     - |     - |     440 B |
| RawAdoNet | .NET Core 3.1 |     134.4 ns |     0.66 | 0.0381 |     - |     - |     160 B |
