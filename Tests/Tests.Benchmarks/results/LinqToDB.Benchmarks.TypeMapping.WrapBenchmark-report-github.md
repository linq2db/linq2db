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
|                      Method |              Runtime |        Mean |      Median |   Ratio |   Gen0 | Allocated | Alloc Ratio |
|---------------------------- |--------------------- |------------:|------------:|--------:|-------:|----------:|------------:|
|            TypeMapperString |             .NET 6.0 |   0.0000 ns |   0.0000 ns |   0.000 |      - |         - |          NA |
|          DirectAccessString |             .NET 6.0 |   2.1013 ns |   2.0989 ns |   1.580 |      - |         - |          NA |
|   TypeMapperWrappedInstance |             .NET 6.0 |  49.5057 ns |  49.5705 ns |  37.453 | 0.0019 |      32 B |          NA |
| DirectAccessWrappedInstance |             .NET 6.0 |   0.9479 ns |   0.9272 ns |   0.711 |      - |         - |          NA |
|     TypeMapperGetEnumerator |             .NET 6.0 |  47.5748 ns |  61.0434 ns |  30.505 | 0.0019 |      32 B |          NA |
|   DirectAccessGetEnumerator |             .NET 6.0 |  56.8911 ns |  56.7594 ns |  42.706 | 0.0019 |      32 B |          NA |
|            TypeMapperString |             .NET 7.0 |   6.1866 ns |   6.0281 ns |   4.636 |      - |         - |          NA |
|          DirectAccessString |             .NET 7.0 |   1.4705 ns |   1.6499 ns |   1.330 |      - |         - |          NA |
|   TypeMapperWrappedInstance |             .NET 7.0 |  48.1458 ns |  48.1969 ns |  36.217 | 0.0019 |      32 B |          NA |
| DirectAccessWrappedInstance |             .NET 7.0 |   1.8089 ns |   1.8304 ns |   1.361 |      - |         - |          NA |
|     TypeMapperGetEnumerator |             .NET 7.0 |  54.4994 ns |  55.0551 ns |  41.009 | 0.0019 |      32 B |          NA |
|   DirectAccessGetEnumerator |             .NET 7.0 |  56.0400 ns |  55.9814 ns |  42.102 | 0.0019 |      32 B |          NA |
|            TypeMapperString |        .NET Core 3.1 |   6.0204 ns |   6.0882 ns |   4.497 |      - |         - |          NA |
|          DirectAccessString |        .NET Core 3.1 |   0.9006 ns |   0.9543 ns |   0.671 |      - |         - |          NA |
|   TypeMapperWrappedInstance |        .NET Core 3.1 |  46.7199 ns |  54.0388 ns |  24.493 | 0.0019 |      32 B |          NA |
| DirectAccessWrappedInstance |        .NET Core 3.1 |   0.8405 ns |   0.8601 ns |   0.630 |      - |         - |          NA |
|     TypeMapperGetEnumerator |        .NET Core 3.1 | 129.7661 ns | 130.2584 ns |  97.581 | 0.0019 |      32 B |          NA |
|   DirectAccessGetEnumerator |        .NET Core 3.1 |  91.3461 ns |  88.8569 ns |  80.741 | 0.0019 |      32 B |          NA |
|            TypeMapperString | .NET Framework 4.7.2 |  23.8604 ns |  23.8231 ns |  17.997 |      - |         - |          NA |
|          DirectAccessString | .NET Framework 4.7.2 |   1.3388 ns |   1.3070 ns |   1.000 |      - |         - |          NA |
|   TypeMapperWrappedInstance | .NET Framework 4.7.2 |  83.3143 ns |  93.1541 ns |  70.953 | 0.0050 |      32 B |          NA |
| DirectAccessWrappedInstance | .NET Framework 4.7.2 |   4.6124 ns |   4.6129 ns |   3.490 |      - |         - |          NA |
|     TypeMapperGetEnumerator | .NET Framework 4.7.2 | 181.4533 ns | 183.8010 ns | 134.832 | 0.0088 |      56 B |          NA |
|   DirectAccessGetEnumerator | .NET Framework 4.7.2 | 156.0771 ns | 162.7708 ns |  95.396 | 0.0088 |      56 B |          NA |
