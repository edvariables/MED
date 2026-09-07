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
        public override string? Script
        {
            get => base.Script;
            set
            {
                RemoveConsumers();

                base.Script = value;

                AddConsumers();
            }
        }

        private IGameController? GetGameController() => ProcessStatic.GetGameController(Process);

        private void AddConsumers()
        {
            if (Process is not IConsumer consumer
                || string.IsNullOrEmpty(Script))
                return;
            var gameController = GetGameController();
            if (gameController == null)
                return;
            var script = ClearComments(Script).Replace(" ", "");
            var matches = Regex.Matches(script, @"if\(.*(property==|gameController\.GetControllerPropertyValue\()""([^""]+)""");
            foreach (var match in matches)
                if (match != null && match is Match match1)
                    gameController.AddConsumer(consumer, match1.Groups[2].Value, Process.GameControllerChanged);
        }
        private void RemoveConsumers()
        {
            if (Process is not IConsumer consumer
                || string.IsNullOrEmpty(Script))
                return;
            var gameController = GetGameController();
            if (gameController == null)
                return;
            gameController.RemoveConsumer(consumer, "");
        }
    }
}
