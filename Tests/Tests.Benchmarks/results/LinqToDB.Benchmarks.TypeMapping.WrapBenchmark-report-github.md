``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-HCNGBR : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-XBFFOD : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-INBZNN : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-THZJXI : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                      Method |              Runtime |        Mean | Allocated |
|---------------------------- |--------------------- |------------:|----------:|
|            TypeMapperString |             .NET 6.0 |   6.4069 ns |         - |
|          DirectAccessString |             .NET 6.0 |   0.6780 ns |         - |
|   TypeMapperWrappedInstance |             .NET 6.0 |  50.0990 ns |      32 B |
| DirectAccessWrappedInstance |             .NET 6.0 |   0.8022 ns |         - |
|     TypeMapperGetEnumerator |             .NET 6.0 |  60.6987 ns |      32 B |
|   DirectAccessGetEnumerator |             .NET 6.0 |  57.2523 ns |      32 B |
|            TypeMapperString |             .NET 7.0 |   6.9989 ns |         - |
|          DirectAccessString |             .NET 7.0 |   3.4882 ns |         - |
|   TypeMapperWrappedInstance |             .NET 7.0 |  43.8514 ns |      32 B |
| DirectAccessWrappedInstance |             .NET 7.0 |   1.8648 ns |         - |
|     TypeMapperGetEnumerator |             .NET 7.0 |  53.9062 ns |      32 B |
|   DirectAccessGetEnumerator |             .NET 7.0 |  48.5600 ns |      32 B |
|            TypeMapperString |        .NET Core 3.1 |   5.4044 ns |         - |
|          DirectAccessString |        .NET Core 3.1 |   0.8721 ns |         - |
|   TypeMapperWrappedInstance |        .NET Core 3.1 |  58.4667 ns |      32 B |
| DirectAccessWrappedInstance |        .NET Core 3.1 |   0.9353 ns |         - |
|     TypeMapperGetEnumerator |        .NET Core 3.1 | 134.5420 ns |      32 B |
|   DirectAccessGetEnumerator |        .NET Core 3.1 | 121.2557 ns |      32 B |
|            TypeMapperString | .NET Framework 4.7.2 |  23.7669 ns |         - |
|          DirectAccessString | .NET Framework 4.7.2 |   1.3048 ns |         - |
|   TypeMapperWrappedInstance | .NET Framework 4.7.2 |  88.4365 ns |      32 B |
| DirectAccessWrappedInstance | .NET Framework 4.7.2 |   0.7600 ns |         - |
|     TypeMapperGetEnumerator | .NET Framework 4.7.2 | 170.9921 ns |      56 B |
|   DirectAccessGetEnumerator | .NET Framework 4.7.2 | 132.4954 ns |      56 B |
