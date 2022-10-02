using System.Diagnostics;
using System.Runtime.CompilerServices;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using FluentAssertions;
using Moq;
using OpenTelemetry.Trace;

namespace Odd.Testing;

public class ApiTests : IDisposable
{
    private static readonly List<Activity> CollectedSpans = new List<Activity>();
    private Activity TestSpan;
    private ApiFunctionWithTelemetry _function;
    public Task InitializeAsync()
    {
        CollectedSpans.RemoveAll(x => true);
        return Task.CompletedTask;
    }

    public ApiTests()
    {
        _function = new ApiFunctionWithTelemetry(CollectedSpans);
        
        TestSpan = _function.TestTracer.StartActivity("Test started");
    }

    [Fact]
    public async Task ApiGet_WithValidContents_ShouldProduceTelemetry()
    {
        var result = await _function.FunctionUnderTest.FunctionHandler(new Mock<APIGatewayProxyRequest>().Object,
            new Mock<ILambdaContext>().Object);

        result.Should().NotBeNull();
        result.StatusCode.Should().Be(200);

        var snsPublish = CollectedSpans.FirstOrDefault(span => span.DisplayName == "SNSPublish");
        var sqsEnqueue = CollectedSpans.FirstOrDefault(span => span.DisplayName == "SQSSendMessage");

        snsPublish.Should().NotBeNull();
        sqsEnqueue.Should().NotBeNull();
        
        var publishedMessageValue = snsPublish.Tags.FirstOrDefault(tag => tag.Key == "messaging.contents").Value;

        publishedMessageValue.Should().Be("\"Traced hello world\"");
    }

    public void Dispose()
    {
        TestSpan.Dispose();
        
        _function.TracerProvider.ForceFlush();
    }
}