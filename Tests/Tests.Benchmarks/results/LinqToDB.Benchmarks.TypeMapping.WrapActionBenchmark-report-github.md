```

BenchmarkDotNet v0.15.2, Windows 10 (10.0.17763.7553/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X 3.39GHz, 2 CPU, 32 logical and 16 physical cores
.NET SDK 9.0.302
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-FTOCRB : .NET 8.0.18 (8.0.1825.31117), X64 RyuJIT AVX2
  Job-DHTNJT : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-QIENBV : .NET Framework 4.8 (4.8.4795.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                          | Runtime              | Mean       | Allocated |
|-------------------------------- |--------------------- |-----------:|----------:|
| TypeMapperAction                | .NET 8.0             |  2.8458 ns |         - |
| DirectAccessAction              | .NET 8.0             |  0.4378 ns |         - |
| TypeMapperActionWithCast        | .NET 8.0             |  2.8087 ns |         - |
| DirectAccessActionWithCast      | .NET 8.0             |  0.3542 ns |         - |
| TypeMapperActionWithParameter   | .NET 8.0             |  2.6835 ns |         - |
| DirectAccessActionWithParameter | .NET 8.0             |  0.4341 ns |         - |
| TypeMapperAction                | .NET 9.0             |  1.2369 ns |         - |
| DirectAccessAction              | .NET 9.0             |  1.8133 ns |         - |
| TypeMapperActionWithCast        | .NET 9.0             |  0.0009 ns |         - |
| DirectAccessActionWithCast      | .NET 9.0             |  0.4443 ns |         - |
| TypeMapperActionWithParameter   | .NET 9.0             |  1.7421 ns |         - |
| DirectAccessActionWithParameter | .NET 9.0             |  0.2427 ns |         - |
| TypeMapperAction                | .NET Framework 4.6.2 | 19.9105 ns |         - |
| DirectAccessAction              | .NET Framework 4.6.2 |  0.4165 ns |         - |
| TypeMapperActionWithCast        | .NET Framework 4.6.2 | 15.4602 ns |         - |
| DirectAccessActionWithCast      | .NET Framework 4.6.2 |  0.9442 ns |         - |
| TypeMapperActionWithParameter   | .NET Framework 4.6.2 | 14.1197 ns |         - |
| DirectAccessActionWithParameter | .NET Framework 4.6.2 |  0.4180 ns |         - |
