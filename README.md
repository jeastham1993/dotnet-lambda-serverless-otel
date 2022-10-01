# Open Telemetry with .NET 6 & AWS Lambda

Implementing Open Telemetry in .NET and AWS Lambda, exporting to [honeycomb.io](https://honeycomb.io/). 

The implementation also includes an abstraction layer for tracing [API Gateway](./src/Shared/ApiGatewayTracedFunction.cs), [SNS](./src/Shared/SnsTracedFunction.cs) and [SNS](./src/Shared/SqsTracedFunction.cs) to easily add tracing regardless of event source. This deals with the intricacies of the different sources whilst providing a common, single place to configure OTEL.

The application is built using [AWS SAM](https://aws.amazon.com/serverless/sam/). To deploy into your own AWS account run the below command from the repo root.

```
sam deploy --guided
```

## To Do

- [ ] SNS doesn't attach to the correct parent span
- [ ] Retrieve Honeycomb API key from secrets manager
- [ ] Add Event Bridge implementation