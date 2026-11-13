import "@mdi/font/css/materialdesignicons.css";
import "@/styles/vuetify-settings.scss";
import "@/styles/main.scss";
import { createVuetify, type ThemeDefinition } from "vuetify";
import { aliases, mdi } from "vuetify/iconsets/mdi";
import * as components from "vuetify/components";
import * as directives from "vuetify/directives";

// The roushtech.net palette Tamias uses, so the products look alike.
const dark: ThemeDefinition = {
    dark: true,
    colors: {
        background: "#0a0e14",
        "on-background": "#e6edf3",
        surface: "#0f1620",
        "surface-bright": "#141d29",
        "surface-light": "#141d29",
        "surface-variant": "#2a3647",
        "on-surface": "#e6edf3",
        "on-surface-variant": "#e6edf3",
        primary: "#4a9eff",
        "on-primary": "#ffffff",
        secondary: "#8b5cf6",
        "on-secondary": "#ffffff",
        error: "#f85149",
        "on-error": "#0a0e14",
        success: "#3fb950",
        "on-success": "#0a0e14",
        info: "#6fb1ff",
        "on-info": "#0a0e14",
        warning: "#d29922",
        "on-warning": "#0a0e14",
    },
    variables: {
        "border-color": "#2a3647",
        "border-opacity": 1,
        "medium-emphasis-opacity": 0.68,
    },
};

const light: ThemeDefinition = {
    dark: false,
    colors: {
        background: "#f4f6f9",
        "on-background": "#0f1620",
        surface: "#ffffff",
        "surface-bright": "#ffffff",
        "surface-light": "#eef2f7",
        "surface-variant": "#3a4656",
        "on-surface": "#0f1620",
        "on-surface-variant": "#eef2f7",
        primary: "#1d6ad6",
        "on-primary": "#ffffff",
        secondary: "#6d3fe0",
        "on-secondary": "#ffffff",
        error: "#c62828",
        "on-error": "#ffffff",
        success: "#1a7f37",
        "on-success": "#ffffff",
        info: "#1d6ad6",
        "on-info": "#ffffff",
        warning: "#9a6700",
        "on-warning": "#ffffff",
    },
    variables: {
        "border-color": "#d5dde7",
        "border-opacity": 1,
    },
};

export default createVuetify({
    components,
    directives,
    defaults: {
        VTextField: { variant: "outlined", density: "comfortable" },
        VSelect: { variant: "outlined", density: "comfortable" },
        VCheckbox: { color: "primary" },
        VTabs: { color: "primary" },
        VBtnToggle: { color: "primary" },
    },
    theme: {
        defaultTheme: "system",
        themes: { light, dark },
    },
    icons: {
        defaultSet: "mdi",
        aliases,
        sets: { mdi },
    },
});
