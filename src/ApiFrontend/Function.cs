using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.Xml;
using System.Text.Json;
using System.Threading;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using OpenTelemetry.Trace;
using Shared;
using Shared.Messaging;
using MessageAttributeValue = Amazon.SQS.Model.MessageAttributeValue;

namespace ApiFrontend
{
    public class Function : ApiGatewayTracedFunction
    {
        private readonly IQueuing _queuing;
        private readonly IPublisher _messagePublisher;
        private readonly HttpClient _client = new HttpClient();
        
        public Function() : base()
        {
            _queuing = new SqsQueuing(new AmazonSQSClient());
            _messagePublisher = new SnsPublisher(new AmazonSimpleNotificationServiceClient());
        }

        internal Function(IAmazonSQS queuing, IAmazonSimpleNotificationService messagePublisher, TracerProvider provider) : base(provider)
        {
            _queuing = new SqsQueuing(queuing);
            this._messagePublisher = new SnsPublisher(messagePublisher);
        }

        public override string SERVICE_NAME => "ApiFrontend";
        
        public override Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>> Handler =>
            FunctionHandler;

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent,
            ILambdaContext context)
        {
            var location = await GetCallingIP();
            var body = new Dictionary<string, string>
            {
                {"message", "hello world"},
                {"location", location}
            };

            var message = new MessageWrapper<string>()
            {
                Data = "Traced hello world",
                Metadata = new MessageMetadata()
            };

            var tasks = new Task[2]
            {
                this._queuing.Enqueue(Environment.GetEnvironmentVariable("QUEUE_URL"), message),
                this._messagePublisher.Publish(Environment.GetEnvironmentVariable("TOPIC_ARN"), message)
            };

            Task.WaitAll(tasks);

            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(body),
                StatusCode = 200,
                Headers = new Dictionary<string, string> {{"Content-Type", "application/json"}}
            };
        }

        private async Task<string> GetCallingIP()
        {
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Add("User-Agent", "AWS Lambda .Net Client");

            var msg = await _client.GetStringAsync("http://checkip.amazonaws.com/")
                .ConfigureAwait(continueOnCapturedContext: false);

            return msg.Replace("\n", "");
        }
        
        public static (string filepath, int lineno, string function)
            CodeInfo(
                [CallerFilePath] string filePath = "",
                [CallerLineNumber] int lineno = -1,
                [CallerMemberName] string function = "")
            => (filePath, lineno, function);
    }
}