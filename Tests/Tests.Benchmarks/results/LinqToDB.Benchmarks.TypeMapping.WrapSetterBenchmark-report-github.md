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
| Method              | Runtime              | Mean       | Allocated |
|-------------------- |--------------------- |-----------:|----------:|
| TypeMapperString    | .NET 8.0             |  6.9944 ns |         - |
| DirectAccessString  | .NET 8.0             |  3.7109 ns |         - |
| TypeMapperInt       | .NET 8.0             |  2.7548 ns |         - |
| DirectAccessInt     | .NET 8.0             |  0.6213 ns |         - |
| TypeMapperBoolean   | .NET 8.0             |  3.1683 ns |         - |
| DirectAccessBoolean | .NET 8.0             |  1.3219 ns |         - |
| TypeMapperWrapper   | .NET 8.0             |  7.9112 ns |         - |
| DirectAccessWrapper | .NET 8.0             |  4.2417 ns |         - |
| TypeMapperString    | .NET 9.0             |  2.5156 ns |         - |
| DirectAccessString  | .NET 9.0             |  3.4409 ns |         - |
| TypeMapperInt       | .NET 9.0             |  3.1974 ns |         - |
| DirectAccessInt     | .NET 9.0             |  0.4507 ns |         - |
| TypeMapperBoolean   | .NET 9.0             |  3.2993 ns |         - |
| DirectAccessBoolean | .NET 9.0             |  1.2170 ns |         - |
| TypeMapperWrapper   | .NET 9.0             |  7.4767 ns |         - |
| DirectAccessWrapper | .NET 9.0             |  5.3892 ns |         - |
| TypeMapperString    | .NET Framework 4.6.2 | 25.5512 ns |         - |
| DirectAccessString  | .NET Framework 4.6.2 |  0.8052 ns |         - |
| TypeMapperInt       | .NET Framework 4.6.2 | 22.8740 ns |         - |
| DirectAccessInt     | .NET Framework 4.6.2 |  1.8265 ns |         - |
| TypeMapperBoolean   | .NET Framework 4.6.2 | 20.4488 ns |         - |
| DirectAccessBoolean | .NET Framework 4.6.2 |  0.6215 ns |         - |
| TypeMapperWrapper   | .NET Framework 4.6.2 | 19.1454 ns |         - |
| DirectAccessWrapper | .NET Framework 4.6.2 |  4.8886 ns |         - |
