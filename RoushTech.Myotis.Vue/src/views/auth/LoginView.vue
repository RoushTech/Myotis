<template>
    <AuthCard title="Sign in">
        <v-form @submit.prevent="login">
            <v-text-field
                v-model="email"
                label="Email"
                type="email"
                autocomplete="username"
                autofocus
            />
            <v-text-field
                v-model="password"
                label="Password"
                type="password"
                autocomplete="current-password"
                :error-messages="error"
            />
            <v-checkbox
                v-model="rememberMe"
                label="Keep me signed in"
                density="compact"
                hide-details
            />
            <v-btn
                type="submit"
                color="primary"
                block
                class="mt-3"
                :loading="busy"
                :disabled="!email || !password"
            >
                Sign in
            </v-btn>
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

const auth = useAuthStore();
const router = useRouter();
const email = ref("");
const password = ref("");
const rememberMe = ref(true);
const busy = ref(false);
const error = ref<string | null>(null);

async function login() {
    busy.value = true;
    error.value = null;
    try {
        auth.signedIn(await authApi.login(email.value, password.value, rememberMe.value));
        await router.push(auth.route ?? "/");
    } catch (e) {
        error.value = errorMessage(e);
    } finally {
        busy.value = false;
    }
}
</script>
