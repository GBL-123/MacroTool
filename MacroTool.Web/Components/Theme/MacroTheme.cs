using MudBlazor;

namespace MacroTool.Web.Components.Theme;

public static class MacroTheme
{
    private static readonly string[] UiFonts =
    [
        "Segoe UI Variable Text",
        "Segoe UI",
        "system-ui",
        "-apple-system",
        "Microsoft YaHei UI",
        "sans-serif"
    ];

    private static readonly string[] DisplayFonts =
    [
        "Sitka Text",
        "Sitka",
        "Segoe UI",
        "Microsoft YaHei UI",
        "serif"
    ];

    private static readonly string[] MonoFonts =
    [
        "Cascadia Mono",
        "Cascadia Code",
        "Consolas",
        "Courier New",
        "monospace"
    ];

    public static readonly MudTheme Instance = new()
    {
        PaletteLight = new PaletteLight
        {
            Black = "#111111",
            Dark = "#2F3437",
            Background = "#F7F6F3",
            BackgroundGray = "#F2F1ED",
            Surface = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#2F3437",
            DrawerIcon = "#787774",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#2F3437",
            Primary = "#111111",
            PrimaryContrastText = "#FFFFFF",
            Secondary = "#787774",
            SecondaryContrastText = "#FFFFFF",
            Info = "#1F6C9F",
            InfoContrastText = "#FFFFFF",
            Success = "#346538",
            SuccessContrastText = "#FFFFFF",
            Warning = "#956400",
            WarningContrastText = "#FFFFFF",
            Error = "#9F2F2D",
            ErrorContrastText = "#FFFFFF",
            TextPrimary = "#2F3437",
            TextSecondary = "#787774",
            TextDisabled = "#B8B3AA",
            ActionDefault = "#787774",
            ActionDisabled = "#C6C1B8",
            ActionDisabledBackground = "rgba(0,0,0,0.04)",
            Divider = "rgba(0,0,0,0.08)",
            DividerLight = "rgba(0,0,0,0.05)",
            LinesDefault = "rgba(0,0,0,0.12)",
            LinesInputs = "rgba(0,0,0,0.28)",
            TableLines = "rgba(0,0,0,0.07)",
            TableStriped = "rgba(0,0,0,0.015)",
            TableHover = "rgba(0,0,0,0.035)",
            GrayDefault = "#787774",
            GrayLight = "#A6A29B",
            GrayLighter = "#DCD8D0",
            GrayDark = "#4A463F",
            GrayDarker = "#2F3437",
            OverlayDark = "rgba(30,27,22,0.45)",
            Skeleton = "#E9E7E1",
            HoverOpacity = 0.05,
            BorderOpacity = 0.12
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "10px"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = UiFonts,
                FontSize = "0.9rem",
                FontWeight = "400",
                LineHeight = "1.55"
            },
            Button = new ButtonTypography
            {
                FontFamily = UiFonts,
                FontSize = "0.875rem",
                FontWeight = "600",
                LetterSpacing = "0.01em",
                TextTransform = "none"
            },
            Caption = new CaptionTypography
            {
                FontFamily = MonoFonts,
                FontSize = "0.6875rem",
                LetterSpacing = "0.08em"
            },
            H5 = new H5Typography
            {
                FontFamily = DisplayFonts,
                FontWeight = "600",
                LetterSpacing = "-0.02em"
            },
            H6 = new H6Typography
            {
                FontFamily = DisplayFonts,
                FontWeight = "600",
                LetterSpacing = "-0.01em"
            }
        }
    };
}
