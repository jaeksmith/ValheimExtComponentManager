using System;

namespace ValheimExtComponentManager
{
    /// <summary>
    /// Represents the installation state of a component.
    /// </summary>
    public enum ComponentState
    {
        /// <summary>
        /// Maintain the current state (no change).
        /// </summary>
        Maintain = 0,
        
        /// <summary>
        /// Component is installed.
        /// </summary>
        Installed = 1,
        
        /// <summary>
        /// Component is uninstalled.
        /// </summary>
        Uninstalled = 2
    }
}
