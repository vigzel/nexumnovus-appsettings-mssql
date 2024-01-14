namespace NexumNovus.AppSettings.MsSql.Test;

using NexumNovus.AppSettings.Common.Secure;
using NexumNovus.AppSettings.MsSql;

[Collection("Sequential")]
public class MsSqlSettingsRepository_Tests
{
  private readonly MsSqlSettingsRepository _sut;
  private readonly MsSqlConfigurationSource _source;
  private readonly DbHelperUtils _dbHelperUtils;

  public MsSqlSettingsRepository_Tests()
  {
    var mockProtector = new Mock<ISecretProtector>();
    mockProtector.Setup(x => x.Protect(It.IsAny<string>())).Returns("***");

    _source = new MsSqlConfigurationSource
    {
      ConnectionString = "Data Source=.;Initial Catalog=NexumNovus;Integrated Security=true;TrustServerCertificate=True;",
      Protector = mockProtector.Object,
      ReloadOnChange = false,
    };
    _dbHelperUtils = new DbHelperUtils(_source);

    _sut = new MsSqlSettingsRepository(_source);

    _dbHelperUtils.CreateDb();
    _dbHelperUtils.CleanDb();
  }

  [Fact]
  public async Task Update_Key_Should_Be_Case_Insensitive_Async()
  {
    // Arrange
    var initialSettings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
      {
        { "Name", "test" },
        { "Age", "36" },
      };
    _dbHelperUtils.SeedDb(initialSettings);

    // Act
    await _sut.UpdateSettingsAsync("name", "New Name"); // key should be case-insensitive

    // Assert
    var result = _dbHelperUtils.GetAllDbSettings();
    result.Count.Should().Be(2);
    result["name"].Should().Be("New Name");
  }

  [Fact]
  public async Task Should_Add_New_Setting_Async()
  {
    // Arrange
    var initialSettings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
      {
        { "Name", "test" },
        { "Age", "36" },
      };
    _dbHelperUtils.SeedDb(initialSettings);

    // Act
    await _sut.UpdateSettingsAsync("Surname", "New Surname");

    // Assert
    var result = _dbHelperUtils.GetAllDbSettings();
    result.Count.Should().Be(3);
    result["surname"].Should().Be("New Surname");
  }

  [Fact]
  public async Task Should_Protect_Settings_With_Secret_Attribute_Async()
  {
    // Arrange
    var initialSettings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
      {
        { "Name", "test" },
        { "Age", "36" },
      };
    _dbHelperUtils.SeedDb(initialSettings);

    // Act
    await _sut.UpdateSettingsAsync("Account", new TestSetting
    {
      Name = "demo",
      Password = "demo",
    });

    // Assert
    var result = _dbHelperUtils.GetAllDbSettings();
    result.Count.Should().Be(6);
    result["Account:Name"].Should().Be("demo");
    result["Account:Password*"].Should().Be("***");
  }

  [Fact]
  public async Task Should_Update_Complex_Objects_Async()
  {
    // Arrange
    var initialSettings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
      {
        { "Name", "test" },
        { "Age", "36" },
      };
    _dbHelperUtils.SeedDb(initialSettings);

    // Act
    await _sut.UpdateSettingsAsync("Account", new TestSetting
    {
      Name = "demo",
      Password = "demo",
      Types = new[] { "A", "B", "C" },
      Data = new Dictionary<string, int>
      {
        { "A", 1 },
        { "B", 2 },
      },
    });

    // Assert
    var result = _dbHelperUtils.GetAllDbSettings();
    result.Count.Should().Be(9);
    result["Account:Types:0"].Should().Be("A");
    result["Account:Types:1"].Should().Be("B");
    result["Account:Types:2"].Should().Be("C");
    result["Account:Data:A"].Should().Be("1");
    result["Account:Data:B"].Should().Be("2");
  }

  private sealed class TestSetting
  {
    public string Name { get; set; } = string.Empty;

    [SecretSetting]
    public string Password { get; set; } = string.Empty;

    public IList<string>? Types { get; set; }

    public IDictionary<string, int>? Data { get; set; }
  }
}
