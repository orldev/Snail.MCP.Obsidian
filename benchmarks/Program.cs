using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Snail.MCP.Obsidian.Benchmarks.SchemeBenchmarks).Assembly).Run(args);
