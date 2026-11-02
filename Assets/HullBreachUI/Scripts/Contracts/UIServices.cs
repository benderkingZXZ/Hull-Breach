namespace HullBreach.UI.Contracts
{
    /// <summary>Единственная точка, через которую UI получает доступ к игровому модулю.</summary>
    public sealed class UIServices
    {
        public IGameFlowService Flow;
        public IProgressionService Progression;
        public IModuleService Modules;
        public ICombatFeed Combat;
        public ICombatInputSink Input;
        /// <summary>true, если вместо бэкенда работает заглушка (Mock/MockGameHost).</summary>
        public bool IsMock;
    }
}
