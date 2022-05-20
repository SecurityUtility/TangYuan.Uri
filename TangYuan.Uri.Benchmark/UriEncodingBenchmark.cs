using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Encodings.Web;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace TangYuan.Uri.Benchmark;

[SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 2, targetCount: 5, invocationCount: 200000)]
public class UriEncodingBenchmark
{
    private string? _data;
    private readonly UrlEncoder _urlEncoder = UrlEncoder.Default;

    [Params(512)]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public int Length { get; set; }

    [IterationSetup]
    public void InitializeData()
    {
        _data = CreateRandomString(Length);
    }

    [Benchmark]
    public string UsingUriEncoding() => UriEncoding.Encode(_data!);

    [Benchmark]
    public string UsingSystemEncoding() => _urlEncoder.Encode(_data!);

    private string CreateRandomString(int length, double percentOfNonAsciiCode = 0.3)
    {   
        var random = new Random();
        var builder = new StringBuilder(length + 1);
        for (int i = 0; i < length; ++i)
        {
            builder.Append(random.NextDouble() > percentOfNonAsciiCode
                ? random.Next(32, 127)
                : random.Next(0x4e00, 0x9fff));
        }

        return builder.ToString();
    }
}