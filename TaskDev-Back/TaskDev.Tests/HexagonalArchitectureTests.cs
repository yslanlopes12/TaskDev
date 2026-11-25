using TaskDev.Api.Adapter.InBound.AdapterHttp.DTOs.Request;
using TaskDev.Api.Adapter.InBound.AdapterHttp.DTOs.Validation;
using TaskDev.Api.Adapter.InBound.AdapterHttp.Mapper;
using TaskDev.Api.Domain.Core.Entities;
using TaskDev.Api.Domain.Core.Enums;
using TaskDev.Api.Domain.Core.ValueObjects;
using Xunit;

namespace TaskDev.Tests;

public class HexagonalArchitectureTests
{
    [Fact]
    public void BaseReturn_Success_ContainsDataAndIsSuccessTrue()
    {
        var result = BaseReturn<string>.Success("Operação bem-sucedida");

        Assert.True(result.IsSuccess);
        Assert.Equal("Operação bem-sucedida", result.SuccessObject);
        Assert.Null(result.ErrorObject);
        Assert.Equal(EnumStatus.Success, result.Status);
    }

    [Fact]
    public void BaseReturn_NotFound_ContainsErrorAndIsSuccessFalse()
    {
        var result = BaseReturn<string>.NotFound("Registro não encontrado");

        Assert.False(result.IsSuccess);
        Assert.Null(result.SuccessObject);
        Assert.NotNull(result.ErrorObject);
        Assert.Equal(EnumStatus.NotFound, result.Status);
        Assert.Equal("Registro não encontrado", result.ErrorObject.Message);
    }

    [Fact]
    public void HttpValidator_InvalidLoginRequest_ReturnsErrors()
    {
        var request = new RequestLogin(string.Empty, string.Empty);

        var (isValid, errors) = HttpValidator.Validate(request);

        Assert.False(isValid);
        Assert.NotNull(errors);
        Assert.True(errors.ContainsKey("Username"));
        Assert.True(errors.ContainsKey("Password"));
    }

    [Fact]
    public void HttpValidator_ValidLoginRequest_ReturnsValidTrue()
    {
        var request = new RequestLogin("devuser", "SecretPass123!");

        var (isValid, errors) = HttpValidator.Validate(request);

        Assert.True(isValid);
        Assert.Null(errors);
    }

    [Fact]
    public void Mapper_TaskItemToResponse_MapsPropertiesCorrectly()
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            SpaceId = Guid.NewGuid(),
            Title = "Implementar Arquitetura Hexagonal",
            Description = "Separação rigorosa de portas e adaptadores",
            Status = "IN_PROGRESS",
            Priority = "HIGH",
            Position = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var response = task.ToResponse();

        Assert.Equal(task.Id, response.Id);
        Assert.Equal(task.Title, response.Title);
        Assert.Equal("HIGH", response.Priority);
        Assert.Equal("IN_PROGRESS", response.Status);
        Assert.Equal(task.SpaceId, response.SpaceId);
    }

    [Fact]
    public void Mapper_RequestCreateTask_ToCommand_BindsFieldsProperly()
    {
        var spaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new RequestCreateTask(
            spaceId: spaceId,
            title: "Nova Tarefa de Teste",
            description: "Descrição detalhada",
            priority: "HIGH",
            dueDate: new DateOnly(2026, 12, 31),
            assigneeUserId: userId
        );

        var cmd = request.ToCommand(userId);

        Assert.Equal(spaceId, cmd.SpaceId);
        Assert.Equal(userId, cmd.CreatedByUserId);
        Assert.Equal("Nova Tarefa de Teste", cmd.Title);
        Assert.Equal("HIGH", cmd.Priority);
        Assert.Equal(new DateOnly(2026, 12, 31), cmd.DueDate);
    }

    [Theory]
    [InlineData("2026-12-31T00:00:00.000Z", 2026, 12, 31)]
    [InlineData("2026-05-15", 2026, 5, 15)]
    [InlineData("2027-01-01T23:59:59Z", 2027, 1, 1)]
    public void Mapper_RequestCreateTask_WithIsoOrDateString_ParsesDueDateCorrectly(string rawDate, int year, int month, int day)
    {
        var request = new RequestCreateTask(
            spaceId: Guid.NewGuid(),
            title: "Tarefa com Data",
            dueDate: rawDate
        );

        var cmd = request.ToCommand(Guid.NewGuid());

        Assert.NotNull(cmd.DueDate);
        Assert.Equal(new DateOnly(year, month, day), cmd.DueDate.Value);
    }

    [Fact]
    public void DateOnlyTypeHandler_ParsesDateTimeAndString()
    {
        var handler = new TaskDev.Api.Infra.Configuration.OutBound.DateOnlyTypeHandler();
        var fromDateTime = handler.Parse(new DateTime(2026, 11, 23, 15, 30, 0));
        var fromString = handler.Parse("2026-11-23");
        var fromDateOnly = handler.Parse(new DateOnly(2026, 11, 23));

        Assert.Equal(new DateOnly(2026, 11, 23), fromDateTime);
        Assert.Equal(new DateOnly(2026, 11, 23), fromString);
        Assert.Equal(new DateOnly(2026, 11, 23), fromDateOnly);
    }
}
