using System.Collections.ObjectModel;

namespace AutoTool.Services.Logging
{
    public class LogMessageService
    {
        public ObservableCollection<string> Messages { get; } = new ObservableCollection<string>();

        public void AddEntry(string entry)
        {
            // Keep UI-friendly limit
            Messages.Add(entry);
            if (Messages.Count > 5000)
            {
                while (Messages.Count > 4000) Messages.RemoveAt(0);
            }
        }

        public void Clear() => Messages.Clear();
    }
}
