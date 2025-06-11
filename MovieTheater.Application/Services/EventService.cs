using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.DTOs.EventDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.Application.Services
{
    public class EventService : IEventService
    {
        private readonly ILoggerService _loggerService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClaimsService _claimsService;

        public EventService(IUnitOfWork unitOfWork, ILoggerService loggerService, IClaimsService claimsService)
        {
            _unitOfWork = unitOfWork;
            _loggerService = loggerService;
            _claimsService = claimsService;
        }

        public async Task<EventResponseDto?> AddEventAsync(EventRequestDto dto)
        {
            _loggerService.Info($"[AddEventAsync] Start adding event: {dto.Name}");

            // Kiểm tra sự kiện có tồn tại với tên đã cho chưa
            var existingEvent = await _unitOfWork.Events.FirstOrDefaultAsync(e => e.Name == dto.Name);
            if (existingEvent != null)
            {
                _loggerService.Warn($"[AddEventAsync] Event with name {dto.Name} already exists.");
                throw new InvalidOperationException("Event with this name already exists.");
            }

            // Kiểm tra PromotionId có hợp lệ hay không (sử dụng IUnitOfWork)
            var existingPromotion = await _unitOfWork.Promotions.GetByIdAsync(dto.PromotionId);
            if (existingPromotion == null)
            {
                _loggerService.Warn($"[AddEventAsync] Promotion with ID {dto.PromotionId} does not exist.");
                throw new KeyNotFoundException("Promotion with the provided ID does not exist.");
            }

            // Tạo đối tượng Event từ DTO
            var newEvent = new Event
            {
                Name = dto.Name,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Detail = dto.Detail,
                Image = dto.Image,
            };

            // Thêm sự kiện vào cơ sở dữ liệu
            await _unitOfWork.Events.AddAsync(newEvent);

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                _loggerService.Error($"DbUpdateException: {dbEx.InnerException?.Message ?? dbEx.Message}");
                throw;
            }

            _loggerService.Success($"[AddEventAsync] Event {newEvent.Name} added successfully.");

            // Trả về DTO chứa thông tin của sự kiện đã thêm
            return new EventResponseDto
            {
                Id = newEvent.Id,
                Name = newEvent.Name,
                StartTime = newEvent.StartTime,
                EndTime = newEvent.EndTime,
                Detail = newEvent.Detail,
                Image = newEvent.Image,
            };
        }
    }
}