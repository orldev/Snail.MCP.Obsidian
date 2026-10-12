using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Snail.MCP.Vault.Benchmarks.SchemeBenchmarks).Assembly).Run(args);
