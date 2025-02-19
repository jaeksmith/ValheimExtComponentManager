using System;
using System.Collections.Generic;
using CommandLine;

namespace ValheimExtComponentManager
{
    public class ProgramOptions
    {
        public static readonly HashSet<string> YesNoOptions = new HashSet<string> { "yes", "no" };
        public static readonly HashSet<string> VoiceCommanderManageOptions = new HashSet<string> { "run-while-valheim-runs", "no", "restart", "start-if-not-running", "stop" };

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

#if DEBUG
        // --ext-component-manager::use-install-root { directory path }
        [Option("ext-component-manager::use-install-root", Required = false, HelpText = "Specifies the root directory for installation (debug builds only).")]
        public string ExtComponentManagerUseInstallRoot { get; set; }

        // --ext-component-manager::use-component-spec-url { url }
        [Option("ext-component-manager::use-component-spec-url", Required = false, HelpText = "Specifies the URL for the component spec (debug builds only).")]
        public string ExtComponentManagerUseComponentSpecUrl { get; set; }
#endif

        public static ProgramOptions ParseArguments(string[] args)
        {
            ProgramOptions options = null;
            var parser = new Parser(with => with.HelpWriter = Console.Out);
            var result = parser.ParseArguments<ProgramOptions>(args)
                               .WithParsed(parsedOptions => options = parsedOptions)
                               .WithNotParsed(errors => options = null);

            if (options != null)
            {
                ValidateOptions(options);
            }

            return options;
        }

        private static void ValidateOptions(ProgramOptions options)
        {
            ValidateOption(options.ExtComponentManagerCheckUpdate, YesNoOptions, "ext-component-manager::check-update");
            ValidateOption(options.ValheimVrCheckUpdate, YesNoOptions, "valheim-vr::check-update");
            ValidateOption(options.ValheimVrEnabled, YesNoOptions, "valheim-vr::enable");
            ValidateOption(options.VoiceCommanderCheckUpdate, YesNoOptions, "voice-commander::check-update");
            ValidateOption(options.VoiceCommanderManage, VoiceCommanderManageOptions, "voice-commander::manage");
            ValidateOption(options.ValheimStart, YesNoOptions, "valheim::start");

#if DEBUG
            if (!string.IsNullOrEmpty(options.ExtComponentManagerUseInstallRoot) && !Directory.Exists(options.ExtComponentManagerUseInstallRoot))
            {
                throw new ArgumentException($"Invalid directory path '{options.ExtComponentManagerUseInstallRoot}' for option 'ext-component-manager::use-install-root'.");
            }
#endif
        }

        private static void ValidateOption(string optionValue, HashSet<string> allowedValues, string optionName)
        {
            if (!string.IsNullOrEmpty(optionValue) && !allowedValues.Contains(optionValue))
            {
                throw new ArgumentException($"Invalid value '{optionValue}' for option '{optionName}'. Allowed values are: {string.Join(", ", allowedValues)}");
            }
        }
    }
}