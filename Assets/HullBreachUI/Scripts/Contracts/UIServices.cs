namespace HullBreach.UI.Contracts
{
    public sealed class UIServices
    {
        public IGameFlowService Flow;
        public IProgressionService Progression;
        public IModuleService Modules;
        public ICombatFeed Combat;
        public ICombatInputSink Input;
        public bool IsMock;
    }
}
