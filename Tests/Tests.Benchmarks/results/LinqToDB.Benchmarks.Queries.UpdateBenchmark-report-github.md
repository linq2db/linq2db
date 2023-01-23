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
|             Method |              Runtime |         Mean | Allocated |
|------------------- |--------------------- |-------------:|----------:|
|            LinqSet |             .NET 6.0 | 263,504.1 ns |   69057 B |
|         LinqObject |             .NET 6.0 | 173,950.8 ns |   38688 B |
|             Object |             .NET 6.0 |  13,453.5 ns |    8288 B |
|    CompiledLinqSet |             .NET 6.0 |  11,588.0 ns |    8048 B |
| CompiledLinqObject |             .NET 6.0 |  12,438.2 ns |    8048 B |
|          RawAdoNet |             .NET 6.0 |     259.3 ns |     712 B |
|            LinqSet |             .NET 7.0 | 174,631.3 ns |   42017 B |
|         LinqObject |             .NET 7.0 | 127,053.0 ns |   32400 B |
|             Object |             .NET 7.0 |  13,713.8 ns |    8288 B |
|    CompiledLinqSet |             .NET 7.0 |  11,321.5 ns |    8048 B |
| CompiledLinqObject |             .NET 7.0 |  11,655.7 ns |    8048 B |
|          RawAdoNet |             .NET 7.0 |     248.9 ns |     712 B |
|            LinqSet |        .NET Core 3.1 | 371,213.5 ns |   71202 B |
|         LinqObject |        .NET Core 3.1 | 200,876.8 ns |   39200 B |
|             Object |        .NET Core 3.1 |  17,132.9 ns |    8320 B |
|    CompiledLinqSet |        .NET Core 3.1 |  16,117.7 ns |    8016 B |
| CompiledLinqObject |        .NET Core 3.1 |  16,874.3 ns |    8016 B |
|          RawAdoNet |        .NET Core 3.1 |     282.9 ns |     712 B |
|            LinqSet | .NET Framework 4.7.2 | 427,996.3 ns |   77106 B |
|         LinqObject | .NET Framework 4.7.2 | 250,268.0 ns |   47977 B |
|             Object | .NET Framework 4.7.2 |  22,454.7 ns |    8698 B |
|    CompiledLinqSet | .NET Framework 4.7.2 |  18,786.6 ns |    8232 B |
| CompiledLinqObject | .NET Framework 4.7.2 |  18,940.1 ns |    8232 B |
|          RawAdoNet | .NET Framework 4.7.2 |     749.8 ns |     810 B |
