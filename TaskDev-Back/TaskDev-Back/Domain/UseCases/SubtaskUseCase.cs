using TaskDev.Api.Domain.Core.Entities;
using TaskDev.Api.Domain.Core.ValueObjects;
using TaskDev.Api.Domain.Ports.InBound;
using TaskDev.Api.Domain.Ports.OutBound;

namespace TaskDev.Api.Domain.UseCases;

public sealed class SubtaskUseCase : ISubtaskUseCase
{
    private readonly ISubtaskRepository _subtaskRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ISpaceRepository _spaceRepository;

    public SubtaskUseCase(
        ISubtaskRepository subtaskRepository,
        ITaskRepository taskRepository,
        ISpaceRepository spaceRepository)
    {
        _subtaskRepository = subtaskRepository;
        _taskRepository = taskRepository;
        _spaceRepository = spaceRepository;
    }

    public async Task<BaseReturn<IReadOnlyList<Subtask>>> GetByTaskAsync(Guid taskId, Guid userId, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null)
        {
            return BaseReturn<IReadOnlyList<Subtask>>.NotFound("Tarefa pai não encontrada.");
        }

        var hasAccess = await _spaceRepository.HasAccessAsync(task.SpaceId, userId);
        if (!hasAccess)
        {
            return BaseReturn<IReadOnlyList<Subtask>>.Forbidden();
        }

        var subtasks = await _subtaskRepository.GetByTaskIdAsync(taskId);
        return BaseReturn<IReadOnlyList<Subtask>>.Success(subtasks);
    }

    public async Task<BaseReturn<Subtask>> CreateSubtaskAsync(CreateSubtaskCommand command, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(command.TaskId);
        if (task == null)
        {
            return BaseReturn<Subtask>.NotFound("Tarefa pai não encontrada.");
        }

        var hasAccess = await _spaceRepository.HasAccessAsync(task.SpaceId, command.UserId);
        if (!hasAccess)
        {
            return BaseReturn<Subtask>.Forbidden();
        }

        if (string.IsNullOrWhiteSpace(command.Title))
        {
            return BaseReturn<Subtask>.ValidationError("O título da subtarefa é obrigatório.");
        }

        var existingSubtasks = await _subtaskRepository.GetByTaskIdAsync(command.TaskId);
        var nextPosition = existingSubtasks.Count > 0 ? existingSubtasks.Max(s => s.Position) + 1000 : 1000;

        var subtask = new Subtask
        {
            Id = Guid.NewGuid(),
            TaskId = command.TaskId,
            Title = command.Title.Trim(),
            IsCompleted = false,
            Position = nextPosition,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var createdId = await _subtaskRepository.CreateAsync(subtask);
        subtask.Id = createdId;

        return BaseReturn<Subtask>.Success(subtask);
    }

    public async Task<BaseReturn<Subtask>> UpdateSubtaskAsync(UpdateSubtaskCommand command, CancellationToken cancellationToken = default)
    {
        var subtask = await _subtaskRepository.GetByIdAsync(command.SubtaskId);
        if (subtask == null)
        {
            return BaseReturn<Subtask>.NotFound("Subtarefa não encontrada.");
        }

        var task = await _taskRepository.GetByIdAsync(subtask.TaskId);
        if (task == null)
        {
            return BaseReturn<Subtask>.NotFound("Tarefa pai não encontrada.");
        }

        var hasAccess = await _spaceRepository.HasAccessAsync(task.SpaceId, command.UserId);
        if (!hasAccess)
        {
            return BaseReturn<Subtask>.Forbidden();
        }

        subtask.Title = command.Title.Trim();
        subtask.IsCompleted = command.IsCompleted;

        await _subtaskRepository.UpdateAsync(subtask);
        return BaseReturn<Subtask>.Success(subtask);
    }

    public async Task<BaseReturn<ToggleSubtaskResult>> ToggleSubtaskAsync(Guid subtaskId, Guid userId, CancellationToken cancellationToken = default)
    {
        var subtask = await _subtaskRepository.GetByIdAsync(subtaskId);
        if (subtask == null)
        {
            return BaseReturn<ToggleSubtaskResult>.NotFound("Subtarefa não encontrada.");
        }

        var task = await _taskRepository.GetByIdAsync(subtask.TaskId);
        if (task == null)
        {
            return BaseReturn<ToggleSubtaskResult>.NotFound("Tarefa pai não encontrada.");
        }

        var hasAccess = await _spaceRepository.HasAccessAsync(task.SpaceId, userId);
        if (!hasAccess)
        {
            return BaseReturn<ToggleSubtaskResult>.Forbidden();
        }

        bool targetCompleted = !subtask.IsCompleted;
        if (targetCompleted)
        {
            var allSubtasks = await _subtaskRepository.GetByTaskIdAsync(subtask.TaskId);
            int subtaskIndex = allSubtasks.ToList().FindIndex(s => s.Id == subtaskId);
            if (subtaskIndex < 0) subtaskIndex = 0;

            int taskTotalXp = GamificationRules.GetXpForPriority(task.Priority);
            int fractionPoints = GamificationRules.GetSubtaskXpFraction(taskTotalXp, allSubtasks.Count, subtaskIndex);

            var (success, awarded) = await _subtaskRepository.CompleteSubtaskWithXpAsync(subtaskId, userId, fractionPoints);
            subtask.IsCompleted = true;
            return BaseReturn<ToggleSubtaskResult>.Success(new ToggleSubtaskResult(subtask, true, awarded, 0));
        }
        else
        {
            var (success, deducted) = await _subtaskRepository.RevertSubtaskXpAsync(subtaskId);
            subtask.IsCompleted = false;
            return BaseReturn<ToggleSubtaskResult>.Success(new ToggleSubtaskResult(subtask, false, 0, deducted));
        }
    }

    public async Task<BaseReturn<bool>> DeleteSubtaskAsync(Guid subtaskId, Guid userId, CancellationToken cancellationToken = default)
    {
        var subtask = await _subtaskRepository.GetByIdAsync(subtaskId);
        if (subtask == null)
        {
            return BaseReturn<bool>.NotFound("Subtarefa não encontrada.");
        }

        var task = await _taskRepository.GetByIdAsync(subtask.TaskId);
        if (task == null)
        {
            return BaseReturn<bool>.NotFound("Tarefa pai não encontrada.");
        }

        var hasAccess = await _spaceRepository.HasAccessAsync(task.SpaceId, userId);
        if (!hasAccess)
        {
            return BaseReturn<bool>.Forbidden();
        }

        await _subtaskRepository.DeleteAsync(subtaskId);
        return BaseReturn<bool>.Success(true);
    }

    public async Task<BaseReturn<bool>> ReorderSubtasksAsync(ReorderSubtasksCommand command, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(command.TaskId);
        if (task == null)
        {
            return BaseReturn<bool>.NotFound("Tarefa pai não encontrada.");
        }

        var hasAccess = await _spaceRepository.HasAccessAsync(task.SpaceId, command.UserId);
        if (!hasAccess)
        {
            return BaseReturn<bool>.Forbidden();
        }

        await _subtaskRepository.ReorderAsync(command.TaskId, command.SubtaskIdsInOrder);
        return BaseReturn<bool>.Success(true);
    }
}
