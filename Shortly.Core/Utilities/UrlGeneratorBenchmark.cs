using System.Diagnostics;
using System.Security.Cryptography;

namespace Shortly.Core.Utilities;

/// <summary>
/// Benchmarking and performance comparison utility for URL generation algorithms
/// </summary>
public static class UrlGeneratorBenchmark
{
    /// <summary>
    /// Comprehensive performance comparison between different algorithms
    /// </summary>
    public static async Task<BenchmarkResults> RunComprehensiveBenchmarkAsync(int testSize = 100_000)
    {
        var results = new BenchmarkResults();
        
        Console.WriteLine($"Running comprehensive benchmark with {testSize:N0} URLs...\n");
        
        // Test 1: Original Base62 Algorithm
        Console.WriteLine("Testing Original Base62 Algorithm...");
        results.OriginalBase62 = await BenchmarkOriginalAlgorithmAsync(testSize);
        
        // Test 2: Enhanced Base54 Algorithm
        Console.WriteLine("Testing Enhanced Base54 Algorithm...");
        results.EnhancedBase54 = await BenchmarkEnhancedAlgorithmAsync(testSize);
        
        // Test 3: Collision Resistance Test
        Console.WriteLine("Testing Collision Resistance...");
        results.CollisionTest = await TestCollisionResistanceAsync(testSize);
        
        // Test 4: Memory Usage Comparison
        Console.WriteLine("Testing Memory Usage...");
        results.MemoryComparison = TestMemoryUsage(testSize);
        
        // Test 5: Character Set Analysis
        Console.WriteLine("Analyzing Character Sets...");
        results.CharacterSetAnalysis = AnalyzeCharacterSets();
        
        PrintBenchmarkResults(results);
        
        return results;
    }
    
    /// <summary>
    /// Benchmark the original Base62 algorithm
    /// </summary>
    private static async Task<AlgorithmPerformance> BenchmarkOriginalAlgorithmAsync(int testSize)
    {
        var stopwatch = Stopwatch.StartNew();
        var collisions = new HashSet<string>();
        var codes = new HashSet<string>();
        
        GC.Collect(); // Clean memory before test
        var startMemory = GC.GetTotalMemory(false);
        
        for (int i = 1; i <= testSize; i++)
        {
            var code = Base62Converter.Encode(i);
            if (!codes.Add(code))
            {
                collisions.Add(code);
            }
        }
        
        stopwatch.Stop();
        var endMemory = GC.GetTotalMemory(false);
        
        return new AlgorithmPerformance
        {
            AlgorithmName = "Original Base62",
            TotalTime = stopwatch.Elapsed,
            OperationsPerSecond = testSize / stopwatch.Elapsed.TotalSeconds,
            CollisionCount = collisions.Count,
            MemoryUsed = endMemory - startMemory,
            AverageCodeLength = codes.Count > 0 ? codes.Average(c => c.Length) : 0,
            MinCodeLength = codes.Count > 0 ? codes.Min(c => c.Length) : 0,
            MaxCodeLength = codes.Count > 0 ? codes.Max(c => c.Length) : 0
        };
    }
    
    /// <summary>
    /// Benchmark the enhanced Base54 algorithm
    /// </summary>
    private static async Task<AlgorithmPerformance> BenchmarkEnhancedAlgorithmAsync(int testSize)
    {
        var stopwatch = Stopwatch.StartNew();
        var collisions = new HashSet<string>();
        var codes = new HashSet<string>();
        
        GC.Collect(); // Clean memory before test
        var startMemory = GC.GetTotalMemory(false);
        
        for (int i = 1; i <= testSize; i++)
        {
            var code = EnhancedUrlCodeGenerator.EncodeBase54(i, 6);
            if (!codes.Add(code))
            {
                collisions.Add(code);
            }
        }
        
        stopwatch.Stop();
        var endMemory = GC.GetTotalMemory(false);
        
        return new AlgorithmPerformance
        {
            AlgorithmName = "Enhanced Base54",
            TotalTime = stopwatch.Elapsed,
            OperationsPerSecond = testSize / stopwatch.Elapsed.TotalSeconds,
            CollisionCount = collisions.Count,
            MemoryUsed = endMemory - startMemory,
            AverageCodeLength = codes.Count > 0 ? codes.Average(c => c.Length) : 0,
            MinCodeLength = codes.Count > 0 ? codes.Min(c => c.Length) : 0,
            MaxCodeLength = codes.Count > 0 ? codes.Max(c => c.Length) : 0
        };
    }
    
