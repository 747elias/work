using Flowstate.Core.Models;

namespace Flowstate.Core.Validation;

public static class TaskValidator
{
    public const int MaxTitleLength = 200;

    public static IReadOnlyList<string> Validate(FlowTask task)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(task.Title))
            errors.Add("Der Titel darf nicht leer sein.");
        else if (task.Title.Length > MaxTitleLength)
            errors.Add($"Der Titel darf hoechstens {MaxTitleLength} Zeichen lang sein.");
        if (task.EstimatedMinutes <= 0)
            errors.Add("Die geschaetzte Dauer muss groesser als 0 Minuten sein.");
        if (task.EstimatedMinutes > 24 * 60)
            errors.Add("Die geschaetzte Dauer darf hoechstens 24 Stunden betragen.");
        if (task.CanSplit && task.MinChunkMinutes <= 0)
            errors.Add("Die minimale Blocklaenge muss groesser als 0 Minuten sein.");
        return errors;
    }
}
