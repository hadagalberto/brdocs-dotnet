using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(BrDocs.Benchmarks.DocumentsBenchmarks).Assembly).Run(args);
