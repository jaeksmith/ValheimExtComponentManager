using System;
using System.Collections.Generic;
using CommandLine;

namespace ValheimExtComponentManager
{
    public class ProgramOptions
    {
        // --ext-component-manager::check-update { yes, no }
        [Option("ext-component-manager::check-update", Required = false, HelpText = "Specifies whether the component-manager should be updated if new versions are available.")]
        public string ExtComponentManagerCheckUpdate { get; set; }

        // --valheim-vr::check-update { yes, no }
        [Option("valheim-vr::check-update", Required = false, HelpText = "Specifies whether the VR extensions should be updated if new versions are available.")]
        public string ValheimVrCheckUpdate { get; set; }

        // --valheim-vr::enable { yes, no }
        [Option("valheim-vr::enable", Required = false, HelpText = "Specifies whether the VR extensions be enabled (installed/active) or not.")]
        public string ValheimVrEnabled { get; set; }

        // --voice-commander::check-update { yes, no }
        [Option("voice-commander::check-update", Required = false, HelpText = "Specifies whether the voice-commander extensions should be updated if new versions are available.")]
        public string VoiceCommanderCheckUpdate { get; set; }

        // --voice-commander::manage { run-while-valheim-runs, no, restart, start-if-not-running, stop }
        [Option("voice-commander::manage", Required = false, HelpText = "Specifies how the manager will handle the voice-commander.")]
        public string VoiceCommanderManage { get; set; }

        // --valheim::start { yes, no }
        [Option("valheim::start", Required = false, HelpText = "Specifies if Valheim should be started and with or without VR mods.")]
        public string ValheimStart { get; set; }

        public static ProgramOptions ParseArguments(string[] args)
        {
            ProgramOptions options = null;
            var parser = new Parser(with => with.HelpWriter = Console.Out);
            var result = parser.ParseArguments<ProgramOptions>(args)
                               .WithParsed(parsedOptions => options = parsedOptions)
                               .WithNotParsed(errors => options = null);
            return options;
        }
    }
}