using System;
using System.Net.Http;
using System.Threading.Tasks;
using GlobalExceptionHandler.Tests.Fixtures;
using GlobalExceptionHandler.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Shouldly;
using Xunit;

namespace GlobalExceptionHandler.Tests.WebApi.LoggerTests
{
    public class HandledExceptionLoggerTests : IClassFixture<WebApiServerFixture>, IAsyncLifetime
    {
        private readonly TestServer _server;
        private bool _exceptionWasHandled;
        private Type _matchedException;
        private Exception _exception;
        private const string RequestUri = "/api/productnotfound";

        public HandledExceptionLoggerTests(WebApiServerFixture fixture)
        {
            // Arrange
            var webHost = fixture.CreateWebHostWithMvc();
            webHost.Configure(app =>
            {
                app.UseGlobalExceptionHandler(x =>
                {
                    x.OnException((context, _) =>
                    {
                        _exceptionWasHandled = context.ExceptionHandled;
                        _matchedException = context.ExceptionMatched;
                        _exception = context.Exception;

                        return Task.CompletedTask;
                    });
                    x.Map<ArgumentException>().ToStatusCode(StatusCodes.Status404NotFound).WithBody((e, c, h) => Task.CompletedTask);
                });

                app.Map(RequestUri, config =>
                {
                    config.Run(context => throw new ArgumentException("Invalid request"));
                });
            });

            _server = new TestServer(webHost);
        }

        public async Task InitializeAsync()
        {
            using var client = _server.CreateClient();
            var requestMessage = new HttpRequestMessage(new HttpMethod("GET"), RequestUri);
            await client.SendAsync(requestMessage);
        }

        [Fact]
        public void ExceptionHandled()
            => _exceptionWasHandled.ShouldBeTrue();

        [Fact]
        public void ExceptionTypeMatches()
            => _matchedException.FullName.ShouldBe("System.ArgumentException");

        [Fact]
        public void ExceptionIsCorrect()
            => _exception.ShouldBeOfType<ArgumentException>();

        public Task DisposeAsync()
            => Task.CompletedTask;
    }
}