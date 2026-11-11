using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace AIResumeAnalyzer.Tests
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("============================================================");
            Console.WriteLine("   AI RESUME ANALYZER - AUTOMATED TEST RUNNER (xUnit)");
            Console.WriteLine("============================================================");
            Console.ResetColor();

            var testClasses = new Type[]
            {
                typeof(PasswordHasherTests),
                typeof(TextExtractorTests),
                typeof(AnalysisEngineTests),
                typeof(DatabaseIntegrationTests)
            };

            int passed = 0;
            int failed = 0;
            var stopwatch = Stopwatch.StartNew();

            foreach (var testClass in testClasses)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Suite: {testClass.Name}");
                Console.ResetColor();

                var methods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => m.GetCustomAttributes(typeof(FactAttribute), inherit: true).Any())
                    .ToList();

                foreach (var method in methods)
                {
                    var instance = Activator.CreateInstance(testClass);
                    try
                    {
                        var methodStopwatch = Stopwatch.StartNew();
                        if (typeof(Task).IsAssignableFrom(method.ReturnType))
                        {
                            var task = (Task)method.Invoke(instance, null)!;
                            await task;
                        }
                        else
                        {
                            method.Invoke(instance, null);
                        }
                        methodStopwatch.Stop();

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("  [PASS] ");
                        Console.ResetColor();
                        Console.WriteLine($"{method.Name} ({methodStopwatch.ElapsedMilliseconds} ms)");
                        passed++;
                    }
                    catch (Exception ex)
                    {
                        var actual = ex is TargetInvocationException tie ? tie.InnerException ?? ex : ex;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("  [FAIL] ");
                        Console.ResetColor();
                        Console.WriteLine($"{method.Name}");
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine($"         {actual.Message}");
                        Console.ResetColor();
                        failed++;
                    }
                }
            }

            stopwatch.Stop();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("------------------------------------------------------------");
            if (failed == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"TEST RESULTS: PASSED!  Total: {passed + failed} | Passed: {passed} | Failed: {failed} ({stopwatch.ElapsedMilliseconds} ms)");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"TEST RESULTS: FAILED!  Total: {passed + failed} | Passed: {passed} | Failed: {failed} ({stopwatch.ElapsedMilliseconds} ms)");
            }
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("------------------------------------------------------------");
            Console.ResetColor();

            return failed == 0 ? 0 : 1;
        }
    }
}
