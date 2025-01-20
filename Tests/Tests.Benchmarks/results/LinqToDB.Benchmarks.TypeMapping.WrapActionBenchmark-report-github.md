```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.17763.6766/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4775.0), X64 RyuJIT VectorSize=256
  Job-GEKMDY : .NET 6.0.36 (6.0.3624.51421), X64 RyuJIT AVX2
  Job-WEIMGV : .NET 8.0.12 (8.0.1224.60305), X64 RyuJIT AVX2
  Job-ARZZBJ : .NET 9.0.1 (9.0.124.61010), X64 RyuJIT AVX2
  Job-HBTJES : .NET Framework 4.8 (4.8.4775.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                          | Runtime              | Mean       | Allocated |
|-------------------------------- |--------------------- |-----------:|----------:|
| TypeMapperAction                | .NET 6.0             |  5.0173 ns |         - |
| DirectAccessAction              | .NET 6.0             |  0.7094 ns |         - |
| TypeMapperActionWithCast        | .NET 6.0             |  0.9016 ns |         - |
| DirectAccessActionWithCast      | .NET 6.0             |  0.2084 ns |         - |
| TypeMapperActionWithParameter   | .NET 6.0             |  6.0783 ns |         - |
| DirectAccessActionWithParameter | .NET 6.0             |  0.9123 ns |         - |
| TypeMapperAction                | .NET 8.0             |  3.8622 ns |         - |
| DirectAccessAction              | .NET 8.0             |  0.4478 ns |         - |
| TypeMapperActionWithCast        | .NET 8.0             |  3.0462 ns |         - |
| DirectAccessActionWithCast      | .NET 8.0             |  0.4414 ns |         - |
| TypeMapperActionWithParameter   | .NET 8.0             |  2.4940 ns |         - |
| DirectAccessActionWithParameter | .NET 8.0             |  0.3133 ns |         - |
| TypeMapperAction                | .NET 9.0             |  2.2938 ns |         - |
| DirectAccessAction              | .NET 9.0             |  1.5470 ns |         - |
| TypeMapperActionWithCast        | .NET 9.0             |  0.0000 ns |         - |
| DirectAccessActionWithCast      | .NET 9.0             |  0.4406 ns |         - |
| TypeMapperActionWithParameter   | .NET 9.0             |  2.4165 ns |         - |
| DirectAccessActionWithParameter | .NET 9.0             |  0.4777 ns |         - |
| TypeMapperAction                | .NET Framework 4.6.2 | 23.3693 ns |         - |
| DirectAccessAction              | .NET Framework 4.6.2 |  0.8501 ns |         - |
| TypeMapperActionWithCast        | .NET Framework 4.6.2 | 14.2791 ns |         - |
| DirectAccessActionWithCast      | .NET Framework 4.6.2 |  0.8808 ns |         - |
| TypeMapperActionWithParameter   | .NET Framework 4.6.2 | 23.2273 ns |         - |
| DirectAccessActionWithParameter | .NET Framework 4.6.2 |  0.8631 ns |         - |
