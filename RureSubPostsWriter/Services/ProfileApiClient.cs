using RureSubPostsWriter.Models.Dtos;
using System.Text;

namespace RureSubPostsWriter.Services;

public class ProfileApiClient(HttpClient client) : IProfileApiClient
{
    private readonly HttpClient client = client;

    public async Task<GetProfileDto?> GetProfile(Guid id)
    {
        var response = await client.GetAsync($"?id={id}");

        if (response.StatusCode != System.Net.HttpStatusCode.OK)
        {
            return null;
        }

        var json = await response.Content.ReadFromJsonAsync<GetProfileDto>();

        return json;
    }
}
