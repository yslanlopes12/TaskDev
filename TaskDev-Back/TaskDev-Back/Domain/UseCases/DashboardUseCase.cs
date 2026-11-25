using TaskDev.Api.Domain.Core.Entities;
using TaskDev.Api.Domain.Core.Enums;
using TaskDev.Api.Domain.Core.Models;
using TaskDev.Api.Domain.Core.ValueObjects;
using TaskDev.Api.Domain.Ports.InBound;
using TaskDev.Api.Domain.Ports.OutBound;

namespace TaskDev.Api.Domain.UseCases;

public sealed class DashboardUseCase : IDashboardUseCase
{
    private readonly ITaskRepository _taskRepository;
    private readonly IUserRepository _userRepository;
    private readonly IXpRepository _xpRepository;

    public DashboardUseCase(
        ITaskRepository taskRepository,
        IUserRepository userRepository,
        IXpRepository xpRepository)
    {
        _taskRepository = taskRepository;
        _userRepository = userRepository;
        _xpRepository = xpRepository;
    }

    public async Task<BaseReturn<DashboardModel>> GetDashboardAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return BaseReturn<DashboardModel>.Unauthorized();
        }

        var tasks = await _taskRepository.GetAccessibleTasksByUserIdAsync(userId);

        // Identifica data de "hoje" no fuso horário do usuário
        TimeZoneInfo tz;
        try
        {
            tz = !string.IsNullOrWhiteSpace(user.Timezone)
                ? TimeZoneInfo.FindSystemTimeZoneById(user.Timezone)
                : TimeZoneInfo.Local;
        }
        catch
        {
            tz = TimeZoneInfo.Utc;
        }

        var userNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
        var today = DateOnly.FromDateTime(userNow.DateTime);

        var todayTasks = new List<TaskItem>();
        var pendingTasks = new List<TaskItem>();
        var plannedTasks = new List<TaskItem>();
        var doneTasks = new List<TaskItem>();

        foreach (var task in tasks)
        {
            if (task.Status == EnumTaskStatus.Done)
            {
                doneTasks.Add(task);
            }
            else if (task.DueDate.HasValue && task.DueDate.Value == today)
            {
                todayTasks.Add(task);
            }
            else if (!task.DueDate.HasValue || task.DueDate.Value < today)
            {
                pendingTasks.Add(task);
            }
            else if (task.DueDate.HasValue && task.DueDate.Value > today)
            {
                plannedTasks.Add(task);
            }
        }

        var model = new DashboardModel
        {
            TodayCount = todayTasks.Count,
            PendingCount = pendingTasks.Count,
            PlannedCount = plannedTasks.Count,
            DoneCount = doneTasks.Count,
            TodayTasks = todayTasks,
            PendingTasks = pendingTasks,
            PlannedTasks = plannedTasks,
            DoneTasks = doneTasks
        };

        return BaseReturn<DashboardModel>.Success(model);
    }

    public async Task<BaseReturn<ProgressModel>> GetProgressAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var totalXp = await _xpRepository.GetTotalXpByUserIdAsync(userId);
        var level = GamificationRules.CalculateLevel(totalXp);
        var rank = GamificationRules.CalculateRank(level);
        var xpInCurrent = totalXp % 100;
        var xpToNext = 100 - xpInCurrent;

        var progress = new ProgressModel
        {
            TotalXp = totalXp,
            Level = level,
            Rank = rank,
            XpInCurrentLevel = xpInCurrent,
            XpForNextLevel = 100,
            XpToNextLevel = xpToNext
        };

        return BaseReturn<ProgressModel>.Success(progress);
    }
}
