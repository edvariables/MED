using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MED
{
    public class GameControllerScript(IProcess process, string eventName) : EventScript(process, eventName)
    {

        private IGameController? GetGameController() => ProcessStatic.GetGameController(Process);

        protected override void AddConsumers()
        {
            ConsumerProperties = [];

            if (Process is not IConsumer consumer
                || string.IsNullOrEmpty(Script))
                return;
            var gameController = GetGameController();
            if (gameController == null)
                return;
            var script = ClearComments(Script).Replace(" ", "");
            var matches = Regex.Matches(script, @".*(if\(.*eventProperty==|gameController\.GetControllerPropertyValue\(|GetControllerState\()(""([^""]+)""|Keys\.(\w+))");
            foreach (var match in matches)
                if (match != null && match is Match match1)
                {
                    var capture = match1.Groups[3].Value;
                    if (string.IsNullOrEmpty(capture))
                        capture = match1.Groups[4].Value;
                    if (!string.IsNullOrEmpty(capture)
                    && !(ConsumerProperties.ContainsKey(gameController)
                        && ConsumerProperties[gameController].Contains(capture))
                    )
                    {
                        gameController.AddConsumer(consumer, capture, Process.GameControllerChanged);
                        if (!ConsumerProperties.ContainsKey(gameController))
                            ConsumerProperties[gameController] = new();
                        ConsumerProperties[gameController].Add(capture);
                    }
                }
        }
        protected override void RemoveConsumers()
        {
            ConsumerProperties = [];

            if (Process is not IConsumer consumer
                || string.IsNullOrEmpty(Script))
                return;
            var gameController = GetGameController();
            if (gameController == null)
                return;
            gameController.RemoveConsumer(consumer, "");
        }

        public override Type ScriptGlobalsType { get; } = typeof(ScriptGlobalsGameController);

        public class ScriptGlobalsGameController(IProcess process, object[]? parameters) : ScriptGlobals(process, parameters)
        {
            public object? GetControllerState(Keys key) => GetControllerState(key.ToString());

            public object? GetControllerState(string key)
            {
                if (this._params_ != null
                && this._params_[0] is IGameController gameController)
                    return gameController.GetControllerPropertyValue(key);
                return null;
            }
        }
    }
}
