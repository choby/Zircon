namespace Server.Web.Models;

public sealed record ConfigurationField(
    string Section,
    string Name,
    string DisplayName,
    Type ValueType,
    string Value,
    bool IsSecret,
    string ApplyMode);

public sealed record ConfigurationSectionModel(string Name, IReadOnlyList<ConfigurationField> Fields);
