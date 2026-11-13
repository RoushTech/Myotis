<template>
    <ErrorAlert v-if="error" what="limits" :message="errorMessage(error)" @retry="refetch()" />
    <v-skeleton-loader v-else-if="!settings" type="card@2" />
    <template v-else>
        <div class="limits">
            <v-card v-for="group in groups" :key="group.name" border flat>
                <v-card-item>
                    <v-card-title>{{ group.name }}</v-card-title>
                    <v-card-subtitle class="card-subtitle">{{ group.description }}</v-card-subtitle>
                </v-card-item>
                <v-card-text class="limits__rows">
                    <div v-for="setting in group.settings" :key="setting.key" class="limits__row">
                        <label :for="setting.key" class="limits__label">{{ setting.name }}</label>
                        <div class="limits__input">
                            <v-text-field
                                :id="setting.key"
                                v-model="draft[setting.key]"
                                inputmode="numeric"
                                density="compact"
                                hide-details
                                :error="!!problem(setting)"
                                class="limits__field"
                            />
                            <span class="text-medium-emphasis">{{ setting.unit }}</span>
                        </div>
                        <div v-if="problem(setting)" class="text-error text-caption">
                            {{ problem(setting) }}
                        </div>
                        <div v-else class="limits__meta">
                            Default {{ setting.default.toLocaleString() }}
                            <template v-if="draft[setting.key] !== String(setting.default)">
                                ·
                                <v-btn
                                    variant="text"
                                    size="x-small"
                                    color="primary"
                                    @click="draft[setting.key] = String(setting.default)"
                                >
                                    Reset to default
                                </v-btn>
                            </template>
                        </div>
                    </div>
                </v-card-text>
            </v-card>
        </div>
        <div v-if="dirty" class="save-bar">
            <span v-if="saveError" class="text-error">{{ saveError }}</span>
            <span v-else class="text-medium-emphasis">Unsaved changes</span>
            <v-btn variant="outlined" @click="reset">Discard</v-btn>
            <v-btn color="primary" variant="flat" :loading="busy" :disabled="invalid" @click="save">
                Save
            </v-btn>
        </div>
        <v-snackbar v-model="saved" timeout="3000">Limits saved</v-snackbar>
    </template>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from "vue";
import { useQuery } from "@pinia/colada";
import { settingsApi, type Setting } from "@/api/settings";
import { errorMessage } from "@/api/errors";
import { settingsQuery, useInvalidateSettings } from "@/queries/settings";
import ErrorAlert from "@c/ErrorAlert.vue";

const DESCRIPTIONS: Record<string, string> = {
    Messages: "Larger messages are refused while they're being sent.",
    Retention: "Applies to every mailbox. The oldest messages go first.",
    "Sending rate": "Senders over the limit are told to retry later, so nothing is lost.",
    "Failed sign-ins": "Slows down anyone guessing SMTP passwords.",
};

const invalidate = useInvalidateSettings();
const { data: settings, error, refetch } = useQuery(settingsQuery);
const draft = reactive<Record<string, string>>({});
const busy = ref(false);
const saved = ref(false);
const saveError = ref<string | null>(null);

watch(settings, reset, { immediate: true });

const groups = computed(() => {
    const byGroup = new Map<string, Setting[]>();
    for (const setting of settings.value ?? []) {
        byGroup.set(setting.group, [...(byGroup.get(setting.group) ?? []), setting]);
    }
    return [...byGroup].map(([name, list]) => ({
        name,
        description: DESCRIPTIONS[name] ?? "",
        settings: list,
    }));
});

const dirty = computed(() => (settings.value ?? []).some((s) => draft[s.key] !== String(s.value)));
const invalid = computed(() => (settings.value ?? []).some((s) => problem(s) !== null));

function reset() {
    for (const setting of settings.value ?? []) {
        draft[setting.key] = String(setting.value);
    }
    saveError.value = null;
}

function problem(setting: Setting): string | null {
    const text = (draft[setting.key] ?? "").trim();
    const value = Number(text);
    if (!/^\d+$/.test(text) || value < setting.min || value > setting.max) {
        return `Enter a whole number from ${setting.min.toLocaleString()} to ${setting.max.toLocaleString()}.`;
    }
    return null;
}

async function save() {
    busy.value = true;
    saveError.value = null;
    try {
        await settingsApi.update(
            (settings.value ?? [])
                .filter((s) => draft[s.key] !== String(s.value))
                .map((s) =>
                    draft[s.key] === String(s.default)
                        ? { key: s.key, clear: true }
                        : { key: s.key, value: draft[s.key] ?? "" }
                )
        );
        await invalidate();
        saved.value = true;
    } catch (e) {
        saveError.value = errorMessage(e);
    } finally {
        busy.value = false;
    }
}
</script>
