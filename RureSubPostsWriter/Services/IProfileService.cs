using RureSubPostsWriter.Models.Dtos;

namespace RureSubPostsWriter.Services;

public interface IProfileService
{
    Task<GetProfileDto?> GetProfile(Guid id);
}