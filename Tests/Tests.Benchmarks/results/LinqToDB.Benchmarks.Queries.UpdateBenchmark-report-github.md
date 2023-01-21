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
|             Method |              Runtime |         Mean |       Median |  Ratio |    Gen0 |   Gen1 | Allocated | Alloc Ratio |
|------------------- |--------------------- |-------------:|-------------:|-------:|--------:|-------:|----------:|------------:|
|            LinqSet |             .NET 6.0 | 310,415.2 ns | 321,771.4 ns | 376.91 |  3.9063 |      - |   68961 B |       85.14 |
|         LinqObject |             .NET 6.0 | 189,267.8 ns | 189,542.2 ns | 230.49 |  2.1973 |      - |   40097 B |       49.50 |
|             Object |             .NET 6.0 |  13,550.9 ns |  12,628.7 ns |  16.41 |  0.4883 |      - |    8288 B |       10.23 |
|    CompiledLinqSet |             .NET 6.0 |  13,343.2 ns |  14,047.3 ns |  16.19 |  0.4730 |      - |    8048 B |        9.94 |
| CompiledLinqObject |             .NET 6.0 |  12,761.2 ns |  12,935.8 ns |  15.47 |  0.4578 |      - |    8048 B |        9.94 |
|          RawAdoNet |             .NET 6.0 |     310.5 ns |     325.0 ns |   0.38 |  0.0424 |      - |     712 B |        0.88 |
|            LinqSet |             .NET 7.0 | 199,144.9 ns | 203,227.6 ns | 242.13 |  2.4414 |      - |   42017 B |       51.87 |
|         LinqObject |             .NET 7.0 | 147,198.1 ns | 153,834.7 ns | 177.94 |  1.8311 |      - |   32544 B |       40.18 |
|             Object |             .NET 7.0 |  15,570.5 ns |  16,093.2 ns |  18.88 |  0.4883 |      - |    8288 B |       10.23 |
|    CompiledLinqSet |             .NET 7.0 |  11,938.2 ns |  12,692.4 ns |  14.35 |  0.4730 |      - |    8048 B |        9.94 |
| CompiledLinqObject |             .NET 7.0 |  12,365.3 ns |  13,298.0 ns |  15.01 |  0.4730 |      - |    8048 B |        9.94 |
|          RawAdoNet |             .NET 7.0 |     309.0 ns |     323.9 ns |   0.37 |  0.0424 |      - |     712 B |        0.88 |
|            LinqSet |        .NET Core 3.1 | 389,529.4 ns | 407,755.5 ns | 471.20 |  3.9063 |      - |   69793 B |       86.16 |
|         LinqObject |        .NET Core 3.1 | 238,127.0 ns | 242,826.2 ns | 287.91 |  1.9531 |      - |   38848 B |       47.96 |
|             Object |        .NET Core 3.1 |  22,050.3 ns |  22,242.1 ns |  26.77 |  0.4883 |      - |    8320 B |       10.27 |
|    CompiledLinqSet |        .NET Core 3.1 |  15,994.2 ns |  16,119.6 ns |  19.46 |  0.4578 |      - |    8016 B |        9.90 |
| CompiledLinqObject |        .NET Core 3.1 |  17,507.3 ns |  18,502.4 ns |  21.32 |  0.4730 |      - |    8016 B |        9.90 |
|          RawAdoNet |        .NET Core 3.1 |     330.1 ns |     333.5 ns |   0.40 |  0.0424 |      - |     712 B |        0.88 |
|            LinqSet | .NET Framework 4.7.2 | 486,863.8 ns | 511,694.2 ns | 591.47 | 11.7188 |      - |   77882 B |       96.15 |
|         LinqObject | .NET Framework 4.7.2 | 308,605.9 ns | 317,611.4 ns | 374.22 |  7.5684 |      - |   47975 B |       59.23 |
|             Object | .NET Framework 4.7.2 |  24,937.5 ns |  24,856.9 ns |  30.38 |  1.3733 | 0.0153 |    8698 B |       10.74 |
|    CompiledLinqSet | .NET Framework 4.7.2 |  19,176.2 ns |  18,982.4 ns |  23.11 |  1.2817 |      - |    8232 B |       10.16 |
| CompiledLinqObject | .NET Framework 4.7.2 |  20,944.1 ns |  21,320.6 ns |  25.56 |  1.2817 |      - |    8232 B |       10.16 |
|          RawAdoNet | .NET Framework 4.7.2 |     849.9 ns |     876.1 ns |   1.00 |  0.1287 |      - |     810 B |        1.00 |
