namespace NexumNovus.AppSettings.MsSql;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NexumNovus.AppSettings.Common;
using NexumNovus.AppSettings.Common.Utils;

/// <summary>
/// A MsSql based implementation of <see cref="IConfigurationProvider"/>.
/// </summary>
public class MsSqlConfigurationProvider : NexumDbConfigurationProvider<MsSqlConfigurationSource>
{
  private bool _dbInitialized;
  private object _dbLock = new ();

  /// <summary>
  /// Initializes a new instance of the <see cref="MsSqlConfigurationProvider"/> class.
  /// </summary>
  /// <param name="source">The source settings.</param>
  public MsSqlConfigurationProvider(MsSqlConfigurationSource source)
    : base(source)
  {
  }

  /// <summary>
  /// Gets settings from database.
  /// </summary>
  /// <returns>Key-value pairs of settings.</returns>
  protected override Dictionary<string, string?> GetSettingsFromDb()
  {
    LazyAction.EnsureInitialized(ref _dbInitialized, ref _dbLock, EnsureCreated);

    var settings = new Dictionary<string, string?>();
    using (var connection = new SqlConnection(Source.ConnectionString))
    {
      connection.Open();

      var command = connection.CreateCommand();
      command.CommandText = Source.GetAllQuery;

      using (var reader = command.ExecuteReader())
      {
        while (reader.Read())
        {
          var key = reader.GetString(0);
          var value = reader.IsDBNull(1) ? null : reader.GetString(1);
          settings.Add(key, value);
        }
      }
    }

    return settings;
  }

  /// <summary>
  /// Gets last update date for settings.
  /// </summary>
  /// <returns>Date of the last update.</returns>
  protected override string? GetLastUpdateDt()
  {
    LazyAction.EnsureInitialized(ref _dbInitialized, ref _dbLock, EnsureCreated);

    using (var connection = new SqlConnection(Source.ConnectionString))
    {
      connection.Open();
      using (var command = connection.CreateCommand())
      {
        command.CommandText = Source.LastUpdateDtQuery;
        return command.ExecuteScalar()?.ToString();
      }
    }
  }

  private void EnsureCreated()
  {
    using (var connection = new SqlConnection(Source.ConnectionString))
    {
      connection.Open();

      var command = connection.CreateCommand();
      command.CommandText = Source.CreateTableCommand;
      command.ExecuteNonQuery();
    }
  }
}
