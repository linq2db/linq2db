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
|                      Method |              Runtime |        Mean |      Median |  Ratio |   Gen0 | Allocated | Alloc Ratio |
|---------------------------- |--------------------- |------------:|------------:|-------:|-------:|----------:|------------:|
|            TypeMapperString |             .NET 6.0 |   2.7250 ns |   2.7250 ns |  1.933 |      - |         - |          NA |
|          DirectAccessString |             .NET 6.0 |   1.4351 ns |   1.3694 ns |  1.006 |      - |         - |          NA |
|   TypeMapperWrappedInstance |             .NET 6.0 |  46.9486 ns |  47.7633 ns | 32.284 | 0.0019 |      32 B |          NA |
| DirectAccessWrappedInstance |             .NET 6.0 |   0.9141 ns |   0.9144 ns |  0.649 |      - |         - |          NA |
|     TypeMapperGetEnumerator |             .NET 6.0 |  59.0080 ns |  59.0342 ns | 41.877 | 0.0019 |      32 B |          NA |
|   DirectAccessGetEnumerator |             .NET 6.0 |  53.4054 ns |  53.3880 ns | 37.866 | 0.0019 |      32 B |          NA |
|            TypeMapperString |             .NET 7.0 |   6.8491 ns |   6.8486 ns |  4.861 |      - |         - |          NA |
|          DirectAccessString |             .NET 7.0 |   0.0000 ns |   0.0000 ns |  0.000 |      - |         - |          NA |
|   TypeMapperWrappedInstance |             .NET 7.0 |  45.7267 ns |  45.9418 ns | 32.442 | 0.0019 |      32 B |          NA |
| DirectAccessWrappedInstance |             .NET 7.0 |   1.8255 ns |   1.8255 ns |  1.294 |      - |         - |          NA |
|     TypeMapperGetEnumerator |             .NET 7.0 |  59.1017 ns |  58.9621 ns | 41.831 | 0.0019 |      32 B |          NA |
|   DirectAccessGetEnumerator |             .NET 7.0 |  52.2574 ns |  52.2623 ns | 37.052 | 0.0019 |      32 B |          NA |
|            TypeMapperString |        .NET Core 3.1 |   5.4791 ns |   5.4789 ns |  3.888 |      - |         - |          NA |
|          DirectAccessString |        .NET Core 3.1 |   0.8568 ns |   0.8564 ns |  0.608 |      - |         - |          NA |
|   TypeMapperWrappedInstance |        .NET Core 3.1 |  52.1745 ns |  52.4792 ns | 37.024 | 0.0019 |      32 B |          NA |
| DirectAccessWrappedInstance |        .NET Core 3.1 |   0.7072 ns |   0.7070 ns |  0.502 |      - |         - |          NA |
|     TypeMapperGetEnumerator |        .NET Core 3.1 | 126.5060 ns | 126.5008 ns | 89.696 | 0.0019 |      32 B |          NA |
|   DirectAccessGetEnumerator |        .NET Core 3.1 | 106.8796 ns | 121.1746 ns | 59.914 | 0.0019 |      32 B |          NA |
|            TypeMapperString | .NET Framework 4.7.2 |  22.2187 ns |  23.2291 ns | 14.885 |      - |         - |          NA |
|          DirectAccessString | .NET Framework 4.7.2 |   1.4093 ns |   1.4088 ns |  1.000 |      - |         - |          NA |
|   TypeMapperWrappedInstance | .NET Framework 4.7.2 | 101.8121 ns | 102.2683 ns | 72.246 | 0.0050 |      32 B |          NA |
| DirectAccessWrappedInstance | .NET Framework 4.7.2 |   1.2177 ns |   1.2580 ns |  0.678 |      - |         - |          NA |
|     TypeMapperGetEnumerator | .NET Framework 4.7.2 | 156.2822 ns | 178.8164 ns | 75.531 | 0.0088 |      56 B |          NA |
|   DirectAccessGetEnumerator | .NET Framework 4.7.2 | 145.0668 ns | 157.3694 ns | 99.770 | 0.0088 |      56 B |          NA |
