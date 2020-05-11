``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-OGAWJV : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-ZLSLVN : .NET Core 2.1.17 (CoreCLR 4.6.28619.01, CoreFX 4.6.28619.01), X64 RyuJIT
  Job-IOHEYN : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
| Method |       Runtime |     Mean |   Median | Ratio |    Gen 0 | Gen 1 | Gen 2 | Allocated |
|------- |-------------- |---------:|---------:|------:|---------:|------:|------:|----------:|
|   Test |    .NET 4.6.2 | 3.047 ms | 2.821 ms |  1.00 |        - |     - |     - | 659.43 KB |
|   Test | .NET Core 2.1 | 1.990 ms | 1.962 ms |  0.72 | 136.7188 |     - |     - | 564.87 KB |
|   Test | .NET Core 3.1 | 1.633 ms | 1.626 ms |  0.62 | 105.4688 |     - |     - | 431.97 KB |
