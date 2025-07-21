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
| Method       | Runtime              | Mean      | Allocated |
|------------- |--------------------- |----------:|----------:|
| TypeMapper   | .NET 8.0             |  41.23 ns |      24 B |
| DirectAccess | .NET 8.0             |  46.40 ns |      24 B |
| TypeMapper   | .NET 9.0             |  42.15 ns |      24 B |
| DirectAccess | .NET 9.0             |  48.25 ns |      24 B |
| TypeMapper   | .NET Framework 4.6.2 | 147.03 ns |      24 B |
| DirectAccess | .NET Framework 4.6.2 |  39.45 ns |      24 B |
