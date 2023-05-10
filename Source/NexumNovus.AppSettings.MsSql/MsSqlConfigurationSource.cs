[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NexumNovus.AppSettings.MsSql.Test")]

namespace NexumNovus.AppSettings.MsSql;

using Microsoft.Extensions.Configuration;
using NexumNovus.AppSettings.Common;

/// <summary>
/// Represents MsSql database as an <see cref="IConfigurationSource"/>.
/// </summary>
public class MsSqlConfigurationSource : NexumDbConfigurationSource
{
  /// <inheritdoc/>
  protected override IConfigurationProvider CreateProvider(IConfigurationBuilder builder)
    => new MsSqlConfigurationProvider(this);

  /// <inheritdoc/>
  protected override void EnsureDefaults()
  {
    base.EnsureDefaults();
    if (string.IsNullOrWhiteSpace(ConnectionString))
    {
      throw new ArgumentException("ConnectionString is required for MsSqlConfigurationSource");
    }
  }

  /// <summary>
  /// Gets SQL Command for table creation.
  /// </summary>
  /// MAX length for indexed varchar column is 450 so I can't put PK on Key column
  internal string CreateTableCommand => @$"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{TableName}' and xtype='U')
                                            CREATE TABLE {TableName} (
                                              [Key] nvarchar(max) COLLATE Latin1_General_100_CI_AS NOT NULL,
                                              [Value] nvarchar(max) NULL,
                                              [LastUpdateDt] datetime NOT NULL
                                            );";

#pragma warning disable SA1600, SA1516 // Elements should be documented and separated by a line
  internal string GetAllQuery => $"SELECT [Key], [Value] FROM {TableName}";
  internal string GetByKeyQuery => $"SELECT [Key], [Value] FROM {TableName} WHERE [Key] = @key or [Key] LIKE @keyLike;";
  internal string InsertCommand => $"INSERT INTO {TableName} ([Key], [Value], [LastUpdateDt]) values(@key, @value, @lastUpdateDt)";
  internal string UpdateCommand => $"UPDATE {TableName} SET [Value] = @value, [LastUpdateDt] = @lastUpdateDt WHERE [Key] = @key";
  internal string DeleteCommand => $"DELETE FROM {TableName} WHERE [Key] = @key";
  internal string LastUpdateDtQuery => $"SELECT max(LastUpdateDt) FROM {TableName}";
#pragma warning restore SA1600, SA1516 // Elements should be documented and separated by a line
}
