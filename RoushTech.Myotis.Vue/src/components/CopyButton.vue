<template>
    <v-btn
        v-if="caption"
        :prepend-icon="icon"
        :color="copied ? 'success' : undefined"
        variant="text"
        :aria-label="`Copy ${label}`"
        @click="copy"
    >
        {{ copied ? "Copied" : caption }}
    </v-btn>
    <v-btn
        v-else
        :icon="icon"
        :color="copied ? 'success' : undefined"
        variant="text"
        size="x-small"
        :title="copied ? 'Copied' : `Copy ${label}`"
        :aria-label="`Copy ${label}`"
        @click="copy"
    />
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, ref } from "vue";

const props = defineProps<{ text: string; label: string; caption?: string }>();
const copied = ref(false);
const icon = computed(() => (copied.value ? "mdi-check" : "mdi-content-copy"));
let reset: ReturnType<typeof setTimeout> | undefined;

async function copy() {
    await navigator.clipboard.writeText(props.text);
    copied.value = true;
    clearTimeout(reset);
    reset = setTimeout(() => (copied.value = false), 1500);
}

onBeforeUnmount(() => clearTimeout(reset));
</script>
