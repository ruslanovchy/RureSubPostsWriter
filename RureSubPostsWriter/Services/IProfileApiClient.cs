using RureSubPostsWriter.Models.Dtos;

namespace RureSubPostsWriter.Services;

public interface IProfileApiClient
{
    Task<GetProfileDto?> GetProfile(Guid id);
}