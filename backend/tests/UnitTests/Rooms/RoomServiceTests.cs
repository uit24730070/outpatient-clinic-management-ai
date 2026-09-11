using ClinicManagement.Application.Rooms;
using ClinicManagement.Application.Rooms.Dtos;
using ClinicManagement.Shared.Results;
using UnitTests.Common;

namespace UnitTests.Rooms;

public sealed class RoomServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldGenerateSequentialCodes()
    {
        var db = TestDbContext.CreateInMemory();
        var service = new RoomService(db);

        var first = await service.CreateAsync(new CreateRoomRequest("Phòng 101", null));
        var second = await service.CreateAsync(new CreateRoomRequest("Phòng 102", "Tầng 1"));

        Assert.Equal("PK-000001", first.Value.Code);
        Assert.Equal("PK-000002", second.Value.Code);
    }

    [Fact]
    public async Task GetListAsync_ShouldFilterBySearch()
    {
        var db = TestDbContext.CreateInMemory();
        var service = new RoomService(db);
        await service.CreateAsync(new CreateRoomRequest("Phòng Nội tổng quát", null));
        await service.CreateAsync(new CreateRoomRequest("Phòng Tim mạch", null));

        var result = await service.GetListAsync(1, 20, "tim", default);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("Phòng Tim mạch", result.Value.Items[0].Name);
    }

    [Fact]
    public async Task DeleteAsync_ShouldHideRoom_ButKeepCodeSequence()
    {
        var db = TestDbContext.CreateInMemory();
        var service = new RoomService(db);
        var first = await service.CreateAsync(new CreateRoomRequest("Phòng 101", null));

        var delete = await service.DeleteAsync(first.Value.Id);
        Assert.True(delete.IsSuccess);

        var getDeleted = await service.GetByIdAsync(first.Value.Id);
        Assert.True(getDeleted.IsFailure);
        Assert.Equal("Room.NotFound", getDeleted.Error.Code);

        // Mã tiếp theo vẫn nối tiếp (đếm cả bản ghi đã xoá) để không trùng.
        var second = await service.CreateAsync(new CreateRoomRequest("Phòng 102", null));
        Assert.Equal("PK-000002", second.Value.Code);
    }

    [Fact]
    public async Task UpdateAsync_ShouldFail_WhenNotFound()
    {
        var db = TestDbContext.CreateInMemory();
        var service = new RoomService(db);

        var result = await service.UpdateAsync(Guid.NewGuid(), new UpdateRoomRequest("X", null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
