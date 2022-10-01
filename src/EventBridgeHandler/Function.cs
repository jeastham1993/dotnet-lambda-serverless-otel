using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using OpenTelemetry.Trace;
using Shared;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace QueueHandler
{
    public class Function : SqsTracedFunction<string>
    {
        public override string SERVICE_NAME => "QueueHandler";
        
        public override Func<SQSEvent, ILambdaContext, Task<string>> Handler => FunctionHandler;

        public Function() : base()
        {
        }

        public async Task<string> FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
        {
            foreach (var record in sqsEvent.Records)
            {
                var hydratedContext = this.HydrateContextFromMessage(record);
                
                using var startProcessingSpan = Activity.Current?.Source.StartActivity(
                    $"StartMessageProcessing{record.MessageId}", ActivityKind.Consumer, parentContext: hydratedContext)?
                    .AddSqsAttributes(record);

                await Task.Delay(1000);

                startProcessingSpan?.Stop();
                
                TracerProvider.Default.ForceFlush();
            }

            return "OK";
        }
    }
}