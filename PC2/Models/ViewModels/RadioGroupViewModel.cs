namespace PC2.Models.ViewModels
{
    /// <summary>
    /// A group of radio buttons rendered by the _RadioGroup partial
    /// </summary>
    /// <param name="Name">The name of the form field the selected choice is submitted as</param>
    /// <param name="Legend">The question shown above the choices</param>
    /// <param name="Choices">The choices to show</param>
    /// <param name="Selected">The choice to show as selected, if any</param>
    /// <param name="RequiredMessage">The error shown when nothing is selected, or null if the question is optional</param>
    public record RadioGroupViewModel(string Name, string Legend, IReadOnlyList<string> Choices, string? Selected,
        string? RequiredMessage = null)
    {
        public bool IsRequired => RequiredMessage != null;
    }
}
