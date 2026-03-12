namespace AgroCDDotnet.Api.Tests;

public sealed record TodoContract(Guid Id, string Title, bool IsCompleted, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
