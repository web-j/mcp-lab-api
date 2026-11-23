using System.Text.Json.Serialization;

namespace McpLab.Context.Notes;

[JsonConverter(typeof(JsonStringEnumConverter<NoteKind>))]
public enum NoteKind
{
    [JsonStringEnumMemberName("goal")] Goal,
    [JsonStringEnumMemberName("pending")] Pending,
    [JsonStringEnumMemberName("decision")] Decision,
    [JsonStringEnumMemberName("preference")] Preference,
    [JsonStringEnumMemberName("fact")] Fact,
    [JsonStringEnumMemberName("handoff")] Handoff,
}

[JsonConverter(typeof(JsonStringEnumConverter<NoteAction>))]
public enum NoteAction
{
    [JsonStringEnumMemberName("complete")] Complete,
    [JsonStringEnumMemberName("reopen")] Reopen,
    [JsonStringEnumMemberName("pin")] Pin,
    [JsonStringEnumMemberName("unpin")] Unpin,
    [JsonStringEnumMemberName("edit")] Edit,
}

internal static class NoteKinds
{
    public const string Open = "open";
    public const string Done = "done";

    public static string Code(this NoteKind kind) => kind switch
    {
        NoteKind.Goal => "goal",
        NoteKind.Pending => "pending",
        NoteKind.Decision => "decision",
        NoteKind.Preference => "preference",
        NoteKind.Fact => "fact",
        NoteKind.Handoff => "handoff",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static string Label(string code) => code switch
    {
        "goal" => "objetivo",
        "pending" => "pendência",
        "decision" => "decisão",
        "preference" => "preferência",
        "fact" => "fato",
        "handoff" => "onde paramos",
        _ => code,
    };
}
