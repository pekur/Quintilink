using BenchmarkDotNet.Attributes;
using System;
using System.Linq;
using System.Text;
using Quintilink.Models;
using Microsoft.VSDiagnostics;

namespace Quintilink.Benchmarks
{
    [MemoryDiagnoser]
    [CPUUsageDiagnoser]
    public class StringOperationsBenchmark
    {
        private byte[] _smallData = null !;
        private byte[] _mediumData = null !;
        private byte[] _largeData = null !;
        [GlobalSetup]
        public void Setup()
        {
            // Small: typical serial command (10 bytes)
            _smallData = new byte[]
            {
                0x02,
                0x41,
                0x42,
                0x43,
                0x0D,
                0x0A,
                0x03,
                0x04,
                0x05,
                0x06
            };
            // Medium: network packet (256 bytes)
            _mediumData = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                _mediumData[i] = (byte)(i % 256);
            }

            // Large: high-frequency stream (1024 bytes)
            _largeData = new byte[1024];
            for (int i = 0; i < 1024; i++)
            {
                _largeData[i] = (byte)(i % 256);
            }
        }

        // Current implementation: BitConverter with Replace
        [Benchmark]
        public string BitConverter_Small()
        {
            return BitConverter.ToString(_smallData).Replace("-", " ");
        }

        [Benchmark]
        public string BitConverter_Medium()
        {
            return BitConverter.ToString(_mediumData).Replace("-", " ");
        }

        [Benchmark]
        public string BitConverter_Large()
        {
            return BitConverter.ToString(_largeData).Replace("-", " ");
        }

        // Old implementation: ToSpacedHex with LINQ
        [Benchmark]
        public string ToSpacedHex_Old_Small()
        {
            return string.Join(" ", _smallData.Select(b => b.ToString("X2")));
        }

        [Benchmark]
        public string ToSpacedHex_Old_Medium()
        {
            return string.Join(" ", _mediumData.Select(b => b.ToString("X2")));
        }

        [Benchmark]
        public string ToSpacedHex_Old_Large()
        {
            return string.Join(" ", _largeData.Select(b => b.ToString("X2")));
        }

        // Optimized implementation: ToSpacedHex with Span
        [Benchmark]
        public string ToSpacedHex_Optimized_Small()
        {
            return MessageDefinition.ToSpacedHex(_smallData);
        }

        [Benchmark]
        public string ToSpacedHex_Optimized_Medium()
        {
            return MessageDefinition.ToSpacedHex(_mediumData);
        }

        [Benchmark]
        public string ToSpacedHex_Optimized_Large()
        {
            return MessageDefinition.ToSpacedHex(_largeData);
        }

        // Optimized implementation: ConvertToReadableAscii with pre-sized StringBuilder
        [Benchmark]
        public string ConvertToReadableAscii_Optimized_Small()
        {
            var sb = new StringBuilder(_smallData.Length * 2);
            foreach (var b in _smallData)
            {
                sb.Append(MacroDefinitions.CollapseByte(b));
            }

            return sb.ToString();
        }

        [Benchmark]
        public string ConvertToReadableAscii_Optimized_Medium()
        {
            var sb = new StringBuilder(_mediumData.Length * 2);
            foreach (var b in _mediumData)
            {
                sb.Append(MacroDefinitions.CollapseByte(b));
            }

            return sb.ToString();
        }

        [Benchmark]
        public string ConvertToReadableAscii_Optimized_Large()
        {
            var sb = new StringBuilder(_largeData.Length * 2);
            foreach (var b in _largeData)
            {
                sb.Append(MacroDefinitions.CollapseByte(b));
            }

            return sb.ToString();
        }

        // Benchmark for ParseHex (hot path in message editor)
        private static string _hexString = "48656C6C6F20576F726C64";  // "Hello World"

        [Benchmark]
        public byte[] ParseHex_Old()
        {
            string clean = MessageDefinition.CompactHex(_hexString);
            return Enumerable.Range(0, clean.Length / 2)
                .Select(i => Convert.ToByte(clean.Substring(i * 2, 2), 16))
                .ToArray();
        }
    }
}