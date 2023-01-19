``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-UZBSVL : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-AYZXIO : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-NXXYQT : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-HMCTKM : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|             Method |              Runtime |         Mean |       Median |  Ratio |    Gen0 | Allocated | Alloc Ratio |
|------------------- |--------------------- |-------------:|-------------:|-------:|--------:|----------:|------------:|
|            LinqSet |             .NET 6.0 | 284,448.4 ns | 285,038.4 ns | 391.91 |  4.3945 |   74705 B |       92.23 |
|         LinqObject |             .NET 6.0 |  78,285.9 ns |  78,201.1 ns | 107.72 |  2.4414 |   42385 B |       52.33 |
|             Object |             .NET 6.0 |  11,685.8 ns |  11,600.4 ns |  16.10 |  0.4883 |    8288 B |       10.23 |
|    CompiledLinqSet |             .NET 6.0 |  10,383.1 ns |  10,133.6 ns |  14.38 |  0.4730 |    8048 B |        9.94 |
| CompiledLinqObject |             .NET 6.0 |  11,869.2 ns |  11,924.9 ns |  16.35 |  0.4730 |    8048 B |        9.94 |
|          RawAdoNet |             .NET 6.0 |     276.1 ns |     275.0 ns |   0.38 |  0.0424 |     712 B |        0.88 |
|            LinqSet |             .NET 7.0 | 192,336.6 ns | 191,369.6 ns | 264.43 |  2.6855 |   47809 B |       59.02 |
|         LinqObject |             .NET 7.0 | 141,390.8 ns | 141,066.6 ns | 194.54 |  1.9531 |   36608 B |       45.20 |
|             Object |             .NET 7.0 |  13,196.5 ns |  13,201.7 ns |  18.15 |  0.4883 |    8288 B |       10.23 |
|    CompiledLinqSet |             .NET 7.0 |  10,578.8 ns |  10,633.2 ns |  14.54 |  0.4730 |    8048 B |        9.94 |
| CompiledLinqObject |             .NET 7.0 |  10,716.9 ns |  11,462.6 ns |  14.49 |  0.4807 |    8048 B |        9.94 |
|          RawAdoNet |             .NET 7.0 |     242.0 ns |     243.0 ns |   0.33 |  0.0424 |     712 B |        0.88 |
|            LinqSet |        .NET Core 3.1 | 355,209.3 ns | 356,503.7 ns | 488.63 |  4.3945 |   75525 B |       93.24 |
|         LinqObject |        .NET Core 3.1 | 221,849.7 ns | 222,515.0 ns | 305.24 |  2.4414 |   42400 B |       52.35 |
|             Object |        .NET Core 3.1 |  19,505.1 ns |  19,614.0 ns |  26.84 |  0.4883 |    8320 B |       10.27 |
|    CompiledLinqSet |        .NET Core 3.1 |  16,661.4 ns |  16,699.1 ns |  22.90 |  0.4578 |    8016 B |        9.90 |
| CompiledLinqObject |        .NET Core 3.1 |  16,561.8 ns |  16,673.3 ns |  22.84 |  0.4578 |    8016 B |        9.90 |
|          RawAdoNet |        .NET Core 3.1 |     250.4 ns |     249.1 ns |   0.34 |  0.0424 |     712 B |        0.88 |
|            LinqSet | .NET Framework 4.7.2 | 468,035.9 ns | 470,521.3 ns | 644.18 | 13.1836 |   85713 B |      105.82 |
|         LinqObject | .NET Framework 4.7.2 | 294,747.8 ns | 286,538.5 ns | 404.75 |  7.8125 |   52121 B |       64.35 |
|             Object | .NET Framework 4.7.2 |  21,172.6 ns |  21,273.6 ns |  29.10 |  1.3733 |    8698 B |       10.74 |
|    CompiledLinqSet | .NET Framework 4.7.2 |  17,983.0 ns |  18,226.2 ns |  24.97 |  1.2970 |    8232 B |       10.16 |
| CompiledLinqObject | .NET Framework 4.7.2 |  16,615.5 ns |  16,478.0 ns |  22.80 |  1.2817 |    8232 B |       10.16 |
|          RawAdoNet | .NET Framework 4.7.2 |     726.8 ns |     726.7 ns |   1.00 |  0.1287 |     810 B |        1.00 |
