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
using Shared;
using MessageAttributeValue = Amazon.SQS.Model.MessageAttributeValue;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ApiFrontend
{
    public class Function : ApiGatewayTracedFunction
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly AmazonSQSClient _sqsClient;
        private readonly AmazonSimpleNotificationServiceClient _snsClient;
        private ActivitySource source;
        
        public override string SERVICE_NAME => "ApiFrontend";
        public override Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>> Handler =>
            FunctionHandler;
        public Function() : base()
        {
            _sqsClient = new AmazonSQSClient();
            _snsClient = new AmazonSimpleNotificationServiceClient();
        }

        private static async Task<string> GetCallingIP()
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", "AWS Lambda .Net Client");

            var msg = await client.GetStringAsync("http://checkip.amazonaws.com/")
                .ConfigureAwait(continueOnCapturedContext: false);

            return msg.Replace("\n", "");
        }

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent,
            ILambdaContext context)
        {
            using var timeConsuming = Activity.Current.Source.StartActivity("Starting a time consuming thing");
            var (filepath, lineno, function) = CodeInfo();
            timeConsuming?.AddTag("code.function", function);
            timeConsuming?.AddTag("code.lineno", lineno - 2);
            timeConsuming?.AddTag("code.filepath", filepath);
            
            context.Logger.LogLine(timeConsuming.SpanId.ToString());

            Thread.Sleep(1000);

            var location = await GetCallingIP();
            var body = new Dictionary<string, string>
            {
                {"message", "hello world"},
                {"location", location}
            };

            Thread.Sleep(500);

            var messageBody = JsonSerializer.Serialize(new MessageWrapper<string>()
            {
                Data = "Traced hello world",
                Metadata = new MessageMetadata()
            });

            await this._sqsClient.SendMessageAsync(new SendMessageRequest()
            {
                QueueUrl = Environment.GetEnvironmentVariable("QUEUE_URL"),
                MessageBody = messageBody,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>(1)
                {
                    {
                        "parentspan", new MessageAttributeValue()
                        {
                            StringValue = timeConsuming.SpanId.ToString(),
                            DataType = "String" 
                        }
                    }
                }
            });

            await this._snsClient.PublishAsync(new PublishRequest()
            {
                TopicArn = Environment.GetEnvironmentVariable("TOPIC_ARN"),
                Message = messageBody,
            });

            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(body),
                StatusCode = 200,
                Headers = new Dictionary<string, string> {{"Content-Type", "application/json"}}
            };
        }
        
        public static (string filepath, int lineno, string function)
            CodeInfo(
                [CallerFilePath] string filePath = "",
                [CallerLineNumber] int lineno = -1,
                [CallerMemberName] string function = "")
            => (filePath, lineno, function);
    }
}