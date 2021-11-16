``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417994 Hz, Resolution=292.5693 ns, Timer=TSC
.NET SDK=5.0.402
  [Host]     : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-ODZCDL : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-PCJJBI : .NET Core 3.1.20 (CoreCLR 4.700.21.47003, CoreFX 4.700.21.47101), X64 RyuJIT
  Job-HHEMGO : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|             Method |              Runtime |         Mean |       Median |    Ratio | Allocated |
|------------------- |--------------------- |-------------:|-------------:|---------:|----------:|
|            LinqSet |             .NET 5.0 | 369,954.5 ns | 369,876.8 ns |   672.70 | 112,283 B |
|         LinqObject |             .NET 5.0 | 220,789.7 ns | 220,525.3 ns |   401.45 |  66,783 B |
|             Object |             .NET 5.0 |  13,729.1 ns |  13,736.8 ns |    24.96 |   8,128 B |
|    CompiledLinqSet |             .NET 5.0 |  11,954.9 ns |  11,935.8 ns |    21.69 |   7,808 B |
| CompiledLinqObject |             .NET 5.0 |  12,171.6 ns |  12,169.4 ns |    22.07 |   7,808 B |
|          RawAdoNet |             .NET 5.0 |     235.4 ns |     235.5 ns |     0.43 |     712 B |
|            LinqSet |        .NET Core 3.1 | 418,539.1 ns | 418,120.0 ns |   759.03 | 112,219 B |
|         LinqObject |        .NET Core 3.1 | 255,002.8 ns | 254,882.0 ns |   463.67 |  67,056 B |
|             Object |        .NET Core 3.1 |  17,317.3 ns |  17,320.0 ns |    31.53 |   8,176 B |
|    CompiledLinqSet |        .NET Core 3.1 |  14,373.2 ns |  14,382.7 ns |    26.07 |   7,776 B |
| CompiledLinqObject |        .NET Core 3.1 |  14,455.0 ns |  14,442.2 ns |    26.35 |   7,776 B |
|          RawAdoNet |        .NET Core 3.1 |     235.5 ns |     232.8 ns |     0.43 |     712 B |
|            LinqSet | .NET Framework 4.7.2 | 713,785.4 ns | 672,616.7 ns | 1,328.07 | 139,264 B |
|         LinqObject | .NET Framework 4.7.2 | 443,429.7 ns | 417,788.9 ns |   839.91 |  81,920 B |
|             Object | .NET Framework 4.7.2 |  20,178.3 ns |  20,143.0 ns |    37.01 |   9,003 B |
|    CompiledLinqSet | .NET Framework 4.7.2 |  45,315.7 ns |  38,326.6 ns |    89.71 |         - |
| CompiledLinqObject | .NET Framework 4.7.2 |  48,680.6 ns |  36,424.9 ns |    98.18 |         - |
|          RawAdoNet | .NET Framework 4.7.2 |     543.6 ns |     537.9 ns |     1.00 |     810 B |
