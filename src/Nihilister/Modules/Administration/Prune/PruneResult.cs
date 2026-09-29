#nullable disable
namespace Nihilister.Modules.Administration.Services;

public enum PruneResult
{
    Success,
    AlreadyRunning,
    FeatureLimit,
}