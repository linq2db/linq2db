``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-FSMYUH : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-TSQXSD : .NET Core 2.1.15 (CoreCLR 4.6.28325.01, CoreFX 4.6.28327.02), X64 RyuJIT
  Job-OUTKHJ : .NET Core 3.1.1 (CoreCLR 4.700.19.60701, CoreFX 4.700.19.60801), X64 RyuJIT

Jit=RyuJit  Platform=X64  MaxIterationCount=5  
MinIterationCount=3  WarmupCount=2  

```
|                  Method |       Runtime |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------ |-------------- |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|        TypeMapperAsEnum |    .NET 4.6.2 | 23.929 ns | 3.4020 ns | 0.8835 ns | 22.91 |    0.68 |     - |     - |     - |         - |
|      DirectAccessAsEnum |    .NET 4.6.2 |  1.057 ns | 0.0265 ns | 0.0041 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|   TypeMapperAsKnownEnum |    .NET 4.6.2 |  9.408 ns | 0.7824 ns | 0.2032 ns |  8.94 |    0.17 |     - |     - |     - |         - |
| DirectAccessAsKnownEnum |    .NET 4.6.2 |  1.079 ns | 0.0598 ns | 0.0155 ns |  1.02 |    0.01 |     - |     - |     - |         - |
|      TypeMapperAsString |    .NET 4.6.2 | 10.597 ns | 0.2722 ns | 0.0707 ns | 10.03 |    0.06 |     - |     - |     - |         - |
|    DirectAccessAsString |    .NET 4.6.2 |  3.256 ns | 0.0607 ns | 0.0158 ns |  3.08 |    0.02 |     - |     - |     - |         - |
|        TypeMapperAsEnum | .NET Core 2.1 | 14.441 ns | 1.5617 ns | 0.4056 ns | 13.70 |    0.43 |     - |     - |     - |         - |
|      DirectAccessAsEnum | .NET Core 2.1 |  1.064 ns | 0.0085 ns | 0.0005 ns |  1.01 |    0.00 |     - |     - |     - |         - |
|   TypeMapperAsKnownEnum | .NET Core 2.1 |  2.466 ns | 0.0914 ns | 0.0237 ns |  2.34 |    0.03 |     - |     - |     - |         - |
| DirectAccessAsKnownEnum | .NET Core 2.1 |  1.063 ns | 0.0280 ns | 0.0043 ns |  1.01 |    0.00 |     - |     - |     - |         - |
|      TypeMapperAsString | .NET Core 2.1 |  5.963 ns | 0.1600 ns | 0.0415 ns |  5.65 |    0.06 |     - |     - |     - |         - |
|    DirectAccessAsString | .NET Core 2.1 |  4.629 ns | 0.0760 ns | 0.0118 ns |  4.38 |    0.03 |     - |     - |     - |         - |
|        TypeMapperAsEnum | .NET Core 3.1 | 13.412 ns | 0.2834 ns | 0.0736 ns | 12.70 |    0.04 |     - |     - |     - |         - |
|      DirectAccessAsEnum | .NET Core 3.1 |  1.021 ns | 0.0390 ns | 0.0101 ns |  0.97 |    0.01 |     - |     - |     - |         - |
|   TypeMapperAsKnownEnum | .NET Core 3.1 |  2.746 ns | 0.0918 ns | 0.0238 ns |  2.60 |    0.03 |     - |     - |     - |         - |
| DirectAccessAsKnownEnum | .NET Core 3.1 |  1.090 ns | 0.0345 ns | 0.0090 ns |  1.03 |    0.01 |     - |     - |     - |         - |
|      TypeMapperAsString | .NET Core 3.1 |  4.600 ns | 0.0860 ns | 0.0223 ns |  4.36 |    0.04 |     - |     - |     - |         - |
|    DirectAccessAsString | .NET Core 3.1 |  2.973 ns | 0.0571 ns | 0.0088 ns |  2.81 |    0.01 |     - |     - |     - |         - |
