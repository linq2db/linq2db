``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-RNZPMW : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-XCCWXF : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WSMVMG : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-FMTKFQ : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|              Method |              Runtime |       Mean |     Median | Ratio |   Gen0 | Allocated | Alloc Ratio |
|-------------------- |--------------------- |-----------:|-----------:|------:|-------:|----------:|------------:|
|    TypeMapperString |             .NET 6.0 |  4.0652 ns |  5.4345 ns |  3.55 |      - |         - |          NA |
|  DirectAccessString |             .NET 6.0 |  0.9075 ns |  0.9262 ns |  0.68 |      - |         - |          NA |
|       TypeMapperInt |             .NET 6.0 |  5.1331 ns |  5.0358 ns |  3.86 |      - |         - |          NA |
|     DirectAccessInt |             .NET 6.0 |  0.8925 ns |  0.9743 ns |  0.61 |      - |         - |          NA |
|      TypeMapperLong |             .NET 6.0 |  5.6376 ns |  5.6820 ns |  4.24 |      - |         - |          NA |
|    DirectAccessLong |             .NET 6.0 |  0.8732 ns |  0.9293 ns |  0.67 |      - |         - |          NA |
|   TypeMapperBoolean |             .NET 6.0 |  4.9658 ns |  5.0238 ns |  3.55 |      - |         - |          NA |
| DirectAccessBoolean |             .NET 6.0 |  0.8656 ns |  0.8749 ns |  0.65 |      - |         - |          NA |
|   TypeMapperWrapper |             .NET 6.0 | 13.5050 ns | 13.5305 ns | 10.17 |      - |         - |          NA |
| DirectAccessWrapper |             .NET 6.0 |  0.8803 ns |  0.9149 ns |  0.67 |      - |         - |          NA |
|      TypeMapperEnum |             .NET 6.0 | 28.5670 ns | 28.4038 ns | 21.39 | 0.0014 |      24 B |          NA |
|    DirectAccessEnum |             .NET 6.0 |  0.8951 ns |  0.9117 ns |  0.68 |      - |         - |          NA |
|   TypeMapperVersion |             .NET 6.0 |  4.7458 ns |  5.4910 ns |  2.99 |      - |         - |          NA |
| DirectAccessVersion |             .NET 6.0 |  0.9292 ns |  0.9650 ns |  0.70 |      - |         - |          NA |
|    TypeMapperString |             .NET 7.0 |  5.1416 ns |  5.1584 ns |  3.87 |      - |         - |          NA |
|  DirectAccessString |             .NET 7.0 |  0.5389 ns |  0.4624 ns |  0.40 |      - |         - |          NA |
|       TypeMapperInt |             .NET 7.0 |  5.1832 ns |  5.1538 ns |  3.90 |      - |         - |          NA |
|     DirectAccessInt |             .NET 7.0 |  0.4881 ns |  0.4745 ns |  0.37 |      - |         - |          NA |
|      TypeMapperLong |             .NET 7.0 |  5.2597 ns |  5.1967 ns |  3.95 |      - |         - |          NA |
|    DirectAccessLong |             .NET 7.0 |  0.4631 ns |  0.4749 ns |  0.35 |      - |         - |          NA |
|   TypeMapperBoolean |             .NET 7.0 |  5.0106 ns |  4.9086 ns |  3.78 |      - |         - |          NA |
| DirectAccessBoolean |             .NET 7.0 |  0.7503 ns |  0.7511 ns |  0.57 |      - |         - |          NA |
|   TypeMapperWrapper |             .NET 7.0 |  8.1043 ns | 11.8941 ns |  9.16 |      - |         - |          NA |
| DirectAccessWrapper |             .NET 7.0 |  0.5633 ns |  0.5964 ns |  0.43 |      - |         - |          NA |
|      TypeMapperEnum |             .NET 7.0 |  6.8463 ns |  6.8506 ns |  5.16 |      - |         - |          NA |
|    DirectAccessEnum |             .NET 7.0 |  0.3926 ns |  0.4619 ns |  0.30 |      - |         - |          NA |
|   TypeMapperVersion |             .NET 7.0 |  4.1074 ns |  5.0542 ns |  3.91 |      - |         - |          NA |
| DirectAccessVersion |             .NET 7.0 |  0.4432 ns |  0.3630 ns |  0.34 |      - |         - |          NA |
|    TypeMapperString |        .NET Core 3.1 |  3.1576 ns |  4.4728 ns |  0.73 |      - |         - |          NA |
|  DirectAccessString |        .NET Core 3.1 |  0.8839 ns |  0.9433 ns |  0.65 |      - |         - |          NA |
|       TypeMapperInt |        .NET Core 3.1 |  5.4003 ns |  5.4070 ns |  4.06 |      - |         - |          NA |
|     DirectAccessInt |        .NET Core 3.1 |  0.9383 ns |  0.9358 ns |  0.70 |      - |         - |          NA |
|      TypeMapperLong |        .NET Core 3.1 |  6.0144 ns |  5.9473 ns |  4.53 |      - |         - |          NA |
|    DirectAccessLong |        .NET Core 3.1 |  0.8534 ns |  0.8648 ns |  0.64 |      - |         - |          NA |
|   TypeMapperBoolean |        .NET Core 3.1 |  5.9078 ns |  5.8180 ns |  4.45 |      - |         - |          NA |
| DirectAccessBoolean |        .NET Core 3.1 |  1.2829 ns |  1.2277 ns |  0.96 |      - |         - |          NA |
|   TypeMapperWrapper |        .NET Core 3.1 | 15.5476 ns | 15.7652 ns | 11.72 |      - |         - |          NA |
| DirectAccessWrapper |        .NET Core 3.1 |  0.9345 ns |  0.8575 ns |  0.69 |      - |         - |          NA |
|      TypeMapperEnum |        .NET Core 3.1 | 34.5994 ns | 34.8720 ns | 25.91 | 0.0014 |      24 B |          NA |
|    DirectAccessEnum |        .NET Core 3.1 |  1.4088 ns |  1.4040 ns |  1.06 |      - |         - |          NA |
|   TypeMapperVersion |        .NET Core 3.1 |  5.8694 ns |  5.9275 ns |  4.28 |      - |         - |          NA |
| DirectAccessVersion |        .NET Core 3.1 |  0.8078 ns |  0.8420 ns |  0.60 |      - |         - |          NA |
|    TypeMapperString | .NET Framework 4.7.2 | 23.7587 ns | 23.8864 ns | 17.89 |      - |         - |          NA |
|  DirectAccessString | .NET Framework 4.7.2 |  1.3308 ns |  1.3639 ns |  1.00 |      - |         - |          NA |
|       TypeMapperInt | .NET Framework 4.7.2 | 22.4002 ns | 23.3690 ns | 13.36 |      - |         - |          NA |
|     DirectAccessInt | .NET Framework 4.7.2 |  0.5629 ns |  0.6004 ns |  0.27 |      - |         - |          NA |
|      TypeMapperLong | .NET Framework 4.7.2 | 10.3871 ns | 10.3512 ns |  7.85 |      - |         - |          NA |
|    DirectAccessLong | .NET Framework 4.7.2 |  0.8313 ns |  0.8080 ns |  0.63 |      - |         - |          NA |
|   TypeMapperBoolean | .NET Framework 4.7.2 | 22.7647 ns | 22.7967 ns | 17.14 |      - |         - |          NA |
| DirectAccessBoolean | .NET Framework 4.7.2 |  0.8186 ns |  0.8491 ns |  0.59 |      - |         - |          NA |
|   TypeMapperWrapper | .NET Framework 4.7.2 | 32.6508 ns | 38.3914 ns | 27.88 |      - |         - |          NA |
| DirectAccessWrapper | .NET Framework 4.7.2 |  0.8394 ns |  0.8563 ns |  0.64 |      - |         - |          NA |
|      TypeMapperEnum | .NET Framework 4.7.2 | 67.8830 ns | 67.7612 ns | 51.13 | 0.0038 |      24 B |          NA |
|    DirectAccessEnum | .NET Framework 4.7.2 |  0.9226 ns |  0.9150 ns |  0.70 |      - |         - |          NA |
|   TypeMapperVersion | .NET Framework 4.7.2 | 23.3183 ns | 23.3285 ns | 17.56 |      - |         - |          NA |
| DirectAccessVersion | .NET Framework 4.7.2 |  0.9789 ns |  0.9855 ns |  0.75 |      - |         - |          NA |
