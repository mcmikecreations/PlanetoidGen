using PlanetoidGen.Agents.Procedural.Benchmarks.Benchmarks;

// Run only with Release configuration and without debugging
//BenchmarkRunner.Run<ProceduralTileGenerationBenchmark>();

var runner = new ProceduralTileGenerationBenchmark();

await runner.GenerateTile();
