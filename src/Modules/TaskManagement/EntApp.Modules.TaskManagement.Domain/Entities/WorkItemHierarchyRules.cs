using EntApp.Modules.TaskManagement.Domain.Enums;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>
/// Work Item hiyerarşi kuralları.
/// Hangi tip hangi tipin altına girebilir, varsayılan hiyerarşi seviyesi nedir.
/// </summary>
public static class WorkItemHierarchyRules
{
    private static readonly Dictionary<WorkItemType, WorkItemType[]> AllowedChildren = new()
    {
        [WorkItemType.Epic]        = [WorkItemType.Feature, WorkItemType.UserStory],
        [WorkItemType.Feature]     = [WorkItemType.UserStory, WorkItemType.Task, WorkItemType.Bug],
        [WorkItemType.UserStory]   = [WorkItemType.Task, WorkItemType.Bug],
        [WorkItemType.Task]        = [],  // leaf node
        [WorkItemType.Bug]         = [WorkItemType.Task],
        [WorkItemType.Improvement] = [WorkItemType.Task],
        [WorkItemType.TechDebt]    = [WorkItemType.Task],
        [WorkItemType.Spike]       = [WorkItemType.Task],
    };

    /// <summary>Çocuk tipin, ebeveyn tipin altına girip giremeyeceğini kontrol eder.</summary>
    public static bool CanBeChildOf(WorkItemType childType, WorkItemType parentType)
        => AllowedChildren.GetValueOrDefault(parentType, []).Contains(childType);

    /// <summary>Varsayılan hiyerarşi seviyesini döner (bağımsız item için).</summary>
    public static int GetDefaultHierarchyLevel(WorkItemType type) => type switch
    {
        WorkItemType.Epic => 0,
        WorkItemType.Feature => 1,
        WorkItemType.UserStory => 2,
        WorkItemType.Task => 3,
        WorkItemType.Bug => 3,
        WorkItemType.Improvement => 2,
        WorkItemType.TechDebt => 2,
        WorkItemType.Spike => 2,
        _ => 3
    };
}
