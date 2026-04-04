namespace cppQuest.Server.Models;

/// <summary>
/// Разрешённый ISU-идентификатор — используется для whitelist-студентов.
/// </summary>
public class AllowedIsu
{
    /// <summary> PK </summary>
    public int Id { get; set; }

    /// <summary> Номер ISU (например "s12345"). </summary>
    public string IsuNumber { get; set; } = "";
}