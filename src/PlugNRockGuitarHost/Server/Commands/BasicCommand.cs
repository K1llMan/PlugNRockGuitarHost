namespace PlugNRockGuitarHost.Server.Commands
{
    public class BasicCommand<TI, TO> : IBasicCommand
    {
        #region Поля

        private Func<TI?, TO?> handler;

        #endregion Поля

        #region Свойства

        public virtual string Type => "";

        #endregion Свойства

        #region Основные функции

        public BasicCommand(Func<TI?, TO?> action)
        {
            handler = action;
        }

        public TO? Execute(TI? parameters)
        {
            return handler(parameters);
        }

        public object? Execute(object? input = null)
        {
            return Execute((TI) input);
        }

        #endregion Основные функции
    }
}