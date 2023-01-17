``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WUZRIO : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-EMBONI : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-HZWTXS : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-VIGHHX : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|             Method |              Runtime |         Mean |       Median |    Ratio |    Gen0 | Allocated | Alloc Ratio |
|------------------- |--------------------- |-------------:|-------------:|---------:|--------:|----------:|------------:|
|            LinqSet |             .NET 6.0 | 362,036.2 ns | 362,430.3 ns | 1,109.11 |  4.3945 |   77203 B |       95.31 |
|         LinqObject |             .NET 6.0 | 227,360.4 ns | 227,366.8 ns |   696.52 |  2.4414 |   44913 B |       55.45 |
|             Object |             .NET 6.0 |  13,171.5 ns |  13,251.7 ns |    40.30 |  0.4883 |    8288 B |       10.23 |
|    CompiledLinqSet |             .NET 6.0 |  11,530.1 ns |  11,529.6 ns |    35.32 |  0.4730 |    8048 B |        9.94 |
| CompiledLinqObject |             .NET 6.0 |  11,566.1 ns |  11,576.3 ns |    35.43 |  0.4730 |    8048 B |        9.94 |
|          RawAdoNet |             .NET 6.0 |     244.2 ns |     244.1 ns |     0.75 |  0.0424 |     712 B |        0.88 |
|            LinqSet |             .NET 7.0 | 266,407.6 ns | 266,501.7 ns |   816.08 |  2.9297 |   55554 B |       68.59 |
|         LinqObject |             .NET 7.0 | 187,401.0 ns | 187,241.9 ns |   574.06 |  2.1973 |   39345 B |       48.57 |
|             Object |             .NET 7.0 |  12,639.5 ns |  12,559.4 ns |    38.62 |  0.4883 |    8288 B |       10.23 |
|    CompiledLinqSet |             .NET 7.0 |  11,011.8 ns |  10,972.1 ns |    33.77 |  0.4730 |    8048 B |        9.94 |
| CompiledLinqObject |             .NET 7.0 |  11,477.6 ns |  11,484.8 ns |    35.16 |  0.4730 |    8048 B |        9.94 |
|          RawAdoNet |             .NET 7.0 |     231.9 ns |     232.1 ns |     0.71 |  0.0424 |     712 B |        0.88 |
|            LinqSet |        .NET Core 3.1 | 454,780.6 ns | 454,405.8 ns | 1,393.12 |  4.3945 |   77411 B |       95.57 |
|         LinqObject |        .NET Core 3.1 | 259,891.9 ns | 287,881.6 ns |   879.46 |  2.4414 |   44306 B |       54.70 |
|             Object |        .NET Core 3.1 |  18,355.7 ns |  18,454.5 ns |    55.63 |  0.4883 |    8320 B |       10.27 |
|    CompiledLinqSet |        .NET Core 3.1 |  15,996.5 ns |  15,973.1 ns |    49.03 |  0.4578 |    8016 B |        9.90 |
| CompiledLinqObject |        .NET Core 3.1 |  16,181.3 ns |  16,186.6 ns |    49.57 |  0.4578 |    8016 B |        9.90 |
|          RawAdoNet |        .NET Core 3.1 |     247.2 ns |     246.9 ns |     0.76 |  0.0424 |     712 B |        0.88 |
|            LinqSet | .NET Framework 4.7.2 | 696,971.4 ns | 696,846.9 ns | 2,135.19 | 14.6484 |   97730 B |      120.65 |
|         LinqObject | .NET Framework 4.7.2 | 446,783.6 ns | 446,462.1 ns | 1,368.73 |  9.7656 |   61898 B |       76.42 |
|             Object | .NET Framework 4.7.2 |  20,760.4 ns |  20,758.7 ns |    63.59 |  1.3733 |    8698 B |       10.74 |
|    CompiledLinqSet | .NET Framework 4.7.2 |  18,168.5 ns |  18,310.5 ns |    55.62 |  1.2817 |    8232 B |       10.16 |
| CompiledLinqObject | .NET Framework 4.7.2 |  19,113.4 ns |  19,165.2 ns |    58.58 |  1.2817 |    8232 B |       10.16 |
|          RawAdoNet | .NET Framework 4.7.2 |     326.4 ns |     326.5 ns |     1.00 |  0.1287 |     810 B |        1.00 |
