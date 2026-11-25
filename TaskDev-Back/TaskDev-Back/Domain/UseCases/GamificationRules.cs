namespace TaskDev.Api.Domain.UseCases;

public static class GamificationRules
{
    public static int CalculateLevel(int totalXp)
    {
        if (totalXp <= 0) return 1;
        return (totalXp / 100) + 1;
    }

    public static string CalculateRank(int level)
    {
        if (level >= 35) return "S";
        if (level >= 21) return "A";
        if (level >= 12) return "B";
        if (level >= 6)  return "C";
        if (level >= 3)  return "D";
        return "E";
    }

    public static int GetXpForPriority(string priority)
    {
        return priority.ToUpperInvariant() switch
        {
            "LOW" => 15,
            "MEDIUM" => 30,
            "HIGH" => 50,
            "URGENT" => 80,
            _ => 30
        };
    }

    /// <summary>
    /// Divide o XP total da tarefa entre as subtarefas existentes.
    /// A última subtarefa recebe o saldo restante para garantir que a soma das frações atinja 100% do XP da tarefa pai.
    /// </summary>
    public static int GetSubtaskXpFraction(int taskTotalXp, int totalSubtasks, int subtaskIndex)
    {
        if (totalSubtasks <= 0 || taskTotalXp <= 0) return 0;
        if (totalSubtasks == 1) return taskTotalXp;

        int baseFraction = taskTotalXp / totalSubtasks;
        if (baseFraction < 1) baseFraction = 1;

        if (subtaskIndex >= totalSubtasks - 1)
        {
            int previousAllocated = baseFraction * (totalSubtasks - 1);
            int remaining = taskTotalXp - previousAllocated;
            return remaining > 0 ? remaining : baseFraction;
        }

        return baseFraction;
    }
}
