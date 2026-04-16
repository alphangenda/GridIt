<template>
  <Transition name="fade">
    <form class="popup" novalidate @submit.prevent="onSubmit">
      <div class="popup__bg" @click="emitClose"></div>
      <div class="popup__container">
        <div class="popup__header">
          <p class="popup__title h2-like">{{ t("pages.programs.addSkillTitle") }}</p>
        </div>
        <div class="popup__content">
          <div class="popup__block">
            <div class="form__group">
              <label class="form__label" for="skill-label">
                {{ t("pages.programs.skillLabel") }}
              </label>
              <input
                id="skill-label"
                ref="inputRef"
                v-model="label"
                class="form__input"
                :class="{ 'form__input--error': displayedError }"
                type="text"
                required
                :maxlength="MAX_LENGTH"
                :disabled="isSaving"
                @input="errorMessage = ''"
              />
              <div v-if="displayedError" class="form__error">{{ displayedError }}</div>
              <div class="form__hint">{{ label.trim().length }} / {{ MAX_LENGTH }}</div>
            </div>
            <div class="form__submit">
              <button
                class="btn btn--fullscreen"
                type="submit"
                :disabled="!isValid || isSaving"
              >
                {{ isSaving ? t("pages.programs.skillsSaving") : t("global.add") }}
              </button>
              <button
                class="btn btn--fullscreen btn--red"
                type="button"
                :disabled="isSaving"
                @click="emitClose"
              >
                {{ t("global.cancel") }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </form>
  </Transition>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue3-i18n";
import { useProgramService } from "@/inversify.config";
import { notifyError, notifySuccess } from "@/notify";

const MIN_LENGTH = 2;
const MAX_LENGTH = 100;
const VALID_PATTERN = /^[\p{L}\p{N}][\p{L}\p{N} \-'’().,/&]*$/u;

const props = defineProps<{
  programId: string;
  existingLabels?: string[];
}>();
const emit = defineEmits<{
  (event: "close"): void;
  (event: "added", skill: { id: string; label: string }): void;
}>();

const { t } = useI18n();
const programService = useProgramService();

const label = ref("");
const errorMessage = ref("");
const isSaving = ref(false);
const inputRef = ref<HTMLInputElement | null>(null);

onMounted(() => inputRef.value?.focus());

const validationError = computed<string>(() => {
  const trimmed = label.value.trim();
  if (trimmed.length === 0) return "";
  if (trimmed.length < MIN_LENGTH)
    return t("pages.programs.skillTooShort", { min: MIN_LENGTH });
  if (trimmed.length > MAX_LENGTH)
    return t("pages.programs.skillTooLong", { max: MAX_LENGTH });
  if (!VALID_PATTERN.test(trimmed))
    return t("pages.programs.skillInvalidChars");
  const normalized = trimmed.toLowerCase();
  const existing = (props.existingLabels ?? []).map((x) => x.trim().toLowerCase());
  if (existing.includes(normalized)) return t("pages.programs.skillDuplicate");
  return "";
});

const isValid = computed(() => {
  const trimmed = label.value.trim();
  return trimmed.length >= MIN_LENGTH && validationError.value === "";
});

const displayedError = computed(() => errorMessage.value || validationError.value);

function emitClose() {
  if (isSaving.value) return;
  emit("close");
}

async function onSubmit() {
  if (!isValid.value) {
    errorMessage.value = validationError.value || t("pages.programs.skillInvalid");
    return;
  }

  isSaving.value = true;
  errorMessage.value = "";
  try {
    const skill = await programService.createSkill(label.value.trim());
    await programService.addSkillToProgram(props.programId, skill.id);
    notifySuccess(t("pages.programs.skillAdded"));
    emit("added", skill);
    emit("close");
  } catch (error) {
    console.error(error);
    errorMessage.value = t("pages.programs.skillAddError");
    notifyError(t("pages.programs.skillAddError"));
  } finally {
    isSaving.value = false;
  }
}
</script>

<style scoped lang="scss">
.form__input--error {
  border-color: #e74c3c !important;
}

.form__error {
  margin-top: 6px;
  font-size: 0.85rem;
  color: #c0392b;
  background: #fff3f3;
  border: 1px solid #f5c2c2;
  border-radius: 6px;
  padding: 6px 10px;
}

.form__hint {
  margin-top: 4px;
  font-size: 0.75rem;
  color: #6b7280;
  text-align: right;
}

.fade-leave-active,
.fade-enter-active {
  transition: opacity 0.2s cubic-bezier(0.69, 0.33, 0.16, 0.97);
}

.fade-enter-to,
.fade-leave-from {
  opacity: 1;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
