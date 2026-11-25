using TaskDev.Api.Domain.Core.Entities;
using TaskDev.Api.Domain.Core.Enums;
using TaskDev.Api.Domain.Core.ValueObjects;
using TaskDev.Api.Domain.Ports.InBound;
using TaskDev.Api.Domain.Ports.OutBound;

namespace TaskDev.Api.Domain.UseCases;

public sealed class TaskUseCase : ITaskUseCase
{
    private readonly ITaskRepository _taskRepository;
    private readonly ISpaceRepository _spaceRepository;
    private readonly ISubtaskRepository _subtaskRepository;

    public TaskUseCase(ITaskRepository taskRepository, ISpaceRepository spaceRepository, ISubtaskRepository subtaskRepository)
    {
        _taskRepository = taskRepository;
        _spaceRepository = spaceRepository;
        _subtaskRepository = subtaskRepository;
    }

    public async Task<BaseReturn<IReadOnlyList<TaskItem>>> GetTasksAsync(Guid userId, Guid? spaceId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TaskItem> tasks;
        if (spaceId.HasValue)
        {
            var hasAccess = await _spaceRepository.HasAccessAsync(spaceId.Value, userId);
            if (!hasAccess)
            {
                return BaseReturn<IReadOnlyList<TaskItem>>.Forbidden("Você não tem acesso a este espaço.");
            }

            tasks = await _taskRepository.GetBySpaceIdAsync(spaceId.Value);
        }
        else
        {
            tasks = await _taskRepository.GetAccessibleTasksByUserIdAsync(userId);
        }

        if (tasks.Count > 0)
        {
            var taskIds = tasks.Select(t => t.Id).ToList();
            var allSubtasks = await _subtaskRepository.GetByTaskIdsAsync(taskIds);
            var subtasksByTaskId = allSubtasks.GroupBy(s => s.TaskId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var task in tasks)
            {
                if (subtasksByTaskId.TryGetValue(task.Id, out var subs))
                {
                    task.Subtasks = subs;
                }
            }
        }

        return BaseReturn<IReadOnlyList<TaskItem>>.Success(tasks);
    }

    public async Task<BaseReturn<TaskItem>> GetByIdAsync(Guid taskId, Guid userId, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null)
        {
            return BaseReturn<TaskItem>.NotFound("Tarefa não encontrada.");
        }

        var hasAccess = await _spaceRepository.HasAccessAsync(task.SpaceId, userId);
        if (!hasAccess)
        {
            return BaseReturn<TaskItem>.Forbidden("Você não tem acesso a este espaço.");
        }

        var subtasks = await _subtaskRepository.GetByTaskIdAsync(taskId);
        task.Subtasks = subtasks.ToList();

