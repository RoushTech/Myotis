<template>
    <v-alert type="error" variant="tonal" border="start">
        <div class="error-alert">
            <div>
                <strong>Couldn't load {{ what }}</strong>
                <div>{{ message }}</div>
            </div>
            <v-btn variant="outlined" size="small" @click="emit('retry')">Try again</v-btn>
        </div>
    </v-alert>
</template>

<script setup lang="ts">
defineProps<{ what: string; message: string }>();
const emit = defineEmits<{ retry: [] }>();
</script>