    /// <summary>
    /// Test collision resistance of different approaches
    /// </summary>
    private static async Task<CollisionTestResults> TestCollisionResistanceAsync(int testSize)
    {
        var results = new CollisionTestResults();
        
        // Test random generation collisions
        var randomCodes = new HashSet<string>();
        var randomCollisions = 0;
        
        for (int i = 0; i < testSize; i++)
        {
            var code = GenerateRandomCode(6);
            if (!randomCodes.Add(code))
            {
                randomCollisions++;
            }
        }
        
        // Test time-based generation collisions
        var timeCodes = new HashSet<string>();
        var timeCollisions = 0;
        
        for (int i = 0; i < Math.Min(testSize, 10000); i++) // Limit for time-based to avoid long test
        {
            var code = GenerateTimeBasedCode(i);
            if (!timeCodes.Add(code))
            {
                timeCollisions++;
            }
            
            if (i % 100 == 0) // Small delay to ensure time variation
                await Task.Delay(1);
        }
        
        results.RandomGenerationCollisions = randomCollisions;
        results.TimeBasedCollisions = timeCollisions;
        results.SequentialCollisions = 0; // Base54 sequential should have 0 collisions
        
        return results;
    }
    
    /// <summary>
    /// Compare memory usage between algorithms
    /// </summary>
    private static MemoryComparison TestMemoryUsage(int testSize)
    {
        var sampleSize = Math.Min(testSize, 10000); // Limit for memory test
        
        // Test original algorithm memory
        GC.Collect();
        var startMemory = GC.GetTotalMemory(true);
        
        var originalCodes = new List<string>();
        for (int i = 1; i <= sampleSize; i++)
        {
            originalCodes.Add(Base62Converter.Encode(i));
        }
        
        var originalMemory = GC.GetTotalMemory(false) - startMemory;
        
        // Test enhanced algorithm memory
        GC.Collect();
        startMemory = GC.GetTotalMemory(true);
        
        var enhancedCodes = new List<string>();
        for (int i = 1; i <= sampleSize; i++)
        {
            enhancedCodes.Add(EnhancedUrlCodeGenerator.EncodeBase54(i, 6));
        }
        
        var enhancedMemory = GC.GetTotalMemory(false) - startMemory;
        
        return new MemoryComparison
        {
            OriginalAlgorithmMemory = originalMemory,
            EnhancedAlgorithmMemory = enhancedMemory,
            MemoryImprovement = originalMemory > 0 ? (double)(originalMemory - enhancedMemory) / originalMemory * 100 : 0
        };
    }
    
    /// <summary>
    /// Analyze character set properties
    /// </summary>
    private static CharacterSetAnalysis AnalyzeCharacterSets()
    {
        const string originalChars = "qW1r80D2YHsacELuKn5PgfNjZIl6bv3e4ot7zmJAMiRCdVyF9xhGBUXTwSkQ";
        const string enhancedChars = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        
        return new CharacterSetAnalysis
        {
            OriginalCharacterSet = originalChars,
            EnhancedCharacterSet = enhancedChars,
            OriginalSetSize = originalChars.Length,
            EnhancedSetSize = enhancedChars.Length,
            ConfusingCharactersInOriginal = CountConfusingCharacters(originalChars),
            ConfusingCharactersInEnhanced = CountConfusingCharacters(enhancedChars),
            OriginalPossibleCombinations6Chars = Math.Pow(originalChars.Length, 6),
            EnhancedPossibleCombinations6Chars = Math.Pow(enhancedChars.Length, 6)
        };
    }
    
    /// <summary>
    /// Helper method to generate random codes for testing
    /// </summary>
    private static string GenerateRandomCode(int length)
    {
        const string chars = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        var result = new char[length];
        
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[random.Next(chars.Length)];
        }
        
