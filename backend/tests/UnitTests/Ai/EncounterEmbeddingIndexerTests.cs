using ClinicManagement.Application.Ai;
using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Encounters;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Shared.Results;
using UnitTests.Common;

namespace UnitTests.Ai;

public sealed class EncounterEmbeddingIndexerTests
{
    private static (TestDbContext db, Encounter encounter) SeedEncounter()
    {
        var db = TestDbContext.CreateInMemory();
        var patient = new Patient("BN-000001", "Nguyễn Văn A", null, Gender.Male, null, null);
        var doctor = new Doctor("BS-000001", "BS. Trần B", Guid.NewGuid(), null, null);
        db.Patients.Add(patient);
        db.Doctors.Add(doctor);

        var encounter = new Encounter(
            Guid.NewGuid(), patient.Id, doctor.Id, "Sốt, ho", "Viêm họng cấp", "Nghỉ ngơi");
        encounter.ReplaceItems(new[] { new PrescriptionItem("Paracetamol", "500mg", 10, "Ngày 2 lần") });
        db.Encounters.Add(encounter);
        db.SaveChanges();
        return (db, encounter);
    }

    [Fact]
    public async Task IndexAsync_ShouldEmbedAndStore_WithPatientSnapshotAndModel()
    {
        var (db, encounter) = SeedEncounter();
        var embeddings = StubEmbeddingService.FromMap(_ => new float[] { 1f, 0f, 0f });
        var store = new InMemoryEncounterEmbeddingStore();
        var indexer = new EncounterEmbeddingIndexer(db, embeddings, store);

        await indexer.IndexAsync(encounter.Id);

        Assert.Equal(1, store.Count);
        Assert.Equal(1, embeddings.Calls);
        // Embed nội dung phiếu với input_type = document.
        Assert.Equal(ClinicManagement.Application.Common.Ai.EmbeddingInputType.Document, embeddings.LastInputType);
        Assert.Contains("Viêm họng cấp", embeddings.LastInputs![0]);

        var matches = await store.SearchAsync(encounter.PatientId, new float[] { 1f, 0f, 0f }, 5);
        Assert.Single(matches);
        Assert.Equal(encounter.Id, matches[0].EncounterId);
    }

    [Fact]
    public async Task IndexAsync_ShouldNotThrowNorStore_WhenEmbeddingFails()
    {
        var (db, encounter) = SeedEncounter();
        var embeddings = StubEmbeddingService.Fail(
            Error.Failure("Embedding.Unavailable", "Dịch vụ embedding không sẵn sàng."));
        var store = new InMemoryEncounterEmbeddingStore();
        var indexer = new EncounterEmbeddingIndexer(db, embeddings, store);

        await indexer.IndexAsync(encounter.Id); // không được ném lỗi

        Assert.Equal(0, store.Count);
    }

    [Fact]
    public async Task ReindexAllAsync_ShouldIndexEveryEncounter()
    {
        var (db, encounter) = SeedEncounter();
        // Phiếu thứ hai của cùng bệnh nhân.
        var e2 = new Encounter(Guid.NewGuid(), encounter.PatientId, encounter.DoctorId, null, "Cúm mùa", null);
        db.Encounters.Add(e2);
        db.SaveChanges();

        var embeddings = StubEmbeddingService.FromMap(_ => new float[] { 0.5f, 0.5f, 0f });
        var store = new InMemoryEncounterEmbeddingStore();
        var indexer = new EncounterEmbeddingIndexer(db, embeddings, store);

        var count = await indexer.ReindexAllAsync();

        Assert.Equal(2, count);
        Assert.Equal(2, store.Count);
    }
}
