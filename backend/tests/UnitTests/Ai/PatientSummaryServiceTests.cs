using ClinicManagement.Application.Ai;
using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Encounters;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Shared.Results;
using UnitTests.Common;

namespace UnitTests.Ai;

public sealed class PatientSummaryServiceTests
{
    /// <summary>Stub client: ghi lại request cuối và trả kết quả/lỗi cấu hình sẵn — không gọi mạng.</summary>
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

        public static StubChatCompletionService Ok(string text = "Tóm tắt mẫu", string model = "test-model") =>
            new(new ChatCompletionResult(text, model));

        public static StubChatCompletionService Fail(Error error) =>
            new(Result.Failure<ChatCompletionResult>(error));
    }

    private static (TestDbContext db, Patient patient) SeedPatient()
    {
        var db = TestDbContext.CreateInMemory();
        var patient = new Patient("BN-000001", "Nguyễn Văn A", null, Gender.Male, null, null);
        db.Patients.Add(patient);
        db.SaveChanges();
        return (db, patient);
    }

    private static void SeedEncounter(TestDbContext db, Guid patientId)
    {
        var doctor = new Doctor("BS-000001", "BS. Trần B", Guid.NewGuid(), null, null);
        db.Doctors.Add(doctor);

        var encounter = new Encounter(
            Guid.NewGuid(), patientId, doctor.Id, "Sốt, ho", "Viêm họng cấp", "Nghỉ ngơi");
        encounter.ReplaceItems(new[]
        {
            new PrescriptionItem("Paracetamol", "500mg", 10, "Ngày 2 lần")
        });
        db.Encounters.Add(encounter);
        db.SaveChanges();
    }

    [Fact]
    public async Task SummarizeAsync_ShouldCallClient_AndReturnSummary_WhenEncountersExist()
    {
        var (db, patient) = SeedPatient();
        SeedEncounter(db, patient.Id);
        var stub = StubChatCompletionService.Ok("Bệnh nhân bị viêm họng cấp.", "claude-opus-4-8");
        var service = new PatientSummaryService(db, stub);

        var result = await service.SummarizeAsync(patient.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, stub.Calls);
        Assert.Equal(1, result.Value.EncounterCount);
        Assert.Equal("Bệnh nhân bị viêm họng cấp.", result.Value.Summary);
        Assert.Equal("claude-opus-4-8", result.Value.Model);
        // Prompt phải chứa dữ liệu bệnh án (chẩn đoán + thuốc) để LLM tóm tắt.
        var prompt = stub.LastRequest!.Messages[0].Content;
        Assert.Contains("Viêm họng cấp", prompt);
        Assert.Contains("Paracetamol", prompt);
        Assert.False(string.IsNullOrWhiteSpace(stub.LastRequest!.System));
    }

    [Fact]
    public async Task SummarizeAsync_ShouldReturnFriendlyMessage_WhenNoEncounters()
    {
        var (db, patient) = SeedPatient();
        var stub = StubChatCompletionService.Ok();
        var service = new PatientSummaryService(db, stub);

        var result = await service.SummarizeAsync(patient.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.EncounterCount);
        Assert.Equal(0, stub.Calls); // không gọi LLM khi chưa có phiếu
        Assert.Contains("chưa có phiếu khám", result.Value.Summary);
    }

    [Fact]
    public async Task SummarizeAsync_ShouldReturnNotFound_WhenPatientMissing()
    {
        var db = TestDbContext.CreateInMemory();
        var stub = StubChatCompletionService.Ok();
        var service = new PatientSummaryService(db, stub);

        var result = await service.SummarizeAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Patient.NotFound", result.Error.Code);
        Assert.Equal(0, stub.Calls);
    }

    [Fact]
    public async Task SummarizeAsync_ShouldMapError_WhenClientFails()
    {
        var (db, patient) = SeedPatient();
        SeedEncounter(db, patient.Id);
        var stub = StubChatCompletionService.Fail(
            Error.Failure("Ai.Unavailable", "Dịch vụ AI không sẵn sàng."));
        var service = new PatientSummaryService(db, stub);

        var result = await service.SummarizeAsync(patient.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.Unavailable", result.Error.Code);
    }
}
