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

        public async Task<int> CleanupExpiredOrDeletedShowTimeSeatsAsync()
        {
            var now = DateTime.UtcNow;

            var expiredOrDeletedShowTimeIds = await _unitOfWork.ShowTimes
                .GetQueryable()
                .Where(st => st.IsDeleted || st.ShowDate < now)
                .Select(st => st.Id)
                .ToListAsync();

            if (!expiredOrDeletedShowTimeIds.Any())
            {
                _loggerService.Info("[TaskCleanupSerivce] No expired or deleted showtimes found for ShowTimeSeat cleanup.");
                return 0;
            }

            var showTimeSeatsToDelete = await _unitOfWork.ShowTimeSeats
                .GetQueryable()
                .Where(sts => expiredOrDeletedShowTimeIds.Contains(sts.ShowTimeId))
                .ToListAsync();

            if (!showTimeSeatsToDelete.Any())
            {
                _loggerService.Info("[TaskCleanupService] No ShowTimeSeat records to delete.");
                return 0;
            }

            await _unitOfWork.ShowTimeSeats.SoftRemoveRange(showTimeSeatsToDelete);
            var affected = await _unitOfWork.SaveChangesAsync();

            _loggerService.Success($"[TaskCleanupService] Deleted {affected} ShowTimeSeat records for expired or deleted showtimes.");
            return affected;
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

        public async Task<int> CreateBirthdayPromotionsAsync()
        {
            var today = DateTime.UtcNow;
            _loggerService.Info("[TaskCleanupService] Start checking birthday promotions for today.");

            var birthdayEvent = await _unitOfWork.Events
                .GetQueryable()
                .FirstOrDefaultAsync(e => e.Name == "Happy Birthday - Special Gift" && !e.IsDeleted);

            if (birthdayEvent == null)
            {
                _loggerService.Warn("[TaskCleanupService] Birthday event not found. No birthday promotions will be available.");
                return 0;
            }

            var members = await _unitOfWork.Users
                .GetQueryable()
                .Where(u => u.Role == RoleType.Member && !u.IsDeleted && u.DateOfBirth.HasValue)
                .ToListAsync();

            if (!members.Any())
            {
                _loggerService.Info("[TaskCleanupService] No members found to check for birthday promotions.");
                return 0;
            }

            int birthdayCount = 0;
            foreach (var member in members)
            {
                if (member.DateOfBirth.HasValue &&
                    member.DateOfBirth.Value.Month == today.Month &&
                    member.DateOfBirth.Value.Day == today.Day)
                {
                    birthdayCount++;
                    _loggerService.Info($"[TaskCleanupService] Member '{member.FullName}' (Email: {member.Email}) has birthday today and is eligible for the birthday promotion.");
                }
            }

            if (birthdayCount == 0)
            {
                _loggerService.Info("[TaskCleanupService] No members have birthday today. No birthday promotions to activate.");
            }
            else
            {
                _loggerService.Success($"[TaskCleanupService] {birthdayCount} member(s) have birthday today and can use the birthday promotion.");
            }

            return birthdayCount;
        }
    }
}
