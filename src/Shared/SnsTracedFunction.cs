using System.Diagnostics;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;

namespace Shared;

public abstract class SnsTracedFunction<TResponse> : TracedFunction<SNSEvent, TResponse>
{
    public override Func<SNSEvent, ILambdaContext, bool> ContextPropagator => SnsPropogator;
    
    public override Func<SNSEvent, Activity, bool> AddRequestAttributes => SnsRequestAttributeLoader;
    
    public override Func<TResponse, Activity, bool> AddResponseAttributes => SnsResponseAttributeLoader;

    private bool SnsPropogator(SNSEvent arg, ILambdaContext context)
    {
        this.Context = new ActivityContext();

        return true;
    }
    
    private bool SnsRequestAttributeLoader(SNSEvent arg, Activity activity)
    {
        activity.AddTag("faas.trigger", "pubsub");
        activity.AddTag("messaging.operation", "process");
        activity.AddTag("messaging.system", "AmazonSNS");
        activity.AddTag("messaging.destination_kind", "topic");

        return true;
    }
    
    private bool SnsResponseAttributeLoader(TResponse arg, Activity activity)
    {
        return true;
    }

    public ActivityContext HydrateContextFromMessage(SNSEvent.SNSRecord message)
    {
        var body = JsonSerializer.Deserialize<MessageWrapper<dynamic>>(message.Sns.Message);

        var xRayId = Environment.GetEnvironmentVariable("_X_AMZN_TRACE_ID");

        var traceID = xRayId.Replace("Root=1-", "").Replace("-", "").Split(";")[0];
        var spanId = xRayId.Split(';')[1].Replace("Parent=", "");

        var hydratedContext = new ActivityContext(ActivityTraceId.CreateFromString(traceID.AsSpan()),
            ActivitySpanId.CreateFromString(spanId.AsSpan()), ActivityTraceFlags.Recorded);

        return hydratedContext;
    }
}