using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MED.GameController;

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
            var pattern = @"eventProperty==|gameController\.GetControllerPropertyValue\(|controllerState\(|controllerPressed\(";
            var matches = Regex.Matches(script, @"\b(" + pattern + @")(""(?<key>[^""]+)""|Keys\.(?<Keys>\w+))", RegexOptions.ExplicitCapture);
            foreach (var match in matches)
                if (match != null && match is Match match1)
                {
                    var capture = match1.Groups["key"].Value;
                    if (string.IsNullOrEmpty(capture))
                        capture = match1.Groups["Keys"].Value;
                    if (!string.IsNullOrEmpty(capture)
                    && !(ConsumerProperties.ContainsKey(gameController)
                        && ConsumerProperties[gameController].Contains(capture))
                    )
                    {
                        gameController.AddConsumer(consumer, capture, Process.OnGameControllerChanged);
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

        public class ScriptGlobalsGameController(IProcess process, object?[] parameters) : ScriptGlobals(process, parameters)
        {
            public bool controllerPressed(Keys key) => controllerPressed(key.ToString());
            public object? controllerState(Keys key) => controllerState(key.ToString());

            public bool controllerPressed(string key)
            {
                if(controllerState(key) is bool pressed)
                    return pressed;
                return false;
            }
            public object? controllerState(string key)
            {
                if (this._params_ != null
                && this._params_[0] is IGameController gameController)
                    return gameController.GetControllerPropertyValue(key);
                return null;
            }
        }
    }
}
