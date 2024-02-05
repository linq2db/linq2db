```

BenchmarkDotNet v0.13.9+228a464e8be6c580ad9408e98f18813f6407fb5a, Windows 10 (10.0.17763.4644/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 7.0.401
  [Host]     : .NET 7.0.11 (7.0.1123.42427), X64 RyuJIT AVX2
  Job-DAXXNM : .NET 6.0.22 (6.0.2223.42425), X64 RyuJIT AVX2
  Job-SLTPYD : .NET 7.0.11 (7.0.1123.42427), X64 RyuJIT AVX2
  Job-YOWJJJ : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-OZLLFF : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method             | Runtime              | Mean         | Allocated |
|------------------- |--------------------- |-------------:|----------:|
| LinqSet            | .NET 6.0             | 290,572.9 ns |   71232 B |
| LinqObject         | .NET 6.0             | 176,275.0 ns |   38624 B |
| Object             | .NET 6.0             |  13,167.8 ns |    8272 B |
| CompiledLinqSet    | .NET 6.0             |  11,759.3 ns |    8032 B |
| CompiledLinqObject | .NET 6.0             |  11,934.0 ns |    8032 B |
| RawAdoNet          | .NET 6.0             |     268.8 ns |     712 B |
| LinqSet            | .NET 7.0             | 194,819.3 ns |   42240 B |
| LinqObject         | .NET 7.0             | 144,965.3 ns |   32256 B |
| Object             | .NET 7.0             |  12,525.0 ns |    8272 B |
| CompiledLinqSet    | .NET 7.0             |  12,127.4 ns |    8032 B |
| CompiledLinqObject | .NET 7.0             |  11,439.8 ns |    8032 B |
| RawAdoNet          | .NET 7.0             |     240.8 ns |     712 B |
| LinqSet            | .NET Core 3.1        | 214,981.1 ns |   70080 B |
| LinqObject         | .NET Core 3.1        | 219,703.6 ns |   38560 B |
| Object             | .NET Core 3.1        |  16,167.8 ns |    8304 B |
| CompiledLinqSet    | .NET Core 3.1        |  15,460.1 ns |    8000 B |
| CompiledLinqObject | .NET Core 3.1        |  17,152.1 ns |    8000 B |
| RawAdoNet          | .NET Core 3.1        |     252.7 ns |     712 B |
| LinqSet            | .NET Framework 4.7.2 | 475,891.3 ns |   79746 B |
| LinqObject         | .NET Framework 4.7.2 | 287,863.4 ns |   47910 B |
| Object             | .NET Framework 4.7.2 |  22,577.7 ns |    8682 B |
| CompiledLinqSet    | .NET Framework 4.7.2 |  18,553.8 ns |    8216 B |
| CompiledLinqObject | .NET Framework 4.7.2 |  19,421.0 ns |    8216 B |
| RawAdoNet          | .NET Framework 4.7.2 |     764.8 ns |     810 B |
