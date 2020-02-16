``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-ZXOHUL : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-TAKBNN : .NET Core 2.1.15 (CoreCLR 4.6.28325.01, CoreFX 4.6.28327.02), X64 RyuJIT
  Job-WOIQBX : .NET Core 3.1.1 (CoreCLR 4.700.19.60701, CoreFX 4.700.19.60801), X64 RyuJIT

Jit=RyuJit  Platform=X64  MaxIterationCount=10  
MinIterationCount=5  WarmupCount=2  

```
|       Method |       Runtime |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------- |-------------- |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|  BuildAction |    .NET 4.6.2 | 14.001 ns | 1.1781 ns | 0.7793 ns | 12.31 |    1.01 |     - |     - |     - |         - |
| DirectAccess |    .NET 4.6.2 |  1.121 ns | 0.1334 ns | 0.0698 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|  BuildAction | .NET Core 2.1 |  2.507 ns | 0.1596 ns | 0.1056 ns |  2.22 |    0.16 |     - |     - |     - |         - |
| DirectAccess | .NET Core 2.1 |  1.263 ns | 0.1090 ns | 0.0721 ns |  1.16 |    0.10 |     - |     - |     - |         - |
|  BuildAction | .NET Core 3.1 |  1.883 ns | 0.1341 ns | 0.0887 ns |  1.67 |    0.14 |     - |     - |     - |         - |
| DirectAccess | .NET Core 3.1 |  1.336 ns | 0.1552 ns | 0.1027 ns |  1.20 |    0.15 |     - |     - |     - |         - |
