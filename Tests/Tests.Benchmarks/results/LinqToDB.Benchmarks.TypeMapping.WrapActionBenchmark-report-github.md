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
| TypeMapperAction                | .NET 8.0             |  2.4230 ns |         - |
| DirectAccessAction              | .NET 8.0             |  0.2773 ns |         - |
| TypeMapperActionWithCast        | .NET 8.0             |  2.2700 ns |         - |
| DirectAccessActionWithCast      | .NET 8.0             |  0.4610 ns |         - |
| TypeMapperActionWithParameter   | .NET 8.0             |  2.7477 ns |         - |
| DirectAccessActionWithParameter | .NET 8.0             |  0.4524 ns |         - |
| TypeMapperAction                | .NET 9.0             |  2.7534 ns |         - |
| DirectAccessAction              | .NET 9.0             |  0.4551 ns |         - |
| TypeMapperActionWithCast        | .NET 9.0             |  2.8089 ns |         - |
| DirectAccessActionWithCast      | .NET 9.0             |  0.3781 ns |         - |
| TypeMapperActionWithParameter   | .NET 9.0             |  2.8479 ns |         - |
| DirectAccessActionWithParameter | .NET 9.0             |  0.2963 ns |         - |
| TypeMapperAction                | .NET Framework 4.6.2 | 22.4665 ns |         - |
| DirectAccessAction              | .NET Framework 4.6.2 |  0.8947 ns |         - |
| TypeMapperActionWithCast        | .NET Framework 4.6.2 | 15.8412 ns |         - |
| DirectAccessActionWithCast      | .NET Framework 4.6.2 |  0.8020 ns |         - |
| TypeMapperActionWithParameter   | .NET Framework 4.6.2 | 22.8099 ns |         - |
| DirectAccessActionWithParameter | .NET Framework 4.6.2 |  0.7097 ns |         - |
