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
|                      Method |              Runtime |        Mean |      Median |  Ratio |   Gen0 | Allocated | Alloc Ratio |
|---------------------------- |--------------------- |------------:|------------:|-------:|-------:|----------:|------------:|
|            TypeMapperString |             .NET 6.0 |   6.0121 ns |   6.0129 ns |   7.03 |      - |         - |          NA |
|          DirectAccessString |             .NET 6.0 |   1.3720 ns |   1.3693 ns |   1.60 |      - |         - |          NA |
|   TypeMapperWrappedInstance |             .NET 6.0 |  47.4360 ns |  47.9967 ns |  55.46 | 0.0019 |      32 B |          NA |
| DirectAccessWrappedInstance |             .NET 6.0 |   0.8389 ns |   0.8974 ns |   0.94 |      - |         - |          NA |
|     TypeMapperGetEnumerator |             .NET 6.0 |  54.6594 ns |  54.9535 ns |  63.89 | 0.0019 |      32 B |          NA |
|   DirectAccessGetEnumerator |             .NET 6.0 |  56.0144 ns |  55.6272 ns |  65.41 | 0.0019 |      32 B |          NA |
|            TypeMapperString |             .NET 7.0 |   7.0588 ns |   7.1073 ns |   8.24 |      - |         - |          NA |
|          DirectAccessString |             .NET 7.0 |   1.8652 ns |   1.8788 ns |   2.18 |      - |         - |          NA |
|   TypeMapperWrappedInstance |             .NET 7.0 |  47.6925 ns |  47.7810 ns |  55.73 | 0.0019 |      32 B |          NA |
| DirectAccessWrappedInstance |             .NET 7.0 |   1.6439 ns |   1.5173 ns |   1.95 |      - |         - |          NA |
|     TypeMapperGetEnumerator |             .NET 7.0 |  60.6385 ns |  59.6502 ns |  71.02 | 0.0019 |      32 B |          NA |
|   DirectAccessGetEnumerator |             .NET 7.0 |  51.7974 ns |  51.7793 ns |  60.54 | 0.0019 |      32 B |          NA |
|            TypeMapperString |        .NET Core 3.1 |   5.4218 ns |   5.4222 ns |   6.34 |      - |         - |          NA |
|          DirectAccessString |        .NET Core 3.1 |   0.8330 ns |   0.8712 ns |   0.98 |      - |         - |          NA |
|   TypeMapperWrappedInstance |        .NET Core 3.1 |  55.8161 ns |  55.9613 ns |  65.27 | 0.0019 |      32 B |          NA |
| DirectAccessWrappedInstance |        .NET Core 3.1 |   0.7854 ns |   0.7853 ns |   0.92 |      - |         - |          NA |
|     TypeMapperGetEnumerator |        .NET Core 3.1 | 127.6941 ns | 127.5229 ns | 149.61 | 0.0019 |      32 B |          NA |
|   DirectAccessGetEnumerator |        .NET Core 3.1 |  72.5073 ns |  72.5155 ns |  84.75 | 0.0019 |      32 B |          NA |
|            TypeMapperString | .NET Framework 4.7.2 |  23.1627 ns |  23.3848 ns |  26.93 |      - |         - |          NA |
|          DirectAccessString | .NET Framework 4.7.2 |   0.8556 ns |   0.8556 ns |   1.00 |      - |         - |          NA |
|   TypeMapperWrappedInstance | .NET Framework 4.7.2 |  83.7606 ns |  90.4667 ns | 103.18 | 0.0050 |      32 B |          NA |
| DirectAccessWrappedInstance | .NET Framework 4.7.2 |   1.4363 ns |   1.4546 ns |   1.69 |      - |         - |          NA |
|     TypeMapperGetEnumerator | .NET Framework 4.7.2 | 168.9461 ns | 164.9507 ns | 195.47 | 0.0088 |      56 B |          NA |
|   DirectAccessGetEnumerator | .NET Framework 4.7.2 | 139.8308 ns | 138.2848 ns | 160.70 | 0.0088 |      56 B |          NA |
