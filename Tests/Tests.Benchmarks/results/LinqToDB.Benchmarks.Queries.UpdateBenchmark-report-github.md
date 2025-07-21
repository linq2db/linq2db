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
| Method             | Runtime              | Mean         | Allocated |
|------------------- |--------------------- |-------------:|----------:|
| LinqSet            | .NET 8.0             | 152,315.9 ns |   43008 B |
| LinqObject         | .NET 8.0             | 107,212.6 ns |   32384 B |
| Object             | .NET 8.0             |   9,287.9 ns |    8272 B |
| CompiledLinqSet    | .NET 8.0             |   7,090.3 ns |    8032 B |
| CompiledLinqObject | .NET 8.0             |   7,926.0 ns |    8032 B |
| RawAdoNet          | .NET 8.0             |     218.8 ns |     712 B |
| LinqSet            | .NET 9.0             | 139,537.1 ns |   42512 B |
| LinqObject         | .NET 9.0             | 100,568.3 ns |   32272 B |
| Object             | .NET 9.0             |   6,691.1 ns |    8224 B |
| CompiledLinqSet    | .NET 9.0             |   6,626.8 ns |    7984 B |
| CompiledLinqObject | .NET 9.0             |   5,649.5 ns |    7984 B |
| RawAdoNet          | .NET 9.0             |     162.3 ns |     712 B |
| LinqSet            | .NET Framework 4.6.2 | 459,351.6 ns |   77418 B |
| LinqObject         | .NET Framework 4.6.2 | 252,160.5 ns |   47913 B |
| Object             | .NET Framework 4.6.2 |  20,815.1 ns |    8842 B |
| CompiledLinqSet    | .NET Framework 4.6.2 |  15,919.0 ns |    8264 B |
| CompiledLinqObject | .NET Framework 4.6.2 |  17,294.5 ns |    8265 B |
| RawAdoNet          | .NET Framework 4.6.2 |     344.7 ns |     810 B |
