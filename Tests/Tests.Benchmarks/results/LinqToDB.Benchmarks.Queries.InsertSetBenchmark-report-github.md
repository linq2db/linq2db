``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417996 Hz, Resolution=292.5691 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-OGAWJV : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-ZLSLVN : .NET Core 2.1.18 (CoreCLR 4.6.28801.04, CoreFX 4.6.28802.05), X64 RyuJIT
  Job-IOHEYN : .NET Core 3.1.4 (CoreCLR 4.700.20.20201, CoreFX 4.700.20.22101), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
| Method |       Runtime |     Mean | Ratio |    Gen 0 | Gen 1 | Gen 2 | Allocated |
|------- |-------------- |---------:|------:|---------:|------:|------:|----------:|
|   Test |    .NET 4.6.2 | 2.330 ms |  1.00 |        - |     - |     - | 659.43 KB |
|   Test | .NET Core 2.1 | 1.788 ms |  0.77 | 136.7188 |     - |     - | 564.89 KB |
|   Test | .NET Core 3.1 | 1.639 ms |  0.71 | 105.4688 |     - |     - | 431.99 KB |
