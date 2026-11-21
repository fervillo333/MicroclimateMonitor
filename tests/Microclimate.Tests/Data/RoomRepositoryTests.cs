using Microclimate.Data;
using Microclimate.Tests.Helpers;

namespace Microclimate.Tests.Data;

public class RoomRepositoryTests
{
    [Fact]
    public void Add_ReturnsRoomWithId_AndCanBeFound()
    {
        using var db = new TestDb();
        var repo = new RoomRepository(db.Database);

        var room = repo.Add("Office", "2nd floor");

        Assert.True(room.Id > 0);
        Assert.Equal(room, repo.GetByName("Office"));
    }

    [Fact]
    public void Add_TrimsName()
    {
        using var db = new TestDb();
        var repo = new RoomRepository(db.Database);
        Assert.Equal("Lab", repo.Add("  Lab  ").Name);
    }

    [Fact]
    public void Add_Duplicate_Throws()
    {
        using var db = new TestDb();
        var repo = new RoomRepository(db.Database);
        repo.Add("Office");
        Assert.Throws<InvalidOperationException>(() => repo.Add("Office"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Add_BlankName_Throws(string name)
    {
        using var db = new TestDb();
        Assert.Throws<ArgumentException>(() => new RoomRepository(db.Database).Add(name));
    }

    [Fact]
    public void GetByName_Missing_ReturnsNull()
    {
        using var db = new TestDb();
        var repo = new RoomRepository(db.Database);
        Assert.Null(repo.GetByName("Nope"));
        Assert.Null(repo.GetByName(""));
    }

    [Fact]
    public void GetAll_IsSortedByName()
    {
        using var db = new TestDb();
        var repo = new RoomRepository(db.Database);
        repo.Add("B");
        repo.Add("A");
        Assert.Equal(new[] { "A", "B" }, repo.GetAll().Select(r => r.Name));
    }

    [Fact]
    public void SqlInjectionInName_IsStoredAsPlainText()
    {
        using var db = new TestDb();
        var repo = new RoomRepository(db.Database);
        const string evil = "x'); DROP TABLE rooms;--";

        repo.Add(evil);
        repo.Add("Still works");

        Assert.Equal(2, repo.GetAll().Count);
        Assert.NotNull(repo.GetByName(evil));
    }

    [Fact]
    public void Constructor_NullDb_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new RoomRepository(null!));
}
