using Microsoft.AspNetCore.Mvc.RazorPages;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.DTOs.PromotionDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Infrastructure.Commons;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.Application.Services;

public class PromotionService : IPromotionService
{
    private readonly ILoggerService _loggerService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsService _claimsService;
    public PromotionService(IUnitOfWork unitOfWork, ILoggerService loggerService, IClaimsService claimsService)
    {
        _unitOfWork = unitOfWork;
        _loggerService = loggerService;
        _claimsService = claimsService;
    }
    public async Task<List<PromotionResponseDto>> GetAllPromotionListAsync()
    {
        try
        {
            var allPromotions = await _unitOfWork.Promotions.GetAllAsync();

            var result = allPromotions.Select(p => new PromotionResponseDto
            {
                Id = p.Id,
                Title = p.Title,
                DiscountValue = p.DiscountValue,
                Detail = p.Detail,
                Image = p.Image
            }).ToList();

            _loggerService.Success($"Retrieved {result.Count} promotions successfully.");

            return new List<PromotionResponseDto>();
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error retrieving promotions: {ex.Message}");
            throw;
        }
    }
}
