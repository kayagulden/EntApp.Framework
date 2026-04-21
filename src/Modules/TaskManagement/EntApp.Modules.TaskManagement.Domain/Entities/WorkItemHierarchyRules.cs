using EntApp.Modules.TaskManagement.Domain.Enums;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>
/// Work Item hiyerarşi kuralları.
/// Hangi tip hangi tipin altına girebilir, varsayılan hiyerarşi seviyesi nedir.
/// </summary>
public static class WorkItemHierarchyRules
{
    private static readonly Dictionary<TaskType, TaskType[]> AllowedChildren = new()
    {
        [TaskType.Epic]        = [TaskType.Feature, TaskType.UserStory],
        [TaskType.Feature]     = [TaskType.UserStory, TaskType.Task, TaskType.Bug],
        [TaskType.UserStory]   = [TaskType.Task, TaskType.Bug],
        [TaskType.Task]        = [],  // leaf node
        [TaskType.Bug]         = [TaskType.Task],
        [TaskType.Improvement] = [TaskType.Task],
        [TaskType.TechDebt]    = [TaskType.Task],
        [TaskType.Spike]       = [TaskType.Task],
    };

    /// <summary>Çocuk tipin, ebeveyn tipin altına girip giremeyeceğini kontrol eder.</summary>
    public static bool CanBeChildOf(TaskType childType, TaskType parentType)
        => AllowedChildren.GetValueOrDefault(parentType, []).Contains(childType);

    /// <summary>Varsayılan hiyerarşi seviyesini döner (bağımsız item için).</summary>
    public static int GetDefaultHierarchyLevel(TaskType type) => type switch
    {
        TaskType.Epic => 0,
        TaskType.Feature => 1,
        TaskType.UserStory => 2,
        TaskType.Task => 3,
        TaskType.Bug => 3,
        TaskType.Improvement => 2,
        TaskType.TechDebt => 2,
        TaskType.Spike => 2,
        _ => 3
    };
}
