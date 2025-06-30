using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Interfaces.Cronjob;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Commons;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.Application.Services.Cronjob
{
    public class TaskCleanupService : ITaskCleanupService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggerService _loggerService;

        public TaskCleanupService(IUnitOfWork unitOfWork, ILoggerService loggerService)
        {
            _unitOfWork = unitOfWork;
            _loggerService = loggerService;
        }

        public async Task<int> CleanupExpiredEventsAsync()
        {
            var now = DateTime.UtcNow;

            var expiredEvents = await _unitOfWork.Events
                .GetQueryable()
                .Where(e => e.EndTime < now && !e.IsDeleted)
                .ToListAsync();

            if (!expiredEvents.Any())
            {
                _loggerService.Info("[TaskCleanupService] No expired events to clean up.");
                return 0;
            }

            int totalPromotionsDeleted = 0;

            foreach (var expiredEvent in expiredEvents)
            {
                var eventWithPromotions = await _unitOfWork.Events
                    .GetByIdAsync(expiredEvent.Id, e => e.Promotions);

                if (eventWithPromotions == null) continue;

                if (eventWithPromotions.Promotions?.Any() == true)
                {
                    await _unitOfWork.Promotions.SoftRemoveRange(eventWithPromotions.Promotions.ToList());
                    totalPromotionsDeleted += eventWithPromotions.Promotions.Count;
                }

                await _unitOfWork.Events.SoftRemove(eventWithPromotions);
            }

            await _unitOfWork.SaveChangesAsync();

            _loggerService.Success($"[TaskCleanupService] Soft deleted {expiredEvents.Count} expired events and {totalPromotionsDeleted} promotions.");
            return expiredEvents.Count;
        }

        public async Task<int> CleanupPastShowTimesAsync()
        {
            var now = DateTime.UtcNow;
            var showTimesToDelete = await _unitOfWork.ShowTimes
                .GetQueryable()
                .Where(st => !st.IsDeleted && st.ShowDate < now)
                .ToListAsync();

            if (!showTimesToDelete.Any())
            {
                _loggerService.Info("[TaskCleanupService] No past showtimes to clean up.");
                return 0;
            }

            await _unitOfWork.ShowTimes.SoftRemoveRange(showTimesToDelete);
            var affected = await _unitOfWork.SaveChangesAsync();

            _loggerService.Success($"[TaskCleanupService] Soft deleted {showTimesToDelete.Count} past showtimes.");
            return showTimesToDelete.Count;
        }
    }
}
