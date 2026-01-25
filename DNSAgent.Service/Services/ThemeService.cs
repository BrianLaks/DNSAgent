using System;

namespace DNSAgent.Service.Services
{
    public enum AppTheme
    {
        Classic,
        Aegis,
        Sentry,
        GhostGuard
    }

    public class ThemeService
    {
        public AppTheme CurrentTheme { get; private set; } = AppTheme.Classic;
        public string ThemeName => CurrentTheme.ToString();
        
        public event Action OnThemeChanged;

        public void SetTheme(AppTheme theme)
        {
            if (CurrentTheme != theme)
            {
                CurrentTheme = theme;
                NotifyThemeChanged();
            }
        }

        private void NotifyThemeChanged() => OnThemeChanged?.Invoke();

        public string GetBrandName() => CurrentTheme switch
        {
            AppTheme.Aegis => "Aegis DNS",
            AppTheme.Sentry => "SentryDNS",
            AppTheme.GhostGuard => "GhostGuard",
            _ => "DNS Agent"
        };

        public string GetLogoPath() => CurrentTheme switch
        {
            AppTheme.Aegis => "logo_aegis.png",
            AppTheme.Sentry => "logo_sentry.png",
            AppTheme.GhostGuard => "logo_ghost.png",
            _ => "logo.png"
        };
        
        public string GetThemeClass() => CurrentTheme switch
        {
            AppTheme.Aegis => "theme-aegis",
            AppTheme.Sentry => "theme-sentry",
            AppTheme.GhostGuard => "theme-ghost",
            _ => "theme-classic"
        };

        public string GetInlineStyles() => CurrentTheme switch
        {
            AppTheme.Aegis => "--brand-primary: #d4af37; --brand-secondary: #0a192f; --brand-accent: #f9e29f;",
            AppTheme.Sentry => "--brand-primary: #e0115f; --brand-secondary: #1a1a1a; --brand-accent: #ff4d4d;",
            AppTheme.GhostGuard => "--brand-primary: #9d00ff; --brand-secondary: #000000; --brand-accent: #00f2ff;",
            _ => "--brand-primary: #1e90ff; --brand-secondary: #000080; --brand-accent: #00bfff;"
        };
    }
}
