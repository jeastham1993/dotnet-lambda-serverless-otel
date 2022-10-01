using System.Diagnostics;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

namespace Shared;

public abstract class SqsTracedFunction<TResponse> : TracedFunction<SQSEvent, TResponse>
{
    public override Func<SQSEvent, ILambdaContext, bool> ContextPropagator => SqsPropogator;
    
    public override Func<SQSEvent, Activity, bool> AddRequestAttributes => SqsRequestAttributeLoader;
    
    public override Func<TResponse, Activity, bool> AddResponseAttributes => SqsResponseAttributeLoader;

    private bool SqsPropogator(SQSEvent arg, ILambdaContext context)
    {
        this.Context = new ActivityContext();
        return true;
    }
    
    private bool SqsRequestAttributeLoader(SQSEvent arg, Activity activity)
    {
        activity.AddTag("faas.trigger", "pubsub");
        activity.AddTag("messaging.operation", "process");
        activity.AddTag("messaging.system", "AmazonSQS");
        activity.AddTag("messaging.destination_kind", "queue");

        return true;
    }
    
    private bool SqsResponseAttributeLoader(TResponse arg, Activity activity)
    {
        return true;
    }

    public ActivityContext HydrateContextFromMessage(SQSEvent.SQSMessage message)
    {
        if (!message.Attributes.ContainsKey("AWSTraceHeader"))
        {
            return new ActivityContext();
        }

        var attributeValue = message.Attributes["AWSTraceHeader"];

        var traceID = attributeValue.Replace("Root=1-", "").Replace("-", "").Split(";")[0];
        var spanId = attributeValue.Split(';')[1].Replace("Parent=", "");

        var hydratedContext = new ActivityContext(ActivityTraceId.CreateFromString(traceID.AsSpan()),
            ActivitySpanId.CreateFromString(spanId.AsSpan()), ActivityTraceFlags.Recorded);

        return hydratedContext;
    }
}