using RureSubPostsWriter.Models.Dtos;

namespace RureSubPostsWriter.Services;

public interface IProfileService
{
    Task<ProfileResponseDto?> GetProfile(Guid id);
}