using System;

namespace CodeBrix.LibGit2.Tests.TestHelpers; //was previously: LibGit2Sharp.Tests.TestHelpers;

public static class DateTimeOffsetExtensions
{
    public static DateTimeOffset TruncateMilliseconds(this DateTimeOffset dto) => new DateTimeOffset(dto.Year, dto.Month, dto.Day, dto.Hour, dto.Minute, dto.Second, dto.Offset);
}
