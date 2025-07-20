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
| TypeMapperString    | .NET 8.0             |  1.6312 ns |         - |
| DirectAccessString  | .NET 8.0             |  3.7519 ns |         - |
| TypeMapperInt       | .NET 8.0             |  0.0390 ns |         - |
| DirectAccessInt     | .NET 8.0             |  0.3739 ns |         - |
| TypeMapperBoolean   | .NET 8.0             |  3.2176 ns |         - |
| DirectAccessBoolean | .NET 8.0             |  1.3158 ns |         - |
| TypeMapperWrapper   | .NET 8.0             |  6.3984 ns |         - |
| DirectAccessWrapper | .NET 8.0             |  3.7755 ns |         - |
| TypeMapperString    | .NET 9.0             |  5.3867 ns |         - |
| DirectAccessString  | .NET 9.0             |  3.6427 ns |         - |
| TypeMapperInt       | .NET 9.0             |  2.9601 ns |         - |
| DirectAccessInt     | .NET 9.0             |  0.4861 ns |         - |
| TypeMapperBoolean   | .NET 9.0             |  3.2130 ns |         - |
| DirectAccessBoolean | .NET 9.0             |  1.4181 ns |         - |
| TypeMapperWrapper   | .NET 9.0             |  6.0308 ns |         - |
| DirectAccessWrapper | .NET 9.0             |  3.7837 ns |         - |
| TypeMapperString    | .NET Framework 4.6.2 | 18.6737 ns |         - |
| DirectAccessString  | .NET Framework 4.6.2 |  5.1127 ns |         - |
| TypeMapperInt       | .NET Framework 4.6.2 | 22.8762 ns |         - |
| DirectAccessInt     | .NET Framework 4.6.2 |  0.9691 ns |         - |
| TypeMapperBoolean   | .NET Framework 4.6.2 | 22.5369 ns |         - |
| DirectAccessBoolean | .NET Framework 4.6.2 |  1.0443 ns |         - |
| TypeMapperWrapper   | .NET Framework 4.6.2 | 33.4594 ns |         - |
| DirectAccessWrapper | .NET Framework 4.6.2 |  3.1954 ns |         - |
