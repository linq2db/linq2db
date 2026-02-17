```

BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6456/22H2/2022Update)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v2
  Job-IDPIOT : .NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v2
  Job-FTOCRB : .NET 8.0.23 (8.0.23, 8.0.2325.60607), X64 RyuJIT x86-64-v2
  Job-DHTNJT : .NET 9.0.12 (9.0.12, 9.0.1225.60609), X64 RyuJIT x86-64-v2
  Job-QIENBV : .NET Framework 4.8.1 (4.8.9310.0), X64 RyuJIT VectorSize=128

Jit=RyuJit  Platform=X64  

```
| Method | Runtime              | Mean     | Allocated |
|------- |--------------------- |---------:|----------:|
| Select | .NET 10.0            | 39.47 ms |   1.19 MB |
| Select | .NET 8.0             | 31.85 ms |   1.22 MB |
| Select | .NET 9.0             | 26.41 ms |   1.21 MB |
| Select | .NET Framework 4.6.2 | 54.31 ms |   2.75 MB |
