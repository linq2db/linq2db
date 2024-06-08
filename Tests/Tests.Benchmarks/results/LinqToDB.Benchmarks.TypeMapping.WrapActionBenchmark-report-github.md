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
| Method                          | Runtime              | Mean       | Allocated |
|-------------------------------- |--------------------- |-----------:|----------:|
| TypeMapperAction                | .NET 6.0             |  4.4995 ns |         - |
| DirectAccessAction              | .NET 6.0             |  0.9015 ns |         - |
| TypeMapperActionWithCast        | .NET 6.0             |  4.2104 ns |         - |
| DirectAccessActionWithCast      | .NET 6.0             |  0.5007 ns |         - |
| TypeMapperActionWithParameter   | .NET 6.0             |  6.0296 ns |         - |
| DirectAccessActionWithParameter | .NET 6.0             |  1.5020 ns |         - |
| TypeMapperAction                | .NET 7.0             |  5.1300 ns |         - |
| DirectAccessAction              | .NET 7.0             |  0.3882 ns |         - |
| TypeMapperActionWithCast        | .NET 7.0             |  5.1409 ns |         - |
| DirectAccessActionWithCast      | .NET 7.0             |  0.3569 ns |         - |
| TypeMapperActionWithParameter   | .NET 7.0             |  5.0931 ns |         - |
| DirectAccessActionWithParameter | .NET 7.0             |  0.4658 ns |         - |
| TypeMapperAction                | .NET Core 3.1        |  5.6221 ns |         - |
| DirectAccessAction              | .NET Core 3.1        |  1.2309 ns |         - |
| TypeMapperActionWithCast        | .NET Core 3.1        |  4.0719 ns |         - |
| DirectAccessActionWithCast      | .NET Core 3.1        |  0.4444 ns |         - |
| TypeMapperActionWithParameter   | .NET Core 3.1        |  6.0838 ns |         - |
| DirectAccessActionWithParameter | .NET Core 3.1        |  1.2432 ns |         - |
| TypeMapperAction                | .NET Framework 4.7.2 | 20.5062 ns |         - |
| DirectAccessAction              | .NET Framework 4.7.2 |  0.9056 ns |         - |
| TypeMapperActionWithCast        | .NET Framework 4.7.2 | 14.9391 ns |         - |
| DirectAccessActionWithCast      | .NET Framework 4.7.2 |  0.8904 ns |         - |
| TypeMapperActionWithParameter   | .NET Framework 4.7.2 | 23.2307 ns |         - |
| DirectAccessActionWithParameter | .NET Framework 4.7.2 |  0.8837 ns |         - |
