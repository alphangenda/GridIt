namespace Web.Features.Members.Classes.GetDuplicationSources;

public record DuplicationSourceDto(
    Guid ClassId,
    string ClassName,
    string SessionName,
    string CreatorEmail,
    bool IsOwner,
    string? ProgramName,
    List<DuplicationSkillDto> Skills
);

public record DuplicationSkillDto(Guid Id, string Label);

