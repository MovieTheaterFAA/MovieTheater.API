using Quartz;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;

namespace MovieTheater.API.Jobs
{
    public class CleanUpExpiredEventsJob : IJob
    {
        private readonly IEventService _eventService;
        private readonly ILoggerService _logger;

        public CleanUpExpiredEventsJob(IEventService eventService, ILoggerService logger)
        {
            _eventService = eventService;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.Info("CleanUpExpiredEventsJob started.");
            try
            {
                await _eventService.CleanUpExpiredEventsAsync();
                _logger.Success("CleanUpExpiredEventsJob completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.Error($"CleanUpExpiredEventsJob failed: {ex.Message}");
                throw;
            }
        }
    }
}
