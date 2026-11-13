import js from "@eslint/js";
import pluginVue from "eslint-plugin-vue";
import { defineConfigWithVueTs, vueTsConfigs } from "@vue/eslint-config-typescript";
import skipFormatting from "@vue/eslint-config-prettier/skip-formatting";
import globals from "globals";

export default defineConfigWithVueTs(
    {
        ignores: ["build/**"],
    },
    js.configs.recommended,
    pluginVue.configs["flat/recommended"],
    vueTsConfigs.recommendedTypeChecked,
    skipFormatting,
    {
        languageOptions: {
            globals: {
                ...globals.node,
            },
        },
        rules: {
            "no-console": process.env.NODE_ENV === "production" ? "warn" : "off",
            "no-debugger": process.env.NODE_ENV === "production" ? "warn" : "off",
            "@typescript-eslint/no-unused-vars": [
                "warn",
                { argsIgnorePattern: "^_", varsIgnorePattern: "^_" },
            ],

            "vue/block-lang": ["error", { script: { lang: "ts" }, style: { lang: "scss" } }],
            "vue/custom-event-name-casing": ["error", "kebab-case", { ignores: ["/^update:/"] }],
            "vue/no-child-content": "error",
            "vue/no-duplicate-attr-inheritance": "error",
            "vue/component-api-style": "error",
            "vue/block-tag-newline": ["error", { singleline: "always", multiline: "always" }],
            "vue/no-useless-v-bind": 1,
            "vue/define-emits-declaration": 1,
            "vue/define-props-declaration": 1,
            "vue/no-template-shadow": ["error", { allow: ["props"] }],

            "@typescript-eslint/restrict-template-expressions": "off",

            "arrow-body-style": ["warn", "as-needed"],
            "func-style": ["error", "declaration"],
            "vue/block-order": ["warn", { order: ["template", "script", "style"] }],
        },
    },
    {
        files: ["src/**/*.ts", "src/**/*.vue"],
        rules: {
            "@typescript-eslint/no-unnecessary-condition": "error",
            "@typescript-eslint/no-redundant-type-constituents": "warn",
            "@typescript-eslint/switch-exhaustiveness-check": [
                "warn",
                { considerDefaultExhaustiveForUnions: true },
            ],
            "@typescript-eslint/no-confusing-void-expression": [
                "warn",
                { ignoreArrowShorthand: true },
            ],
        },
    },
    {
        files: ["**/*.{js,mjs,cjs,ts,vue}"],
        ignores: ["src/api/**"],
        rules: {
            "no-restricted-imports": [
                "error",
                {
                    name: "axios",
                    importNames: ["default", "axios"],
                    message: "Do not import axios outside of API folder.",
                },
            ],
        },
    }
);
