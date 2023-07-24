``` ini

BenchmarkDotNet=v0.13.5, OS=Windows 10 (10.0.17763.4010/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.201
  [Host]     : .NET 7.0.3 (7.0.323.6910), X64 RyuJIT AVX2
  Job-ZOLDKB : .NET 6.0.14 (6.0.1423.7309), X64 RyuJIT AVX2
  Job-EHWHZK : .NET 7.0.3 (7.0.323.6910), X64 RyuJIT AVX2
  Job-LWJRKG : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-AGOWOF : .NET Framework 4.8 (4.8.4614.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|             Method |              Runtime |         Mean | Allocated |
|------------------- |--------------------- |-------------:|----------:|
|            LinqSet |             .NET 6.0 | 284,455.9 ns |   69185 B |
|         LinqObject |             .NET 6.0 | 159,377.4 ns |   38689 B |
|             Object |             .NET 6.0 |  13,189.6 ns |    8288 B |
|    CompiledLinqSet |             .NET 6.0 |  12,090.9 ns |    8048 B |
| CompiledLinqObject |             .NET 6.0 |  12,371.9 ns |    8048 B |
|          RawAdoNet |             .NET 6.0 |     253.8 ns |     712 B |
|            LinqSet |             .NET 7.0 | 194,793.6 ns |   42241 B |
|         LinqObject |             .NET 7.0 | 134,076.7 ns |   32544 B |
|             Object |             .NET 7.0 |   9,413.9 ns |    8288 B |
|    CompiledLinqSet |             .NET 7.0 |  11,670.4 ns |    8048 B |
| CompiledLinqObject |             .NET 7.0 |  10,021.8 ns |    8048 B |
|          RawAdoNet |             .NET 7.0 |     123.2 ns |     712 B |
|            LinqSet |        .NET Core 3.1 | 348,184.8 ns |   69793 B |
|         LinqObject |        .NET Core 3.1 | 215,939.3 ns |   38273 B |
|             Object |        .NET Core 3.1 |  18,651.8 ns |    8320 B |
|    CompiledLinqSet |        .NET Core 3.1 |  15,785.0 ns |    8016 B |
| CompiledLinqObject |        .NET Core 3.1 |  16,560.5 ns |    8016 B |
|          RawAdoNet |        .NET Core 3.1 |     264.2 ns |     712 B |
|            LinqSet | .NET Framework 4.7.2 | 467,928.1 ns |   79426 B |
|         LinqObject | .NET Framework 4.7.2 | 287,043.5 ns |   48365 B |
|             Object | .NET Framework 4.7.2 |  22,060.4 ns |    8698 B |
|    CompiledLinqSet | .NET Framework 4.7.2 |  18,414.8 ns |    8232 B |
| CompiledLinqObject | .NET Framework 4.7.2 |  19,388.7 ns |    8232 B |
|          RawAdoNet | .NET Framework 4.7.2 |     778.7 ns |     810 B |