        return BaseReturn<TaskItem>.Success(task);
    }

    public async Task<BaseReturn<TaskItem>> CreateTaskAsync(CreateTaskCommand command, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _spaceRepository.HasAccessAsync(command.SpaceId, command.CreatedByUserId);
        if (!hasAccess)
        {
            return BaseReturn<TaskItem>.Forbidden("Você não tem permissão para criar tarefas neste espaço.");
        }

        if (string.IsNullOrWhiteSpace(command.Title))
        {
            return BaseReturn<TaskItem>.ValidationError("O título da tarefa é obrigatório.");
        }

        if (command.AssigneeUserId.HasValue)
        {
            var assigneeHasAccess = await _spaceRepository.HasAccessAsync(command.SpaceId, command.AssigneeUserId.Value);
            if (!assigneeHasAccess)
            {
                return BaseReturn<TaskItem>.ValidationError("O usuário atribuído não tem acesso a este espaço.");
            }
        }

        var subtaskTitles = command.Subtasks ?? [];
        if (subtaskTitles.Any(title => string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200))
        {
            return BaseReturn<TaskItem>.ValidationError("Cada subtarefa deve ter um título de 1 a 200 caracteres.");
        }

        var maxPos = await _taskRepository.GetMaxPositionAsync(command.SpaceId, EnumTaskStatus.Pending);
        var position = maxPos + 1000;

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            SpaceId = command.SpaceId,
            CreatedByUserId = command.CreatedByUserId,
            AssigneeUserId = command.AssigneeUserId,
            Title = command.Title.Trim(),
            Description = command.Description,
            Status = EnumTaskStatus.Pending,
            Priority = EnumTaskPriority.IsValid(command.Priority) ? command.Priority.ToUpperInvariant() : EnumTaskPriority.Medium,
            DueDate = command.DueDate,
            Position = position,
            XpAwarded = false
        };

        task.Subtasks = subtaskTitles.Select((title, index) => new Subtask
        {
            TaskId = task.Id,
            Title = title.Trim(),
            Position = (index + 1L) * 1000,
            CreatedAt = task.CreatedAt
        }).ToList();

        var createdId = await _taskRepository.CreateAsync(task);
        task.Id = createdId;

        return BaseReturn<TaskItem>.Success(task);
    }

    public async Task<BaseReturn<TaskItem>> UpdateTaskAsync(UpdateTaskCommand command, CancellationToken cancellationToken = default)
    {
        var existing = await _taskRepository.GetByIdAsync(command.TaskId);
        if (existing == null)
        {
            return BaseReturn<TaskItem>.NotFound("Tarefa não encontrada.");
        }

        var hasAccess = await _spaceRepository.HasAccessAsync(existing.SpaceId, command.UserId);
        if (!hasAccess)
        {
            return BaseReturn<TaskItem>.Forbidden();
        }

        if (command.AssigneeUserId.HasValue)
        {
            var assigneeHasAccess = await _spaceRepository.HasAccessAsync(existing.SpaceId, command.AssigneeUserId.Value);
            if (!assigneeHasAccess)
            {
                return BaseReturn<TaskItem>.ValidationError("O usuário atribuído não tem acesso a este espaço.");
            }
        }

        if (command.Status == EnumTaskStatus.Done && !existing.XpAwarded)
        {
            await _taskRepository.CompleteWithXpTransactionAsync(existing.Id, command.UserId);
        }
        else if (command.Status != EnumTaskStatus.Done && existing.Status == EnumTaskStatus.Done && existing.XpAwarded)
        {
            await _taskRepository.RevertXpTransactionAsync(existing.Id, command.UserId, command.Status);
        }

        existing.Title = command.Title.Trim();
        existing.Description = command.Description;
        existing.Status = EnumTaskStatus.IsValid(command.Status) ? command.Status : existing.Status;
        existing.Priority = EnumTaskPriority.IsValid(command.Priority) ? command.Priority : existing.Priority;
        existing.DueDate = command.DueDate;
        existing.AssigneeUserId = command.AssigneeUserId;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        var subtasks = await _subtaskRepository.GetByTaskIdAsync(existing.Id);
        existing.Subtasks = subtasks.ToList();

        await _taskRepository.UpdateAsync(existing);
        return BaseReturn<TaskItem>.Success(existing);
    }

    public async Task<BaseReturn<bool>> DeleteTaskAsync(Guid taskId, Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await _taskRepository.GetByIdAsync(taskId);
        if (existing == null)
        {
            return BaseReturn<bool>.NotFound("Tarefa não encontrada.");
        }

        var hasAccess = await _spaceRepository.HasAccessAsync(existing.SpaceId, userId);
        if (!hasAccess)
        {
            return BaseReturn<bool>.Forbidden();
        }

        await _taskRepository.DeleteAsync(taskId);
        return BaseReturn<bool>.Success(true);
    }

    public async Task<BaseReturn<bool>> UpdateStatusAsync(Guid taskId, Guid userId, string status, CancellationToken cancellationToken = default)
    {
        if (!EnumTaskStatus.IsValid(status))
        {
            return BaseReturn<bool>.ValidationError("Status inválido.");
        }

        var existing = await _taskRepository.GetByIdAsync(taskId);
        if (existing == null)
        {
            return BaseReturn<bool>.NotFound("Tarefa não encontrada.");
        }

        var hasAccess = await _spaceRepository.HasAccessAsync(existing.SpaceId, userId);
        if (!hasAccess)
        {
            return BaseReturn<bool>.Forbidden();
        }

        if (status == EnumTaskStatus.Done && !existing.XpAwarded)
        {
            await _taskRepository.CompleteWithXpTransactionAsync(taskId, userId);
        }
        else if (status != EnumTaskStatus.Done && existing.Status == EnumTaskStatus.Done && existing.XpAwarded)
        {
            await _taskRepository.RevertXpTransactionAsync(taskId, userId, status);
        }
        else
        {
            await _taskRepository.UpdateStatusAsync(taskId, status);
        }

        return BaseReturn<bool>.Success(true);
    }

    public async Task<BaseReturn<bool>> MoveTaskAsync(MoveTaskCommand command, CancellationToken cancellationToken = default)
    {
        if (!EnumTaskStatus.IsValid(command.Status))
        {
            return BaseReturn<bool>.ValidationError("Status de destino inválido.");
        }

        var existing = await _taskRepository.GetByIdAsync(command.TaskId);
        if (existing == null)
        {
            return BaseReturn<bool>.NotFound("Tarefa não encontrada.");
        }

        var hasAccess = await _spaceRepository.HasAccessAsync(existing.SpaceId, command.UserId);
        if (!hasAccess)
        {
            return BaseReturn<bool>.Forbidden();
        }

        if (command.Status == EnumTaskStatus.Done && !existing.XpAwarded)
        {
            await _taskRepository.CompleteWithXpTransactionAsync(command.TaskId, command.UserId);
        }
        else if (command.Status != EnumTaskStatus.Done && existing.Status == EnumTaskStatus.Done && existing.XpAwarded)
        {
            await _taskRepository.RevertXpTransactionAsync(command.TaskId, command.UserId, command.Status);
        }

        await _taskRepository.MoveAsync(command.TaskId, command.Status, command.Position);
        return BaseReturn<bool>.Success(true);
    }

    public async Task<BaseReturn<CompleteTaskResult>> CompleteTaskAsync(Guid taskId, Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await _taskRepository.GetByIdAsync(taskId);
        if (existing == null)
        {
            return BaseReturn<CompleteTaskResult>.NotFound("Tarefa não encontrada.");
        }

        var hasAccess = await _spaceRepository.HasAccessAsync(existing.SpaceId, userId);
        if (!hasAccess)
        {
            return BaseReturn<CompleteTaskResult>.Forbidden();
        }

        var (xpAwarded, pointsAwarded) = await _taskRepository.CompleteWithXpTransactionAsync(taskId, userId);
        return BaseReturn<CompleteTaskResult>.Success(new CompleteTaskResult(xpAwarded, pointsAwarded));
    }

    public async Task<BaseReturn<int>> UncompleteTaskAsync(Guid taskId, Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await _taskRepository.GetByIdAsync(taskId);
        if (existing == null)
        {
            return BaseReturn<int>.NotFound("Tarefa não encontrada.");
        }

        var hasAccess = await _spaceRepository.HasAccessAsync(existing.SpaceId, userId);
        if (!hasAccess)
        {
            return BaseReturn<int>.Forbidden();
        }

        var (_, pointsDeducted) = await _taskRepository.RevertXpTransactionAsync(taskId, userId, EnumTaskStatus.Pending);
        return BaseReturn<int>.Success(pointsDeducted);
    }
}
