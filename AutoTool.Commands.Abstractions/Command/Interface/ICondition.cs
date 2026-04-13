namespace AutoTool.Commands.Interface
{
    public interface ICondition
    {
        IConditionSettings Settings { get; }

        Task<bool> Evaluate(CancellationToken cancellationToken);
    }
}
