<template>
    <AuthCard title="Set up Myotis" subtitle="Create the first administrator account.">
        <v-form ref="form" @submit.prevent="setup">
            <v-text-field
                v-model="email"
                label="Email"
                type="email"
                autocomplete="username"
                autofocus
            />
            <NewPasswordFields v-model="password" />
            <v-alert v-if="error" type="error" variant="tonal" class="mb-3">{{ error }}</v-alert>
            <v-btn type="submit" color="primary" block :loading="busy">Create account</v-btn>
        </v-form>
    </AuthCard>
</template>

<script setup lang="ts">
import { ref } from "vue";
import { useRouter } from "vue-router";
import { authApi } from "@/api/auth";
import { errorMessage } from "@/api/errors";
import { useAuthStore } from "@/store/auth";
import AuthCard from "@c/auth/AuthCard.vue";
import NewPasswordFields from "@c/auth/NewPasswordFields.vue";

const auth = useAuthStore();
const router = useRouter();
const form = ref<{ validate: () => Promise<{ valid: boolean }> } | null>(null);
const email = ref("");
const password = ref("");
const busy = ref(false);
const error = ref<string | null>(null);

async function setup() {
    if (!(await form.value?.validate())?.valid) {
        return;
    }
    busy.value = true;
    error.value = null;
    try {
        auth.signedIn(await authApi.setup(email.value, password.value));
        await router.push("/");
    } catch (e) {
        error.value = errorMessage(e);
    } finally {
        busy.value = false;
    }
}
</script>