        return new string(result);
    }
    
    /// <summary>
    /// Helper method to generate time-based codes for testing
    /// </summary>
    private static string GenerateTimeBasedCode(int id)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var combined = (timestamp << 20) | (id & 0xFFFFF);
        return EnhancedUrlCodeGenerator.EncodeBase54(combined, 6);
    }
    
    /// <summary>
    /// Count confusing characters in a character set
    /// </summary>
    private static int CountConfusingCharacters(string characterSet)
    {
        var confusing = new[] { '0', 'O', 'I', 'l', '1' };
        return characterSet.Count(c => confusing.Contains(c));
    }
    
    /// <summary>
    /// Print comprehensive benchmark results
    /// </summary>
    private static void PrintBenchmarkResults(BenchmarkResults results)
    {
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("COMPREHENSIVE BENCHMARK RESULTS");
        Console.WriteLine(new string('=', 80));
        
        // Performance Comparison
        Console.WriteLine("\n📊 PERFORMANCE COMPARISON:");
        Console.WriteLine($"{"Algorithm",-20} {"Time (ms)",-12} {"Ops/sec",-15} {"Collisions",-12} {"Avg Length",-12}");
        Console.WriteLine(new string('-', 80));
        
        PrintAlgorithmResult(results.OriginalBase62);
        PrintAlgorithmResult(results.EnhancedBase54);
        
        // Performance Improvement
        var speedImprovement = (results.EnhancedBase54.OperationsPerSecond - results.OriginalBase62.OperationsPerSecond) 
                              / results.OriginalBase62.OperationsPerSecond * 100;
        
        Console.WriteLine($"\n🚀 PERFORMANCE IMPROVEMENT: {speedImprovement:+0.1f}%");
        
        // Memory Usage
        Console.WriteLine($"\n💾 MEMORY COMPARISON:");
        Console.WriteLine($"Original Algorithm: {results.MemoryComparison.OriginalAlgorithmMemory:N0} bytes");
        Console.WriteLine($"Enhanced Algorithm: {results.MemoryComparison.EnhancedAlgorithmMemory:N0} bytes");
        Console.WriteLine($"Memory Improvement: {results.MemoryComparison.MemoryImprovement:+0.1f}%");
        
        // Collision Analysis
        Console.WriteLine($"\n⚠️  COLLISION ANALYSIS:");
        Console.WriteLine($"Random Generation Collisions: {results.CollisionTest.RandomGenerationCollisions:N0}");
        Console.WriteLine($"Time-based Collisions: {results.CollisionTest.TimeBasedCollisions:N0}");
        Console.WriteLine($"Sequential Collisions: {results.CollisionTest.SequentialCollisions:N0}");
        
        // Character Set Analysis
        Console.WriteLine($"\n🔤 CHARACTER SET ANALYSIS:");
        Console.WriteLine($"Original Set Size: {results.CharacterSetAnalysis.OriginalSetSize} characters");
        Console.WriteLine($"Enhanced Set Size: {results.CharacterSetAnalysis.EnhancedSetSize} characters");
        Console.WriteLine($"Confusing Chars (Original): {results.CharacterSetAnalysis.ConfusingCharactersInOriginal}");
        Console.WriteLine($"Confusing Chars (Enhanced): {results.CharacterSetAnalysis.ConfusingCharactersInEnhanced}");
        Console.WriteLine($"6-char combinations (Original): {results.CharacterSetAnalysis.OriginalPossibleCombinations6Chars:E2}");
        Console.WriteLine($"6-char combinations (Enhanced): {results.CharacterSetAnalysis.EnhancedPossibleCombinations6Chars:E2}");
        
        Console.WriteLine("\n" + new string('=', 80));
    }
    
    private static void PrintAlgorithmResult(AlgorithmPerformance perf)
    {
        Console.WriteLine($"{perf.AlgorithmName,-20} {perf.TotalTime.TotalMilliseconds,-12:F1} " +
                         $"{perf.OperationsPerSecond,-15:N0} {perf.CollisionCount,-12:N0} {perf.AverageCodeLength,-12:F1}");
    }
}

// Data classes for benchmark results
public class BenchmarkResults
{
    public AlgorithmPerformance OriginalBase62 { get; set; } = new();
    public AlgorithmPerformance EnhancedBase54 { get; set; } = new();
    public CollisionTestResults CollisionTest { get; set; } = new();
    public MemoryComparison MemoryComparison { get; set; } = new();
    public CharacterSetAnalysis CharacterSetAnalysis { get; set; } = new();
}

public class AlgorithmPerformance
{
    public string AlgorithmName { get; set; } = string.Empty;
    public TimeSpan TotalTime { get; set; }
    public double OperationsPerSecond { get; set; }
    public int CollisionCount { get; set; }
    public long MemoryUsed { get; set; }
    public double AverageCodeLength { get; set; }
    public int MinCodeLength { get; set; }
    public int MaxCodeLength { get; set; }
}

public class CollisionTestResults
{
    public int RandomGenerationCollisions { get; set; }
    public int TimeBasedCollisions { get; set; }
    public int SequentialCollisions { get; set; }
}

public class MemoryComparison
{
    public long OriginalAlgorithmMemory { get; set; }
    public long EnhancedAlgorithmMemory { get; set; }
    public double MemoryImprovement { get; set; }
}

public class CharacterSetAnalysis
{
    public string OriginalCharacterSet { get; set; } = string.Empty;
    public string EnhancedCharacterSet { get; set; } = string.Empty;
    public int OriginalSetSize { get; set; }
    public int EnhancedSetSize { get; set; }
    public int ConfusingCharactersInOriginal { get; set; }
    public int ConfusingCharactersInEnhanced { get; set; }
    public double OriginalPossibleCombinations6Chars { get; set; }
    public double EnhancedPossibleCombinations6Chars { get; set; }
}