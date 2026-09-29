#nullable disable
namespace Nihilister.Modules.Administration;

public partial class Administration : NadekoModule<AdministrationService>
{
    private readonly IHttpClientFactory _httpClientFactory;

    public Administration(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
}