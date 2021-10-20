``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417994 Hz, Resolution=292.5693 ns, Timer=TSC
.NET SDK=5.0.402
  [Host]     : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-QARMCC : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-GQFZGN : .NET Core 3.1.20 (CoreCLR 4.700.21.47003, CoreFX 4.700.21.47101), X64 RyuJIT
  Job-VPXILQ : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                        Method |              Runtime |       Mean |     Median | Ratio | Allocated |
|------------------------------ |--------------------- |-----------:|-----------:|------:|----------:|
|               Update_Nullable |             .NET 5.0 |   274.8 μs |   271.7 μs |  1.40 |     59 KB |
|          Update_Nullable_Full |             .NET 5.0 |   670.1 μs |   664.8 μs |  3.56 |    209 KB |
|      Compiled_Update_Nullable |             .NET 5.0 |   263.5 μs |   251.9 μs |  1.33 |     29 KB |
| Compiled_Update_Nullable_Full |             .NET 5.0 |   500.8 μs |   501.2 μs |  2.56 |    179 KB |
|                        Update |             .NET 5.0 |   241.0 μs |   240.4 μs |  1.29 |     54 KB |
|                   Update_Full |             .NET 5.0 |   660.4 μs |   654.8 μs |  3.36 |    214 KB |
|               Compiled_Update |             .NET 5.0 |   103.7 μs |   101.5 μs |  0.53 |     29 KB |
|          Compiled_Update_Full |             .NET 5.0 |   519.2 μs |   504.8 μs |  2.61 |    188 KB |
|               Update_Nullable |        .NET Core 3.1 |   325.8 μs |   324.0 μs |  1.65 |     60 KB |
|          Update_Nullable_Full |        .NET Core 3.1 |   850.7 μs |   850.6 μs |  4.42 |    215 KB |
|      Compiled_Update_Nullable |        .NET Core 3.1 |   116.9 μs |   116.1 μs |  0.60 |     29 KB |
| Compiled_Update_Nullable_Full |        .NET Core 3.1 |   630.1 μs |   624.4 μs |  3.37 |    186 KB |
|                        Update |        .NET Core 3.1 |   272.0 μs |   267.1 μs |  1.38 |     53 KB |
|                   Update_Full |        .NET Core 3.1 |   804.7 μs |   795.1 μs |  4.21 |    211 KB |
|               Compiled_Update |        .NET Core 3.1 |   109.9 μs |   108.6 μs |  0.57 |     29 KB |
|          Compiled_Update_Full |        .NET Core 3.1 |   629.6 μs |   627.5 μs |  3.34 |    185 KB |
|               Update_Nullable | .NET Framework 4.7.2 |   647.8 μs |   598.6 μs |  3.29 |     72 KB |
|          Update_Nullable_Full | .NET Framework 4.7.2 | 2,316.8 μs | 2,138.4 μs | 11.75 |    312 KB |
|      Compiled_Update_Nullable | .NET Framework 4.7.2 |   283.0 μs |   235.8 μs |  1.43 |     32 KB |
| Compiled_Update_Nullable_Full | .NET Framework 4.7.2 | 1,957.3 μs | 1,851.1 μs |  9.86 |    272 KB |
|                        Update | .NET Framework 4.7.2 |   304.1 μs |   299.8 μs |  1.59 |     58 KB |
|                   Update_Full | .NET Framework 4.7.2 | 2,267.6 μs | 2,069.2 μs | 11.40 |    304 KB |
|               Compiled_Update | .NET Framework 4.7.2 |   217.8 μs |   199.7 μs |  1.00 |     32 KB |
|          Compiled_Update_Full | .NET Framework 4.7.2 |   951.3 μs |   950.9 μs |  5.06 |    245 KB |
