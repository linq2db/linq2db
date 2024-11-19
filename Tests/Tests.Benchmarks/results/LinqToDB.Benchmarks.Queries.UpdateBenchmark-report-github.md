```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.17763.5696/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.204
  [Host]     : .NET 8.0.4 (8.0.424.16909), X64 RyuJIT AVX2
  Job-VZLGGZ : .NET 6.0.29 (6.0.2924.17105), X64 RyuJIT AVX2
  Job-AZKKUX : .NET 8.0.4 (8.0.424.16909), X64 RyuJIT AVX2
  Job-TQCFWV : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method             | Runtime              | Mean         | Allocated |
|------------------- |--------------------- |-------------:|----------:|
| LinqSet            | .NET 6.0             | 181,833.0 ns |   55520 B |
| LinqObject         | .NET 6.0             |  98,785.8 ns |   30672 B |
| Object             | .NET 6.0             |  13,999.3 ns |    8304 B |
| CompiledLinqSet    | .NET 6.0             |  11,902.7 ns |    8064 B |
| CompiledLinqObject | .NET 6.0             |  11,568.0 ns |    8064 B |
| RawAdoNet          | .NET 6.0             |     265.7 ns |     712 B |
| LinqSet            | .NET 8.0             |  99,622.9 ns |   37856 B |
| LinqObject         | .NET 8.0             |  61,979.0 ns |   27504 B |
| Object             | .NET 8.0             |   8,845.6 ns |    8304 B |
| CompiledLinqSet    | .NET 8.0             |   7,337.6 ns |    8064 B |
| CompiledLinqObject | .NET 8.0             |   8,354.8 ns |    8064 B |
| RawAdoNet          | .NET 8.0             |     224.2 ns |     712 B |
| LinqSet            | .NET Framework 4.6.2 | 324,804.3 ns |   67005 B |
| LinqObject         | .NET Framework 4.6.2 | 169,378.1 ns |   40947 B |
| Object             | .NET Framework 4.6.2 |  23,719.6 ns |    8714 B |
| CompiledLinqSet    | .NET Framework 4.6.2 |  18,055.6 ns |    8248 B |
| CompiledLinqObject | .NET Framework 4.6.2 |  19,874.8 ns |    8248 B |
| RawAdoNet          | .NET Framework 4.6.2 |     793.7 ns |     810 B |
