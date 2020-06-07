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
|       Method |       Runtime |     Mean | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------- |-------------- |---------:|------:|-------:|------:|------:|----------:|
|   TypeMapper |    .NET 4.6.2 | 158.8 ns |  1.13 | 0.0057 |     - |     - |      24 B |
| DirectAccess |    .NET 4.6.2 | 140.8 ns |  1.00 | 0.0057 |     - |     - |      24 B |
|   TypeMapper | .NET Core 2.1 | 105.6 ns |  0.75 | 0.0056 |     - |     - |      24 B |
| DirectAccess | .NET Core 2.1 | 118.1 ns |  0.84 | 0.0055 |     - |     - |      24 B |
|   TypeMapper | .NET Core 3.1 | 107.9 ns |  0.77 | 0.0057 |     - |     - |      24 B |
| DirectAccess | .NET Core 3.1 | 119.2 ns |  0.84 | 0.0057 |     - |     - |      24 B |
