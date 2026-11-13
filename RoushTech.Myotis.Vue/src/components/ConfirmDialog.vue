<template>
    <v-dialog :model-value="open" max-width="460" @update:model-value="emit('cancel')">
        <v-card :title="title">
            <v-card-text>
                <p>{{ body }}</p>
                <v-alert v-if="error" type="error" variant="tonal" class="mt-3">{{
                    error
                }}</v-alert>
            </v-card-text>
            <v-card-actions>
                <v-spacer />
                <v-btn variant="text" @click="emit('cancel')">Cancel</v-btn>
                <v-btn color="error" variant="flat" :loading="busy" @click="emit('confirm')">
                    {{ action }}
                </v-btn>
            </v-card-actions>
        </v-card>
    </v-dialog>
</template>

<script setup lang="ts">
defineProps<{
    open: boolean;
    title: string;
    body: string;
    action: string;
    busy?: boolean;
    error?: string | null;
}>();
const emit = defineEmits<{ confirm: []; cancel: [] }>();
</script>
