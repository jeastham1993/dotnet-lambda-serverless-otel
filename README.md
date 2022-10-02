# Open Telemetry with .NET 6 & AWS Lambda

Implementing Open Telemetry in .NET and AWS Lambda, exporting to [honeycomb.io](https://honeycomb.io/). 

The implementation also includes an abstraction layer for tracing [API Gateway](./src/Shared/ApiGatewayTracedFunction.cs), [SNS](./src/Shared/SnsTracedFunction.cs) and [SNS](./src/Shared/SqsTracedFunction.cs) to easily add tracing regardless of event source. This deals with the intricacies of the different sources whilst providing a common, single place to configure OTEL.

The application is built using [AWS SAM](https://aws.amazon.com/serverless/sam/). To deploy into your own AWS account run the below command from the repo root.

```
sam deploy --guided
```

## Tests

The repo includes tests following the principles of observability driven development (ODD), inspired by [Martin Thwaites repo based on an ASP.NET Core ToDo app](https://github.com/martinjt/todo-odd). As well as asserting based on the Function response, assertions are also made on the spans emitted by the telemtry. 

Writing tests based on spans ensures that good observability will be available in production, and not that observability becomes an afterthought.

## To Do

- [X] SNS doesn't attach to the correct parent span
- [X] Add OTEL driven tests
- [ ] Retrieve Honeycomb API key from secrets manager
- [ ] Add Event Bridge implementation