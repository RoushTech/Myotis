<template>
    <v-text-field
        v-model="password"
        :label="label"
        type="password"
        autocomplete="new-password"
        :rules="[long]"
    />
    <v-text-field
        v-model="confirm"
        label="Confirm password"
        type="password"
        autocomplete="new-password"
        :rules="[matches]"
    />
</template>

<script setup lang="ts">
import { ref } from "vue";

withDefaults(defineProps<{ label?: string }>(), { label: "Password" });
const password = defineModel<string>({ required: true });
const confirm = ref("");

function long(value: string) {
    return (value.length >= 6 && /\d/.test(value)) || "At least 6 characters, including a digit";
}

function matches(value: string) {
    return value === password.value || "Passwords don't match";
}
</script>
