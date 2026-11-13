<template>
    <v-container class="fill-height d-flex align-center justify-center">
        <v-card width="420" class="pa-2" border flat>
            <v-card-item>
                <AppBrand class="auth-card__logo" />
            </v-card-item>
            <v-card-title class="auth-card__title">{{ title }}</v-card-title>
            <v-card-subtitle v-if="subtitle" class="auth-card__subtitle">{{
                subtitle
            }}</v-card-subtitle>
            <v-card-text>
                <slot />
            </v-card-text>
        </v-card>
    </v-container>
</template>

<script setup lang="ts">
import AppBrand from "@c/AppBrand.vue";

defineProps<{ title: string; subtitle?: string | undefined }>();
</script>
