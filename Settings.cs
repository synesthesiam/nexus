namespace Nexus
{
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase
    {
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default
        {
            get { return (defaultInstance); }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("/dev/ttyUSB0")]
        public string HITTPortName
        {
            get { return ((string)(this["HITTPortName"])); }
            set { this["HITTPortName"] = value; }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("false")]
        public bool HITTAutoConnect
        {
            get { return ((bool)(this["HITTAutoConnect"])); }
            set { this["HITTAutoConnect"] = value; }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("false")]
        public bool GraphicsFullscreen
        {
            get { return ((bool)(this["GraphicsFullscreen"])); }
            set { this["GraphicsFullscreen"] = value; }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1024")]
        public int GraphicsScreenWidth
        {
            get { return ((int)(this["GraphicsScreenWidth"])); }
            set { this["GraphicsScreenWidth"] = value; }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("768")]
        public int GraphicsScreenHeight
        {
            get { return ((int)(this["GraphicsScreenHeight"])); }
            set { this["GraphicsScreenHeight"] = value; }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("16")]
        public int MaxPlayers
        {
            get { return ((int)(this["MaxPlayers"])); }
            set { this["MaxPlayers"] = value; }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string PlayersFileDirectory
        {
            get { return ((string)(this["PlayersFileDirectory"])); }
            set { this["PlayersFileDirectory"] = value; }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string GamesFileDirectory
        {
            get { return ((string)(this["GamesFileDirectory"])); }
            set { this["GamesFileDirectory"] = value; }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string DataFileDirectory
        {
            get { return ((string)(this["DataFileDirectory"])); }
            set { this["DataFileDirectory"] = value; }
        }
    }
}
