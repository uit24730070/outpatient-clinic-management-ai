using ClinicManagement.Application.Assistant;
using ClinicManagement.Application.Assistant.Dtos;
using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Shared.Results;
using UnitTests.Common;

namespace UnitTests.Assistant;

public sealed class AssistantServiceTests
{
    /// <summary>Công cụ giả lập: ghi lại số lần gọi + context, trả kết quả cố định.</summary>
    private sealed class StubTool : IAssistantTool
    {
        private readonly string _output;
        public int Calls { get; private set; }
        public AssistantContext? LastContext { get; private set; }
        public string? LastArguments { get; private set; }

        public StubTool(string name, string output)
        {
            _output = output;
            Definition = new AiTool(name, "công cụ thử", new[]
            {
                new AiToolParameter("q", "string", "tham số thử")
            });
        }

        public AiTool Definition { get; }

        public Task<string> ExecuteAsync(string argumentsJson, AssistantContext context, CancellationToken ct = default)
        {
            Calls++;
            LastContext = context;
            LastArguments = argumentsJson;
            return Task.FromResult(_output);
        }
    }

    private static AssistantChatRequest Ask(string text) =>
        new(new[] { new AssistantMessageDto("user", text) });

    private static readonly AssistantContext AdminContext =
        new(Guid.NewGuid(), "Admin", null);

    [Fact]
    public async Task ChatAsync_ShouldReturnAnswer_WithoutCallingTools()
    {
        var client = StubAssistantCompletionService.Sequence(
            StubAssistantCompletionService.Text("Xin chào, tôi có thể giúp gì?"));
        var tool = new StubTool("noop", "{}");
        var service = new AssistantService(client, new[] { (IAssistantTool)tool });

        var result = await service.ChatAsync(Ask("Chào bạn"), AdminContext);

        Assert.True(result.IsSuccess);
        Assert.Equal("Xin chào, tôi có thể giúp gì?", result.Value.Answer);
        Assert.Empty(result.Value.ToolCalls);
        Assert.Equal(0, tool.Calls);
        Assert.Equal(1, client.Calls);
    }

    [Fact]
    public async Task ChatAsync_ShouldExecuteTool_ThenReturnFinalAnswer()
    {
        var tool = new StubTool("search_patients", "{\"total\":1,\"items\":[{\"fullName\":\"Nguyễn Văn A\"}]}");
        var client = StubAssistantCompletionService.Sequence(
            StubAssistantCompletionService.ToolUse("tu_1", "search_patients", "{\"query\":\"A\"}"),
            StubAssistantCompletionService.Text("Tìm thấy bệnh nhân Nguyễn Văn A."));
        var service = new AssistantService(client, new[] { (IAssistantTool)tool });

        var result = await service.ChatAsync(Ask("Tìm bệnh nhân tên A"), AdminContext);

        Assert.True(result.IsSuccess);
        Assert.Equal("Tìm thấy bệnh nhân Nguyễn Văn A.", result.Value.Answer);
        Assert.Equal(1, tool.Calls);
        Assert.Equal(2, client.Calls);
        // Công cụ đã gọi được ghi lại để truy vết.
        Assert.Single(result.Value.ToolCalls);
        Assert.Equal("search_patients", result.Value.ToolCalls[0].Name);
        // Context được truyền đúng xuống công cụ.
        Assert.Equal(AdminContext.UserId, tool.LastContext!.UserId);
        Assert.Contains("query", tool.LastArguments);
    }

    [Fact]
    public async Task ChatAsync_ShouldStop_WhenToolLoopExceeded()
    {
        var tool = new StubTool("search_patients", "{}");
        // LLM luôn đòi gọi công cụ → phải chặn sau số vòng tối đa.
        var client = StubAssistantCompletionService.Always(
            StubAssistantCompletionService.ToolUse("tu", "search_patients", "{}"));
        var service = new AssistantService(client, new[] { (IAssistantTool)tool });

        var result = await service.ChatAsync(Ask("Lặp mãi"), AdminContext);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.ToolLoopExceeded", result.Error.Code);
        Assert.Equal(ErrorType.Failure, result.Error.Type);
        // 6 lần gọi LLM (vòng 0..5), sau đó dừng.
        Assert.Equal(6, client.Calls);
    }

    [Fact]
    public async Task ChatAsync_ShouldReturnValidation_WhenNoUserMessage()
    {
        var client = StubAssistantCompletionService.Sequence(
            StubAssistantCompletionService.Text("không nên tới đây"));
        var service = new AssistantService(client, Array.Empty<IAssistantTool>());

        var result = await service.ChatAsync(
            new AssistantChatRequest(new[] { new AssistantMessageDto("user", "   ") }), AdminContext);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.QuestionRequired", result.Error.Code);
        Assert.Equal(0, client.Calls);
    }

    [Fact]
    public async Task ChatAsync_ShouldSurfaceError_WhenClientFails()
    {
        var client = StubAssistantCompletionService.Sequence(
            Result.Failure<AssistantCompletionResult>(
                Error.Failure("Ai.Unavailable", "Dịch vụ AI không sẵn sàng.")));
        var service = new AssistantService(client, Array.Empty<IAssistantTool>());

        var result = await service.ChatAsync(Ask("Câu hỏi"), AdminContext);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.Unavailable", result.Error.Code);
    }

    [Fact]
    public async Task ChatAsync_ShouldReportUnknownTool_WithoutCrashing()
    {
        var client = StubAssistantCompletionService.Sequence(
            StubAssistantCompletionService.ToolUse("tu_1", "khong_ton_tai", "{}"),
            StubAssistantCompletionService.Text("Đã xử lý."));
        var service = new AssistantService(client, Array.Empty<IAssistantTool>());

        var result = await service.ChatAsync(Ask("Gọi công cụ lạ"), AdminContext);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.ToolCalls);
        Assert.Contains("Không có công cụ", result.Value.ToolCalls[0].Result);
    }
}
