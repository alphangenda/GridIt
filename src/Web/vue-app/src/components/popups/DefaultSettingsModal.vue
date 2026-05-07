<template>
  <teleport to="body">
    <div
      v-if="modelValue"
      class="dsm-overlay"
      tabindex="-1"
      @keydown.esc="close"
    >
      <div class="dsm-backdrop" @click="close"></div>

      <div class="dsm-modal">
        <div class="dsm-header">
          <div>
            <h2 class="dsm-title">barème de correction</h2>
            <p class="dsm-subtitle">
              {{ t("pages.defaultSettingsModal.subtitle") }}
            </p>
          </div>

          <button class="dsm-close" type="button" @click="close">✕</button>
        </div>

        <div class="dsm-body" v-if="!isLoading">
          <section class="dsm-section">
            <div class="dsm-section-head">
              <h3>{{ t("pages.defaultSettingsModal.defaultLetters") }}</h3>
              <p>
                {{ t("pages.defaultSettingsModal.defaultLettersHelp") }}
              </p>
            </div>

            <div class="dsm-table-wrap">
              <table class="dsm-table">
                <thead>
                  <tr>
                    <th>{{ t("pages.defaultSettingsModal.table.letter") }}</th>
                    <th>{{ t("pages.defaultSettingsModal.table.description") }}</th>
                    <th>{{ t("pages.defaultSettingsModal.table.defaultPercent") }}</th>
                    <th>{{ t("pages.defaultSettingsModal.table.isEnabled") }}</th>
                    <th></th>
                  </tr>
                </thead>

                <tbody>
                  <tr v-for="(letter, index) in letters" :key="letter.letter">
                    <td class="dsm-letter-cell">
                      <span class="dsm-letter-badge">{{ letter.letter }}</span>
                    </td>

                    <td>
                      <input
                        v-model="letter.description"
                        type="text"
                        class="dsm-input"
                        @input="scheduleAutoSave"
                      />
                    </td>

                    <td>
                      <input
                        v-model.number="letter.defaultPercent"
                        type="number"
                        min="0"
                        max="100"
                        class="dsm-input dsm-input--small"
                        @input="normalizePercent(letter)"
                      />
                    </td>

                    <td class="dsm-checkbox-cell">
                      <input
                        v-model="letter.isEnabled"
                        type="checkbox"
                        @change="scheduleAutoSave"
                      />
                    </td>

                    <td class="dsm-remove-cell">
                      <button
                        v-if="!isCoreLetter(letter.letter)"
                        class="dsm-close"
                        type="button"
                        @click="removeLetter(index)"
                      >
                        ✕
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>

            <div class="dsm-add-letter">
              <button class="dsm-btn dsm-btn--primary" type="button" @click="addLetter">
                + Ajouter une lettre
              </button>
            </div>
          </section>
        </div>

        <div v-else class="dsm-loading">
          {{ t("pages.defaultSettingsModal.loading") }}
        </div>

        <div class="dsm-footer">
          <div class="dsm-status">
            <span v-if="saveState === 'saving'">{{ t("pages.defaultSettingsModal.status.saving") }}</span>
            <span v-else-if="saveState === 'saved'">{{ t("pages.defaultSettingsModal.status.saved") }}</span>
            <span v-else-if="saveState === 'error'">{{ t("pages.defaultSettingsModal.status.error") }}</span>
          </div>

          <div class="dsm-actions">
            <button class="dsm-btn dsm-btn--ghost" type="button" @click="close">
              {{ t("pages.defaultSettingsModal.actions.close") }}
            </button>

            <button
              class="dsm-btn dsm-btn--primary"
              type="button"
              :disabled="isSaving"
              @click="saveAll"
            >
              {{ isSaving ? t("pages.defaultSettingsModal.actions.saving") : t("pages.defaultSettingsModal.actions.saveNow") }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </teleport>
</template>

<script setup lang="ts">
import { nextTick, onBeforeUnmount, ref, watch } from "vue";
import { useI18n } from "vue3-i18n";

const { t } = useI18n();

type LetterRow = {
  letter: string;
  description: string;
  defaultPercent: number;
  isEnabled: boolean;
};

type SaveState = "idle" | "saving" | "saved" | "error";

const props = defineProps<{
  modelValue: boolean;
}>();

const emit = defineEmits<{
  (e: "update:modelValue", value: boolean): void;
  (e: "saved"): void;
}>();

const letters = ref<LetterRow[]>([]);
const isLoading = ref(false);
const isSaving = ref(false);
const saveState = ref<SaveState>("idle");

let autoSaveTimer: ReturnType<typeof setTimeout> | null = null;

function close() {
  if (autoSaveTimer) {
    clearTimeout(autoSaveTimer);
    autoSaveTimer = null;
  }

  emit("update:modelValue", false);
}

function normalizePercent(letter: LetterRow) {
  if (Number.isNaN(letter.defaultPercent)) {
    letter.defaultPercent = 0;
  }

  if (letter.defaultPercent < 0) {
    letter.defaultPercent = 0;
  }

  if (letter.defaultPercent > 100) {
    letter.defaultPercent = 100;
  }

  scheduleAutoSave();
}

function scheduleAutoSave() {
  saveState.value = "idle";

  if (autoSaveTimer) {
    clearTimeout(autoSaveTimer);
  }

  autoSaveTimer = setTimeout(() => {
    saveAll();
  }, 700);
}

function addLetter() {
  if (letters.value.length === 0) {
    letters.value.push({
      letter: "A",
      description: "",
      defaultPercent: 0,
      isEnabled: true,
    });
    scheduleAutoSave();
    return;
  }

  const last = letters.value[letters.value.length - 1].letter;
  const nextCharCode = last.charCodeAt(0) + 1;
  if (nextCharCode > 90) return;
  const nextLetter = String.fromCharCode(nextCharCode);

  letters.value.push({
    letter: nextLetter,
    description: "",
    defaultPercent: 0,
    isEnabled: true,
  });

  scheduleAutoSave();
}

function removeLetter(index: number) {
  const row = letters.value[index];
  if (!row || isCoreLetter(row.letter)) return;
  letters.value.splice(index, 1);
  scheduleAutoSave();
}

function isCoreLetter(letter: string) {
  return ["A", "B", "C", "D", "E", "F"].includes(letter);
}

async function loadLetters() {
  const res = await fetch("/api/default-criterion-letters");

  if (!res.ok) {
    throw new Error("Impossible de charger les lettres par défaut");
  }

  const data = await res.json();

  letters.value = (data as any[]).map((row) => ({
    letter: String(row.letter),
    description: String(row.description ?? ""),
    defaultPercent: Number(row.defaultPercent ?? 0),
    isEnabled: !!row.isEnabled,
  }));
}

async function loadAll() {
  isLoading.value = true;
  saveState.value = "idle";

  try {
    await loadLetters();
  } finally {
    isLoading.value = false;
  }
}



async function saveLetters() {
  const payload = {
    letters: letters.value.map((letter) => ({
      letter: letter.letter,
      description: letter.description,
      defaultPercent: letter.defaultPercent,
      isEnabled: letter.isEnabled,
    })),
  };

  const res = await fetch("/api/default-criterion-letters", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    throw new Error("Erreur lors de l'enregistrement des lettres");
  }
}

async function saveAll() {
  if (isSaving.value) return;

  isSaving.value = true;
  saveState.value = "saving";

  try {
    await saveLetters();
    saveState.value = "saved";
    emit("saved");
  } catch (error) {
    console.error(error);
    saveState.value = "error";
  } finally {
    isSaving.value = false;
  }
}

watch(
  () => props.modelValue,
  async (open) => {
    if (open) {
      await nextTick();
      document.body.style.overflow = "hidden";
      await loadAll();
    } else {
      document.body.style.overflow = "";

      if (autoSaveTimer) {
        clearTimeout(autoSaveTimer);
        autoSaveTimer = null;
      }
    }
  }
);

onBeforeUnmount(() => {
  document.body.style.overflow = "";

  if (autoSaveTimer) {
    clearTimeout(autoSaveTimer);
    autoSaveTimer = null;
  }
});
</script>

<style scoped>
.dsm-overlay {
  position: fixed;
  inset: 0;
  z-index: 9999;
  display: grid;
  place-items: center;
}

.dsm-backdrop {
  position: absolute;
  inset: 0;
  background: rgba(15, 23, 42, 0.65);
  backdrop-filter: blur(3px);
}

.dsm-modal {
  position: relative;
  z-index: 1;
  width: min(1100px, 95vw);
  max-height: 90vh;
  background: #ffffff;
  border-radius: 18px;
  box-shadow: 0 24px 70px rgba(22, 22, 42, 0.18);
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.dsm-header {
  padding: 20px 24px;
  border-bottom: 1px solid rgba(184, 160, 136, 0.2);
  display: flex;
  justify-content: space-between;
  gap: 20px;
  align-items: flex-start;
}

.dsm-title {
  margin: 0;
  font-size: 1.35rem;
  font-weight: 800;
  color: #1a1a2e;
}

.dsm-subtitle {
  margin: 6px 0 0;
  color: #6b7280;
  line-height: 1.45;
}

.dsm-close {
  border: none;
  background: #f5f0ea;
  border-radius: 10px;
  width: 38px;
  height: 38px;
  cursor: pointer;
  font-size: 1rem;
  color: #1a1a2e;
}

.dsm-body {
  padding: 24px;
  overflow: auto;
  display: grid;
  gap: 24px;
}

.dsm-section {
  border: 1px solid rgba(184, 160, 136, 0.25);
  border-radius: 16px;
  padding: 18px;
  background: #f9fafb;
}

.dsm-section-head h3 {
  margin: 0;
  font-size: 1.05rem;
  color: #1a1a2e;
}

.dsm-section-head p {
  margin: 6px 0 0;
  color: #6b7280;
}

.dsm-skills-grid {
  margin-top: 16px;
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 12px;
}

.dsm-skill-card {
  display: flex;
  align-items: center;
  gap: 10px;
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  padding: 12px 14px;
  cursor: pointer;
}

.dsm-table-wrap {
  margin-top: 16px;
  overflow-x: auto;
}

.dsm-add-letter {
  margin-top: 12px;
}

.dsm-table {
  width: 100%;
  border-collapse: collapse;
  background: white;
  border-radius: 14px;
  overflow: hidden;
}

.dsm-table th,
.dsm-table td {
  padding: 12px 14px;
  border-bottom: 1px solid rgba(184, 160, 136, 0.2);
  text-align: left;
  vertical-align: middle;
}

.dsm-table th {
  background: #f5f0ea;
  color: #1a1a2e;
  font-size: 0.92rem;
}

.dsm-letter-cell {
  width: 90px;
}

.dsm-letter-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 36px;
  height: 36px;
  padding: 0 10px;
  border-radius: 10px;
  background: #1a1a2e;
  color: white;
  font-weight: 700;
}

.dsm-input {
  width: 100%;
  border: 1px solid rgba(184, 160, 136, 0.4);
  border-radius: 10px;
  padding: 10px 12px;
  outline: none;
  background: white;
  color: #1a1a2e;
}

.dsm-input--small {
  max-width: 110px;
}

.dsm-checkbox-cell {
  text-align: center;
}

.dsm-remove-cell {
  text-align: right;
}

.dsm-footer {
  padding: 18px 24px;
  border-top: 1px solid rgba(184, 160, 136, 0.2);
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: center;
  background: white;
}

.dsm-status {
  color: #6b7280;
  font-size: 0.92rem;
}

.dsm-actions {
  display: flex;
  gap: 10px;
}

.dsm-btn {
  border: none;
  border-radius: 10px;
  padding: 10px 14px;
  cursor: pointer;
  font-weight: 600;
}

.dsm-btn--ghost {
  background: #f5f0ea;
  color: #1a1a2e;
}

.dsm-btn--primary {
  background: #b8a088;
  color: #1a1a2e;
}

.dsm-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.dsm-loading {
  padding: 40px 24px;
  color: #6b7280;
}
</style>

<style>
[data-theme="dark"] .dsm-modal {
  background: #2d2d44;
  color: rgba(255, 255, 255, 0.9);
}

[data-theme="dark"] .dsm-header {
  border-color: rgba(255, 255, 255, 0.08);
}

[data-theme="dark"] .dsm-title {
  color: rgba(255, 255, 255, 0.9);
}

[data-theme="dark"] .dsm-subtitle {
  color: rgba(255, 255, 255, 0.45);
}

[data-theme="dark"] .dsm-close {
  background: rgba(255, 255, 255, 0.08);
  color: rgba(255, 255, 255, 0.7);
}

[data-theme="dark"] .dsm-body {
  background: #2d2d44;
}

[data-theme="dark"] .dsm-section {
  background: #1f1f35;
  border-color: rgba(255, 255, 255, 0.06);
}

[data-theme="dark"] .dsm-section-head h3 {
  color: rgba(255, 255, 255, 0.9);
}

[data-theme="dark"] .dsm-section-head p {
  color: rgba(255, 255, 255, 0.45);
}

[data-theme="dark"] .dsm-table {
  background: #2d2d44;
}

[data-theme="dark"] .dsm-table th {
  background: #16162a;
  color: rgba(255, 255, 255, 0.7);
}

[data-theme="dark"] .dsm-table th,
[data-theme="dark"] .dsm-table td {
  border-color: rgba(255, 255, 255, 0.06);
  color: rgba(255, 255, 255, 0.85);
}

[data-theme="dark"] .dsm-letter-badge {
  background: #b8a088;
  color: #1a1a2e;
}

[data-theme="dark"] .dsm-input {
  background: #16162a !important;
  border-color: rgba(255, 255, 255, 0.1) !important;
  color: rgba(255, 255, 255, 0.9) !important;
  color-scheme: dark;
}

[data-theme="dark"] .dsm-status {
  color: rgba(255, 255, 255, 0.45);
}

[data-theme="dark"] .dsm-footer {
  background: #2d2d44;
  border-color: rgba(255, 255, 255, 0.08);
}

[data-theme="dark"] .dsm-btn--ghost {
  background: rgba(255, 255, 255, 0.08);
  color: rgba(255, 255, 255, 0.85);
}

[data-theme="dark"] .dsm-btn--primary {
  background: #b8a088;
  color: #1a1a2e;
}

[data-theme="dark"] .dsm-loading {
  color: rgba(255, 255, 255, 0.45);
}
</style>
