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
|             Method |              Runtime |         Mean |       Median |  Ratio |    Gen0 | Allocated | Alloc Ratio |
|------------------- |--------------------- |-------------:|-------------:|-------:|--------:|----------:|------------:|
|            LinqSet |             .NET 6.0 | 364,264.8 ns | 366,243.4 ns | 460.10 |  4.3945 |   77203 B |       95.31 |
|         LinqObject |             .NET 6.0 | 226,096.1 ns | 226,472.6 ns | 285.67 |  2.4414 |   44913 B |       55.45 |
|             Object |             .NET 6.0 |   5,585.2 ns |   5,583.0 ns |   7.05 |  0.4883 |    8288 B |       10.23 |
|    CompiledLinqSet |             .NET 6.0 |  11,320.8 ns |  11,322.5 ns |  14.29 |  0.4730 |    8048 B |        9.94 |
| CompiledLinqObject |             .NET 6.0 |  11,485.8 ns |  11,517.7 ns |  14.50 |  0.4730 |    8048 B |        9.94 |
|          RawAdoNet |             .NET 6.0 |     223.1 ns |     221.9 ns |   0.28 |  0.0424 |     712 B |        0.88 |
|            LinqSet |             .NET 7.0 | 267,522.4 ns | 267,337.1 ns | 338.34 |  2.9297 |   55106 B |       68.03 |
|         LinqObject |             .NET 7.0 | 187,517.6 ns | 188,057.4 ns | 236.76 |  2.1973 |   39345 B |       48.57 |
|             Object |             .NET 7.0 |  12,902.4 ns |  12,884.2 ns |  16.30 |  0.4883 |    8288 B |       10.23 |
|    CompiledLinqSet |             .NET 7.0 |   8,191.7 ns |   8,710.9 ns |  12.35 |  0.4807 |    8048 B |        9.94 |
| CompiledLinqObject |             .NET 7.0 |  10,361.5 ns |  11,978.2 ns |  12.49 |  0.4730 |    8048 B |        9.94 |
|          RawAdoNet |             .NET 7.0 |     232.3 ns |     233.9 ns |   0.29 |  0.0424 |     712 B |        0.88 |
|            LinqSet |        .NET Core 3.1 | 451,722.7 ns | 452,645.5 ns | 570.85 |  4.3945 |   77411 B |       95.57 |
|         LinqObject |        .NET Core 3.1 | 281,360.2 ns | 281,303.7 ns | 355.27 |  2.4414 |   44308 B |       54.70 |
|             Object |        .NET Core 3.1 |  18,447.9 ns |  18,613.0 ns |  23.33 |  0.4883 |    8320 B |       10.27 |
|    CompiledLinqSet |        .NET Core 3.1 |  15,885.0 ns |  15,740.0 ns |  20.08 |  0.4578 |    8016 B |        9.90 |
| CompiledLinqObject |        .NET Core 3.1 |  16,499.5 ns |  16,680.6 ns |  20.82 |  0.4578 |    8016 B |        9.90 |
|          RawAdoNet |        .NET Core 3.1 |     251.5 ns |     250.9 ns |   0.32 |  0.0424 |     712 B |        0.88 |
|            LinqSet | .NET Framework 4.7.2 | 684,061.3 ns | 687,589.6 ns | 861.08 | 14.6484 |   97730 B |      120.65 |
|         LinqObject | .NET Framework 4.7.2 | 436,760.3 ns | 436,283.6 ns | 551.48 |  9.7656 |   62285 B |       76.90 |
|             Object | .NET Framework 4.7.2 |  21,489.6 ns |  21,374.1 ns |  27.14 |  1.3733 |    8698 B |       10.74 |
|    CompiledLinqSet | .NET Framework 4.7.2 |  18,004.1 ns |  18,006.5 ns |  22.73 |  1.2817 |    8232 B |       10.16 |
| CompiledLinqObject | .NET Framework 4.7.2 |   8,380.1 ns |   8,353.6 ns |  10.63 |  1.2817 |    8232 B |       10.16 |
|          RawAdoNet | .NET Framework 4.7.2 |     791.7 ns |     788.4 ns |   1.00 |  0.1287 |     810 B |        1.00 |
