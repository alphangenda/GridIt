namespace Web.Features.Members.Classes.GetDuplicationSources;

public record DuplicationSourceDto(
    Guid ClassId,
    string ClassName,
    string SessionName,
    string CreatorEmail,
    bool IsOwner,
    List<DuplicationSkillDto> Skills,
    List<DuplicationExamDto> Exams
);

public record DuplicationSkillDto(Guid Id, string Label);

public record DuplicationExamDto(Guid Id, string Name);
