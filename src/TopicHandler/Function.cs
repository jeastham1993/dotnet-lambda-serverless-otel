using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Amazon.Lambda.SQSEvents;
using OpenTelemetry.Trace;
using Shared;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace TopicHandler
{
    public class Function : SnsTracedFunction<string>
    {
        public override string SERVICE_NAME => "SnsTopicHandler";
        
        public override Func<SNSEvent, ILambdaContext, Task<string>> Handler => FunctionHandler;

        public Function() : base()
        {
        }

        public async Task<string> FunctionHandler(SNSEvent snsEvent, ILambdaContext context)
        {
            foreach (var record in snsEvent.Records)
            {
                var hydratedContext = this.HydrateContextFromMessage(record);

                using var startProcessingSpan = Activity.Current?.Source.StartActivity(
                        $"StartSnsProcessing{record.Sns.MessageId}", ActivityKind.Consumer,
                        parentContext: hydratedContext)?
                    .AddSnsAttributes(record.Sns);

                await Task.Delay(1000);

                startProcessingSpan?.Stop();
                
                TracerProvider.Default.ForceFlush();
            }

            return "OK";
        }
    }
}