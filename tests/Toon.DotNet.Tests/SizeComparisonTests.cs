// file: tests/ToonFormat.Tests/SizeComparisonTests.cs
using System;
using System.Text.Json;
using Xunit;
using ToonFormat;

namespace ToonFormat.Tests
{
    public class SizeComparisonTests
    {
        [Fact]
        public void SizeComparison_MatchesManualComputation_ForInteger()
        {
            int input = 12345;

            var actual = Toon.SizeComparisonPercentage(input);

            var json = JsonSerializer.Serialize(input);
            var toon = Toon.Encode(input);
            var expected = json.Length == 0
                ? 0m
                : Math.Round(100m - ((decimal)toon.Length * 100m / (decimal)json.Length), 2);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SizeComparison_MatchesManualComputation_ForComplexObject()
        {
            var input = new
            {
                Users = new[]
                {
                    new { Id = 1, Name = "Alice", Role = "admin" },
                    new { Id = 2, Name = "Bob", Role = "user" }
                },
                Count = 2
            };

            var actual = Toon.SizeComparisonPercentage(input);

            var json = JsonSerializer.Serialize(input);
            var toon = Toon.Encode(input);
            var expected = json.Length == 0
                ? 0m
                : Math.Round(100m - ((decimal)toon.Length * 100m / (decimal)json.Length), 2);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SizeComparison_MatchesManualComputation_ForNull()
        {
            object? input = null;

            var actual = Toon.SizeComparisonPercentage(input);

            var json = JsonSerializer.Serialize(input);
            var toon = Toon.Encode(input);
            var expected = json.Length == 0
                ? 0m
                : Math.Round(100m - ((decimal)toon.Length * 100m / (decimal)json.Length), 2);

            Assert.Equal(expected, actual);
        }
    }
}