using ClinicManagement.Application.Ai;
using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Encounters;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Shared.Results;
using UnitTests.Common;

namespace UnitTests.Ai;

public sealed class PatientQuestionServiceTests
{
    /// <summary>Stub client chat: ghi lại request cuối, trả kết quả/lỗi cấu hình sẵn — không gọi mạng.</summary>
    private sealed class StubChatCompletionService : IChatCompletionService
    {
        private readonly Result<ChatCompletionResult> _result;
        public int Calls { get; private set; }
        public ChatCompletionRequest? LastRequest { get; private set; }

        public StubChatCompletionService(Result<ChatCompletionResult> result) => _result = result;

        public Task<Result<ChatCompletionResult>> CompleteAsync(
            ChatCompletionRequest request, CancellationToken ct = default)
        {
            Calls++;
            LastRequest = request;
            return Task.FromResult(_result);
        }

        public static StubChatCompletionService Ok(string text = "Trả lời mẫu", string model = "test-model") =>
            new(new ChatCompletionResult(text, model));
    }

    private static (TestDbContext db, Patient patient, Encounter a, Encounter b) SeedTwoEncounters()
    {
        var db = TestDbContext.CreateInMemory();
        var patient = new Patient("BN-000001", "Nguyễn Văn A", null, Gender.Male, null, null);
        var doctor = new Doctor("BS-000001", "BS. Trần B", Guid.NewGuid(), null, null);
        db.Patients.Add(patient);
        db.Doctors.Add(doctor);

        var a = new Encounter(Guid.NewGuid(), patient.Id, doctor.Id, "Sốt, ho", "Viêm họng cấp", null);
        var b = new Encounter(Guid.NewGuid(), patient.Id, doctor.Id, "Đau đầu", "Tăng huyết áp", null);
        db.Encounters.Add(a);
        db.Encounters.Add(b);
        db.SaveChanges();
        return (db, patient, a, b);
    }

    /// <summary>Map embedding: câu hỏi → vector gần phiếu A ([1,0,0]); dùng chung để nạp store.</summary>
    private static StubEmbeddingService QueryCloserToA() =>
        StubEmbeddingService.FromMap(_ => new float[] { 0.9f, 0.1f, 0f });

    [Fact]
    public async Task AnswerAsync_ShouldRetrieveTopK_AndAnswerWithSources()
    {
        var (db, patient, a, b) = SeedTwoEncounters();
        var store = new InMemoryEncounterEmbeddingStore();
        await store.UpsertAsync(new EncounterEmbeddingRecord(a.Id, patient.Id, new float[] { 1f, 0f, 0f }, "stub"));
        await store.UpsertAsync(new EncounterEmbeddingRecord(b.Id, patient.Id, new float[] { 0f, 1f, 0f }, "stub"));

        var chat = StubChatCompletionService.Ok("Bệnh nhân từng bị viêm họng cấp.", "claude-opus-4-8");
        var service = new PatientQuestionService(db, QueryCloserToA(), store, chat);

        var result = await service.AnswerAsync(patient.Id, "Tiền sử viêm họng?");

        Assert.True(result.IsSuccess);
        Assert.Equal(1, chat.Calls);
        Assert.Equal("claude-opus-4-8", result.Value.Model);
        Assert.NotEmpty(result.Value.Sources);
        // Phiếu A (viêm họng) tương đồng cao hơn → đứng đầu nguồn.
        Assert.Equal(a.Id, result.Value.Sources[0].EncounterId);
        // Prompt phải chứa nội dung phiếu được truy hồi.
        Assert.Contains("Viêm họng cấp", chat.LastRequest!.Messages[0].Content);
        Assert.False(string.IsNullOrWhiteSpace(chat.LastRequest!.System));
    }

    [Fact]
    public async Task AnswerAsync_ShouldReturnFriendly_WhenNoEmbeddingsIndexed()
    {
        var (db, patient, _, _) = SeedTwoEncounters();
        var store = new InMemoryEncounterEmbeddingStore(); // chưa lập chỉ mục phiếu nào
        var chat = StubChatCompletionService.Ok();
        var service = new PatientQuestionService(db, QueryCloserToA(), store, chat);

        var result = await service.AnswerAsync(patient.Id, "Tiền sử bệnh?");

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Sources);
        Assert.Equal(0, chat.Calls); // không gọi LLM khi không truy hồi được gì
        Assert.Contains("Chưa có dữ liệu", result.Value.Answer);
    }

    [Fact]
    public async Task AnswerAsync_ShouldReturnNotFound_WhenPatientMissing()
    {
        var db = TestDbContext.CreateInMemory();
        var embeddings = QueryCloserToA();
        var service = new PatientQuestionService(db, embeddings, new InMemoryEncounterEmbeddingStore(),
            StubChatCompletionService.Ok());

        var result = await service.AnswerAsync(Guid.NewGuid(), "Câu hỏi?");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Patient.NotFound", result.Error.Code);
        Assert.Equal(0, embeddings.Calls); // kiểm tra bệnh nhân trước khi embed
    }

    [Fact]
    public async Task AnswerAsync_ShouldMapError_WhenEmbeddingFails()
    {
        var (db, patient, _, _) = SeedTwoEncounters();
        var embeddings = StubEmbeddingService.Fail(
            Error.Failure("Embedding.Unavailable", "Dịch vụ embedding không sẵn sàng."));
        var service = new PatientQuestionService(db, embeddings, new InMemoryEncounterEmbeddingStore(),
            StubChatCompletionService.Ok());

        var result = await service.AnswerAsync(patient.Id, "Câu hỏi?");

        Assert.True(result.IsFailure);
        Assert.Equal("Embedding.Unavailable", result.Error.Code);
    }

    [Fact]
    public async Task AnswerAsync_ShouldReturnValidation_WhenQuestionEmpty()
    {
        var (db, patient, _, _) = SeedTwoEncounters();
        var embeddings = QueryCloserToA();
        var service = new PatientQuestionService(db, embeddings, new InMemoryEncounterEmbeddingStore(),
            StubChatCompletionService.Ok());

        var result = await service.AnswerAsync(patient.Id, "   ");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Ai.QuestionRequired", result.Error.Code);
        Assert.Equal(0, embeddings.Calls);
    }
}
