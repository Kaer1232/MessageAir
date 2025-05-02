using Microsoft.AspNetCore.SignalR;

namespace SignalRChatServer.Controllers
{
    public class ErrorHandlingFilter: IHubFilter
    {
        private readonly ILogger<ErrorHandlingFilter> _logger;

        public ErrorHandlingFilter(ILogger<ErrorHandlingFilter> logger)
        {
            _logger = logger;
        }

        public async ValueTask<object?> InvokeMethodAsync(
            HubInvocationContext invocationContext,
            Func<HubInvocationContext, ValueTask<object?>> next)
        {
            try
            {
                return await next(invocationContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in {invocationContext.HubMethodName}");
                throw;
            }
        }
    }
}
