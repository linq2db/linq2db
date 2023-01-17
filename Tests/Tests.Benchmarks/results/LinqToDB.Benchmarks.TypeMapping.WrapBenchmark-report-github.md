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
|                      Method |              Runtime |        Mean |      Median |  Ratio |   Gen0 | Allocated | Alloc Ratio |
|---------------------------- |--------------------- |------------:|------------:|-------:|-------:|----------:|------------:|
|            TypeMapperString |             .NET 6.0 |   6.0157 ns |   5.9512 ns |   7.03 |      - |         - |          NA |
|          DirectAccessString |             .NET 6.0 |   1.3723 ns |   1.3721 ns |   1.60 |      - |         - |          NA |
|   TypeMapperWrappedInstance |             .NET 6.0 |  47.9498 ns |  47.9423 ns |  55.86 | 0.0019 |      32 B |          NA |
| DirectAccessWrappedInstance |             .NET 6.0 |   0.9130 ns |   0.9143 ns |   1.06 |      - |         - |          NA |
|     TypeMapperGetEnumerator |             .NET 6.0 |  60.2434 ns |  59.6464 ns |  69.88 | 0.0019 |      32 B |          NA |
|   DirectAccessGetEnumerator |             .NET 6.0 |  60.5610 ns |  60.4979 ns |  70.55 | 0.0019 |      32 B |          NA |
|            TypeMapperString |             .NET 7.0 |   6.8607 ns |   6.8596 ns |   7.99 |      - |         - |          NA |
|          DirectAccessString |             .NET 7.0 |   1.8302 ns |   1.8303 ns |   2.13 |      - |         - |          NA |
|   TypeMapperWrappedInstance |             .NET 7.0 |  39.7136 ns |  45.8891 ns |  39.58 | 0.0019 |      32 B |          NA |
| DirectAccessWrappedInstance |             .NET 7.0 |   1.7514 ns |   1.7037 ns |   2.08 |      - |         - |          NA |
|     TypeMapperGetEnumerator |             .NET 7.0 |  61.7822 ns |  61.3518 ns |  71.82 | 0.0019 |      32 B |          NA |
|   DirectAccessGetEnumerator |             .NET 7.0 |  53.0164 ns |  53.0339 ns |  61.74 | 0.0019 |      32 B |          NA |
|            TypeMapperString |        .NET Core 3.1 |   6.1416 ns |   6.2273 ns |   7.15 |      - |         - |          NA |
|          DirectAccessString |        .NET Core 3.1 |   1.7137 ns |   0.2059 ns |   3.05 |      - |         - |          NA |
|   TypeMapperWrappedInstance |        .NET Core 3.1 |  51.4864 ns |  51.5383 ns |  59.96 | 0.0019 |      32 B |          NA |
| DirectAccessWrappedInstance |        .NET Core 3.1 |   0.8737 ns |   0.8436 ns |   1.01 |      - |         - |          NA |
|     TypeMapperGetEnumerator |        .NET Core 3.1 | 126.4408 ns | 126.4110 ns | 147.30 | 0.0019 |      32 B |          NA |
|   DirectAccessGetEnumerator |        .NET Core 3.1 | 122.4295 ns | 122.4246 ns | 142.62 | 0.0019 |      32 B |          NA |
|            TypeMapperString | .NET Framework 4.7.2 |  22.9374 ns |  23.4457 ns |  24.32 |      - |         - |          NA |
|          DirectAccessString | .NET Framework 4.7.2 |   0.8584 ns |   0.8584 ns |   1.00 |      - |         - |          NA |
|   TypeMapperWrappedInstance | .NET Framework 4.7.2 |  81.9643 ns |  81.7871 ns |  95.50 | 0.0050 |      32 B |          NA |
| DirectAccessWrappedInstance | .NET Framework 4.7.2 |   1.3411 ns |   1.3334 ns |   1.56 |      - |         - |          NA |
|     TypeMapperGetEnumerator | .NET Framework 4.7.2 | 205.2163 ns | 204.7281 ns | 238.86 | 0.0088 |      56 B |          NA |
|   DirectAccessGetEnumerator | .NET Framework 4.7.2 | 180.4887 ns | 180.5418 ns | 210.25 | 0.0088 |      56 B |          NA |
