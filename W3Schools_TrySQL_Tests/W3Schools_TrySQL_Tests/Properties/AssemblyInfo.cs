using NUnit.Framework;

//ParallelScope
[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelism(16)]
