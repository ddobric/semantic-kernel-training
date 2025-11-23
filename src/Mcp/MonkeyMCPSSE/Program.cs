using Microsoft.Extensions.Configuration.UserSecrets;
using ModelContextProtocol.Protocol;
using MonkeyMCPSSE;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddMcpServer()
     .WithHttpTransport(httpTransportOptions =>
     {
         httpTransportOptions.Stateless = true;
         //httpTransportOptions.RunSessionHandler = (httpContext, mcpServer, cancellationToken) =>
         //{
         //    return mcpServer.RunAsync(cancellationToken);
         //};
     })
    .WithTools<MonkeyTools>()
    .WithTools<EchoTool>();

//https://github.com/microsoft/mcp-for-beginners/blob/main/03-GettingStarted/06-http-streaming/solution/dotnet/Program.cs
//builder.Services
//       .AddMcpServer()
//       .WithHttpTransport(o => o.Stateless = true)
//       .WithTools<Tools>();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<MonkeyService>();

var app = builder.Build();

app.MapMcp();

app.Run();