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
|                      Method |              Runtime |        Mean |      Median |   Ratio |   Gen0 | Allocated | Alloc Ratio |
|---------------------------- |--------------------- |------------:|------------:|--------:|-------:|----------:|------------:|
|            TypeMapperString |             .NET 6.0 |   6.2470 ns |   6.7059 ns |   4.490 |      - |         - |          NA |
|          DirectAccessString |             .NET 6.0 |   0.8408 ns |   0.8546 ns |   0.644 |      - |         - |          NA |
|   TypeMapperWrappedInstance |             .NET 6.0 |  47.6719 ns |  47.8245 ns |  37.579 | 0.0019 |      32 B |          NA |
| DirectAccessWrappedInstance |             .NET 6.0 |   0.8231 ns |   0.9489 ns |   0.558 |      - |         - |          NA |
|     TypeMapperGetEnumerator |             .NET 6.0 |  58.9298 ns |  58.1692 ns |  45.075 | 0.0019 |      32 B |          NA |
|   DirectAccessGetEnumerator |             .NET 6.0 |  57.0699 ns |  57.3033 ns |  43.611 | 0.0019 |      32 B |          NA |
|            TypeMapperString |             .NET 7.0 |   6.0826 ns |   6.0165 ns |   4.736 |      - |         - |          NA |
|          DirectAccessString |             .NET 7.0 |   1.9441 ns |   1.9613 ns |   1.488 |      - |         - |          NA |
|   TypeMapperWrappedInstance |             .NET 7.0 |  49.7951 ns |  49.8106 ns |  38.100 | 0.0019 |      32 B |          NA |
| DirectAccessWrappedInstance |             .NET 7.0 |   0.0000 ns |   0.0000 ns |   0.000 |      - |         - |          NA |
|     TypeMapperGetEnumerator |             .NET 7.0 |  58.4993 ns |  58.6383 ns |  45.139 | 0.0019 |      32 B |          NA |
|   DirectAccessGetEnumerator |             .NET 7.0 |  54.1987 ns |  54.3666 ns |  41.427 | 0.0019 |      32 B |          NA |
|            TypeMapperString |        .NET Core 3.1 |   4.8523 ns |   5.6020 ns |   4.332 |      - |         - |          NA |
|          DirectAccessString |        .NET Core 3.1 |   1.0225 ns |   1.0605 ns |   0.783 |      - |         - |          NA |
|   TypeMapperWrappedInstance |        .NET Core 3.1 |  53.7691 ns |  53.3171 ns |  41.303 | 0.0019 |      32 B |          NA |
| DirectAccessWrappedInstance |        .NET Core 3.1 |   0.8859 ns |   0.9685 ns |   0.627 |      - |         - |          NA |
|     TypeMapperGetEnumerator |        .NET Core 3.1 | 122.6192 ns | 128.4124 ns |  72.070 | 0.0019 |      32 B |          NA |
|   DirectAccessGetEnumerator |        .NET Core 3.1 | 110.9098 ns | 129.1229 ns |  76.334 | 0.0019 |      32 B |          NA |
|            TypeMapperString | .NET Framework 4.7.2 |  19.9845 ns |  21.0273 ns |  16.126 |      - |         - |          NA |
|          DirectAccessString | .NET Framework 4.7.2 |   1.3072 ns |   1.3155 ns |   1.000 |      - |         - |          NA |
|   TypeMapperWrappedInstance | .NET Framework 4.7.2 |  93.1359 ns |  92.8601 ns |  71.266 | 0.0050 |      32 B |          NA |
| DirectAccessWrappedInstance | .NET Framework 4.7.2 |   1.4573 ns |   1.4587 ns |   1.116 |      - |         - |          NA |
|     TypeMapperGetEnumerator | .NET Framework 4.7.2 | 189.3677 ns | 188.6090 ns | 144.895 | 0.0088 |      56 B |          NA |
|   DirectAccessGetEnumerator | .NET Framework 4.7.2 | 154.5590 ns | 153.4264 ns | 118.268 | 0.0088 |      56 B |          NA |
