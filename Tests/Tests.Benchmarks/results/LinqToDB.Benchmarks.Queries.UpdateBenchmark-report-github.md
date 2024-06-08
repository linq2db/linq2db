```

BenchmarkDotNet v0.13.9+228a464e8be6c580ad9408e98f18813f6407fb5a, Windows 10 (10.0.17763.5328/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.101
  [Host]     : .NET 7.0.15 (7.0.1523.57226), X64 RyuJIT AVX2
  Job-KJWIMT : .NET 6.0.26 (6.0.2623.60508), X64 RyuJIT AVX2
  Job-GULBRG : .NET 7.0.15 (7.0.1523.57226), X64 RyuJIT AVX2
  Job-LRGNRQ : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-SJROSW : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method             | Runtime              | Mean         | Allocated |
|------------------- |--------------------- |-------------:|----------:|
| LinqSet            | .NET 6.0             | 284,627.8 ns |   69120 B |
| LinqObject         | .NET 6.0             | 161,909.4 ns |   38272 B |
| Object             | .NET 6.0             |  13,161.8 ns |    8272 B |
| CompiledLinqSet    | .NET 6.0             |  11,903.0 ns |    8032 B |
| CompiledLinqObject | .NET 6.0             |  11,894.6 ns |    8032 B |
| RawAdoNet          | .NET 6.0             |     268.0 ns |     712 B |
| LinqSet            | .NET 7.0             | 190,667.2 ns |   42176 B |
| LinqObject         | .NET 7.0             | 145,363.5 ns |   32096 B |
| Object             | .NET 7.0             |  13,092.0 ns |    8272 B |
| CompiledLinqSet    | .NET 7.0             |   7,614.1 ns |    8032 B |
| CompiledLinqObject | .NET 7.0             |  10,330.2 ns |    8032 B |
| RawAdoNet          | .NET 7.0             |     247.9 ns |     712 B |
| LinqSet            | .NET Core 3.1        | 338,004.4 ns |   74432 B |
| LinqObject         | .NET Core 3.1        | 217,318.3 ns |   38208 B |
| Object             | .NET Core 3.1        |  18,781.8 ns |    8304 B |
| CompiledLinqSet    | .NET Core 3.1        |  15,810.4 ns |    8000 B |
| CompiledLinqObject | .NET Core 3.1        |  16,537.2 ns |    8000 B |
| RawAdoNet          | .NET Core 3.1        |     249.7 ns |     712 B |
| LinqSet            | .NET Framework 4.7.2 | 213,748.6 ns |   78578 B |
| LinqObject         | .NET Framework 4.7.2 | 287,136.3 ns |   48685 B |
| Object             | .NET Framework 4.7.2 |  22,850.4 ns |    8682 B |
| CompiledLinqSet    | .NET Framework 4.7.2 |  18,776.7 ns |    8216 B |
| CompiledLinqObject | .NET Framework 4.7.2 |  19,491.7 ns |    8216 B |
| RawAdoNet          | .NET Framework 4.7.2 |     773.3 ns |     810 B |
