<template>
    <v-container class="page">
        <div class="page-head">
            <div>
                <h1 class="page-title">Limits</h1>
                <p class="page-subtitle">
                    Changes apply to new connections straight away. No restart needed.
                </p>
            </div>
        </div>
        <LimitsForm />
    </v-container>
</template>

<script setup lang="ts">
import LimitsForm from "@c/admin/LimitsForm.vue";
</script>
