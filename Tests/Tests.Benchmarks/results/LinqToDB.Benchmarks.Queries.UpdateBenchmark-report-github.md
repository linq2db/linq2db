``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-TEPEZT : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-ISYUTK : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-SMHCKK : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-DHDWVI : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|             Method |              Runtime |         Mean |       Median |  Ratio |    Gen0 | Allocated | Alloc Ratio |
|------------------- |--------------------- |-------------:|-------------:|-------:|--------:|----------:|------------:|
|            LinqSet |             .NET 6.0 | 255,039.2 ns | 255,404.4 ns | 344.68 |  3.9063 |   68961 B |       85.14 |
|         LinqObject |             .NET 6.0 | 175,448.1 ns | 175,966.5 ns | 235.27 |  2.1973 |   38688 B |       47.76 |
|             Object |             .NET 6.0 |  13,655.2 ns |  13,695.5 ns |  18.24 |  0.4883 |    8288 B |       10.23 |
|    CompiledLinqSet |             .NET 6.0 |  11,730.2 ns |  11,735.9 ns |  15.74 |  0.4730 |    8048 B |        9.94 |
| CompiledLinqObject |             .NET 6.0 |  12,370.0 ns |  12,344.1 ns |  16.69 |  0.4730 |    8048 B |        9.94 |
|          RawAdoNet |             .NET 6.0 |     235.5 ns |     243.8 ns |   0.32 |  0.0424 |     712 B |        0.88 |
|            LinqSet |             .NET 7.0 | 196,776.5 ns | 196,956.8 ns | 263.62 |  2.4414 |   43169 B |       53.30 |
|         LinqObject |             .NET 7.0 | 136,962.0 ns | 140,280.0 ns | 155.28 |  1.7090 |   32320 B |       39.90 |
|             Object |             .NET 7.0 |  13,471.1 ns |  13,475.6 ns |  18.10 |  0.4883 |    8288 B |       10.23 |
|    CompiledLinqSet |             .NET 7.0 |  10,897.5 ns |  10,936.1 ns |  14.56 |  0.4730 |    8048 B |        9.94 |
| CompiledLinqObject |             .NET 7.0 |  11,227.3 ns |  11,784.0 ns |  15.07 |  0.4807 |    8048 B |        9.94 |
|          RawAdoNet |             .NET 7.0 |     227.5 ns |     228.0 ns |   0.30 |  0.0424 |     712 B |        0.88 |
|            LinqSet |        .NET Core 3.1 | 365,311.7 ns | 365,036.7 ns | 489.59 |  4.1504 |   71201 B |       87.90 |
|         LinqObject |        .NET Core 3.1 | 226,500.9 ns | 226,647.6 ns | 303.53 |  2.1973 |   39201 B |       48.40 |
|             Object |        .NET Core 3.1 |  18,304.6 ns |  18,912.9 ns |  20.53 |  0.4883 |    8320 B |       10.27 |
|    CompiledLinqSet |        .NET Core 3.1 |  16,159.6 ns |  16,255.2 ns |  21.63 |  0.4578 |    8016 B |        9.90 |
| CompiledLinqObject |        .NET Core 3.1 |  16,814.8 ns |  16,927.9 ns |  22.55 |  0.4578 |    8016 B |        9.90 |
|          RawAdoNet |        .NET Core 3.1 |     236.2 ns |     261.9 ns |   0.23 |  0.0424 |     712 B |        0.88 |
|            LinqSet | .NET Framework 4.7.2 | 371,410.0 ns | 470,278.8 ns | 363.45 | 12.2070 |   77094 B |       95.18 |
|         LinqObject | .NET Framework 4.7.2 | 284,423.6 ns | 284,878.5 ns | 381.15 |  7.3242 |   48365 B |       59.71 |
|             Object | .NET Framework 4.7.2 |  18,644.1 ns |  21,449.2 ns |  20.48 |  1.3733 |    8698 B |       10.74 |
|    CompiledLinqSet | .NET Framework 4.7.2 |  18,854.5 ns |  18,905.9 ns |  25.28 |  1.2817 |    8232 B |       10.16 |
| CompiledLinqObject | .NET Framework 4.7.2 |  19,159.9 ns |  19,187.9 ns |  25.67 |  1.2817 |    8232 B |       10.16 |
|          RawAdoNet | .NET Framework 4.7.2 |     745.8 ns |     748.6 ns |   1.00 |  0.1287 |     810 B |        1.00 |
