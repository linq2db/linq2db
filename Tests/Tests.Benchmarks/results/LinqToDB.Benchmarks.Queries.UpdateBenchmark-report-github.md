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
| LinqSet            | .NET 8.0             | 200,306.6 ns |   86864 B |
| LinqObject         | .NET 8.0             | 152,654.7 ns |   76800 B |
| Object             | .NET 8.0             |  15,927.6 ns |   22752 B |
| CompiledLinqSet    | .NET 8.0             |  78,557.0 ns |   58080 B |
| CompiledLinqObject | .NET 8.0             |  78,231.1 ns |   58080 B |
| RawAdoNet          | .NET 8.0             |     221.3 ns |     712 B |
| LinqSet            | .NET 9.0             | 209,278.6 ns |   86624 B |
| LinqObject         | .NET 9.0             | 148,562.8 ns |   76176 B |
| Object             | .NET 9.0             |  32,962.4 ns |   22704 B |
| CompiledLinqSet    | .NET 9.0             |  51,907.2 ns |   57136 B |
| CompiledLinqObject | .NET 9.0             |  47,933.6 ns |   57136 B |
| RawAdoNet          | .NET 9.0             |     122.0 ns |     712 B |
| LinqSet            | .NET Framework 4.6.2 | 412,672.5 ns |  123836 B |
| LinqObject         | .NET Framework 4.6.2 | 305,056.1 ns |  100182 B |
| Object             | .NET Framework 4.6.2 |  81,123.1 ns |   23236 B |
| CompiledLinqSet    | .NET Framework 4.6.2 | 158,126.7 ns |   67832 B |
| CompiledLinqObject | .NET Framework 4.6.2 |  73,394.6 ns |   67831 B |
| RawAdoNet          | .NET Framework 4.6.2 |     751.3 ns |     810 B |
