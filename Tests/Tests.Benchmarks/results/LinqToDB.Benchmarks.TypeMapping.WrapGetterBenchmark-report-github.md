``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-XCPGVR : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-RHOQGE : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WEVYVV : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-ORXRGX : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|              Method |              Runtime |       Mean |     Median | Ratio |   Gen0 | Allocated | Alloc Ratio |
|-------------------- |--------------------- |-----------:|-----------:|------:|-------:|----------:|------------:|
|    TypeMapperString |             .NET 6.0 |  5.2416 ns |  5.3013 ns |  3.99 |      - |         - |          NA |
|  DirectAccessString |             .NET 6.0 |  0.8857 ns |  0.9275 ns |  0.68 |      - |         - |          NA |
|       TypeMapperInt |             .NET 6.0 |  5.6018 ns |  5.5663 ns |  4.27 |      - |         - |          NA |
|     DirectAccessInt |             .NET 6.0 |  0.9135 ns |  0.9241 ns |  0.69 |      - |         - |          NA |
|      TypeMapperLong |             .NET 6.0 |  5.6538 ns |  5.6990 ns |  4.28 |      - |         - |          NA |
|    DirectAccessLong |             .NET 6.0 |  0.9178 ns |  0.9251 ns |  0.69 |      - |         - |          NA |
|   TypeMapperBoolean |             .NET 6.0 |  5.5916 ns |  5.4462 ns |  4.23 |      - |         - |          NA |
| DirectAccessBoolean |             .NET 6.0 |  0.9371 ns |  0.9424 ns |  0.71 |      - |         - |          NA |
|   TypeMapperWrapper |             .NET 6.0 | 13.5782 ns | 13.7523 ns | 10.26 |      - |         - |          NA |
| DirectAccessWrapper |             .NET 6.0 |  0.8790 ns |  0.9262 ns |  0.65 |      - |         - |          NA |
|      TypeMapperEnum |             .NET 6.0 | 26.8382 ns | 26.7921 ns | 20.34 | 0.0014 |      24 B |          NA |
|    DirectAccessEnum |             .NET 6.0 |  0.6102 ns |  0.9131 ns |  0.14 |      - |         - |          NA |
|   TypeMapperVersion |             .NET 6.0 |  5.8536 ns |  6.0038 ns |  4.39 |      - |         - |          NA |
| DirectAccessVersion |             .NET 6.0 |  0.9588 ns |  0.9872 ns |  0.73 |      - |         - |          NA |
|    TypeMapperString |             .NET 7.0 |  5.2195 ns |  5.2024 ns |  3.96 |      - |         - |          NA |
|  DirectAccessString |             .NET 7.0 |  0.4244 ns |  0.3244 ns |  0.33 |      - |         - |          NA |
|       TypeMapperInt |             .NET 7.0 |  5.1190 ns |  5.1270 ns |  3.88 |      - |         - |          NA |
|     DirectAccessInt |             .NET 7.0 |  0.4913 ns |  0.4945 ns |  0.37 |      - |         - |          NA |
|      TypeMapperLong |             .NET 7.0 |  5.2289 ns |  5.3147 ns |  3.97 |      - |         - |          NA |
|    DirectAccessLong |             .NET 7.0 |  0.4623 ns |  0.4595 ns |  0.35 |      - |         - |          NA |
|   TypeMapperBoolean |             .NET 7.0 |  5.0067 ns |  4.9898 ns |  3.80 |      - |         - |          NA |
| DirectAccessBoolean |             .NET 7.0 |  0.5490 ns |  0.5741 ns |  0.41 |      - |         - |          NA |
|   TypeMapperWrapper |             .NET 7.0 | 12.2113 ns | 12.2780 ns |  9.25 |      - |         - |          NA |
| DirectAccessWrapper |             .NET 7.0 |  0.4053 ns |  0.4566 ns |  0.29 |      - |         - |          NA |
|      TypeMapperEnum |             .NET 7.0 | 13.5990 ns | 13.6551 ns | 10.29 |      - |         - |          NA |
|    DirectAccessEnum |             .NET 7.0 |  0.4630 ns |  0.4735 ns |  0.35 |      - |         - |          NA |
|   TypeMapperVersion |             .NET 7.0 |  5.1470 ns |  5.2815 ns |  3.78 |      - |         - |          NA |
| DirectAccessVersion |             .NET 7.0 |  0.4228 ns |  0.4557 ns |  0.32 |      - |         - |          NA |
|    TypeMapperString |        .NET Core 3.1 |  6.1515 ns |  6.2021 ns |  4.67 |      - |         - |          NA |
|  DirectAccessString |        .NET Core 3.1 |  1.1708 ns |  1.2138 ns |  0.88 |      - |         - |          NA |
|       TypeMapperInt |        .NET Core 3.1 |  6.5229 ns |  6.4249 ns |  4.96 |      - |         - |          NA |
|     DirectAccessInt |        .NET Core 3.1 |  0.8296 ns |  0.8826 ns |  0.40 |      - |         - |          NA |
|      TypeMapperLong |        .NET Core 3.1 |  5.2914 ns |  5.3437 ns |  4.05 |      - |         - |          NA |
|    DirectAccessLong |        .NET Core 3.1 |  1.4702 ns |  1.4616 ns |  1.11 |      - |         - |          NA |
|   TypeMapperBoolean |        .NET Core 3.1 |  5.9267 ns |  5.9345 ns |  4.49 |      - |         - |          NA |
| DirectAccessBoolean |        .NET Core 3.1 |  1.2763 ns |  1.2990 ns |  0.96 |      - |         - |          NA |
|   TypeMapperWrapper |        .NET Core 3.1 | 16.1975 ns | 16.3158 ns | 12.28 |      - |         - |          NA |
| DirectAccessWrapper |        .NET Core 3.1 |  0.8921 ns |  0.9130 ns |  0.68 |      - |         - |          NA |
|      TypeMapperEnum |        .NET Core 3.1 | 32.4174 ns | 32.6696 ns | 24.50 | 0.0014 |      24 B |          NA |
|    DirectAccessEnum |        .NET Core 3.1 |  0.9165 ns |  0.9217 ns |  0.69 |      - |         - |          NA |
|   TypeMapperVersion |        .NET Core 3.1 |  4.4542 ns |  5.7844 ns |  1.03 |      - |         - |          NA |
| DirectAccessVersion |        .NET Core 3.1 |  0.7934 ns |  0.9424 ns |  0.63 |      - |         - |          NA |
|    TypeMapperString | .NET Framework 4.7.2 | 21.1798 ns | 21.0968 ns | 15.99 |      - |         - |          NA |
|  DirectAccessString | .NET Framework 4.7.2 |  1.3199 ns |  1.3275 ns |  1.00 |      - |         - |          NA |
|       TypeMapperInt | .NET Framework 4.7.2 | 22.7828 ns | 22.8353 ns | 17.26 |      - |         - |          NA |
|     DirectAccessInt | .NET Framework 4.7.2 |  1.3683 ns |  1.3743 ns |  1.03 |      - |         - |          NA |
|      TypeMapperLong | .NET Framework 4.7.2 | 20.3651 ns | 20.2156 ns | 15.29 |      - |         - |          NA |
|    DirectAccessLong | .NET Framework 4.7.2 |  0.9236 ns |  0.9326 ns |  0.70 |      - |         - |          NA |
|   TypeMapperBoolean | .NET Framework 4.7.2 | 22.1605 ns | 22.1960 ns | 16.79 |      - |         - |          NA |
| DirectAccessBoolean | .NET Framework 4.7.2 |  0.8364 ns |  0.8491 ns |  0.64 |      - |         - |          NA |
|   TypeMapperWrapper | .NET Framework 4.7.2 | 42.6654 ns | 42.5622 ns | 32.23 |      - |         - |          NA |
| DirectAccessWrapper | .NET Framework 4.7.2 |  1.2497 ns |  1.2989 ns |  0.96 |      - |         - |          NA |
|      TypeMapperEnum | .NET Framework 4.7.2 | 61.6486 ns | 68.7109 ns | 40.00 | 0.0038 |      24 B |          NA |
|    DirectAccessEnum | .NET Framework 4.7.2 |  0.8997 ns |  0.9207 ns |  0.68 |      - |         - |          NA |
|   TypeMapperVersion | .NET Framework 4.7.2 | 24.0130 ns | 23.9831 ns | 18.24 |      - |         - |          NA |
| DirectAccessVersion | .NET Framework 4.7.2 |  1.3116 ns |  1.3204 ns |  1.00 |      - |         - |          NA |
