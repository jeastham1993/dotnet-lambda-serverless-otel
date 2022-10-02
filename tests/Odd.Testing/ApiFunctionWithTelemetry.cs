using System.Diagnostics;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using ApiFrontend;
using Moq;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Shared.Messaging;

namespace Odd.Testing;

public class ApiFunctionWithTelemetry
{
    private const string TestTracerName = "odd-sample";
    public readonly TracerProvider TracerProvider;
    public ActivitySource TestTracer { get; set; }
    public string TestRunId = Guid.NewGuid().ToString();
    public Function FunctionUnderTest { get; private set; }
    
    public ApiFunctionWithTelemetry(List<Activity> spans)
    {
        TracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(TestTracerName)
            .AddProcessor(new TestRunSpanProcessor())
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(TestTracerName))
            .AddInMemoryExporter(spans)
            .AddConsoleExporter()
            .Build();

        TestTracer = new ActivitySource(TestTracerName);

        FunctionUnderTest = new Function(new Mock<IAmazonSQS>().Object, new Mock<IAmazonSimpleNotificationService>().Object, TracerProvider);
    }
}

public class TestRunSpanProcessor : BaseProcessor<Activity>
{
    public static string TestRunId { get; } = Guid.NewGuid().ToString();

    public override void OnStart(Activity data)
    {
        data?.SetTag("test.run_id", TestRunId);
    }
}