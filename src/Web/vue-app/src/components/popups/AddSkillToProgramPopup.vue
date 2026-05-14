<template>
  <Transition name="fade">
    <form class="popup" novalidate @submit.prevent="onSubmit">
      <div class="popup__bg" @click="emitClose"></div>
      <div class="popup__container popup__container--wide">
        <div class="popup__header">
          <p class="popup__title h2-like">{{ t("pages.programs.addSkillTitle") }}</p>
        </div>
        <div class="popup__content">
          <div class="popup__block">
            <!-- Skill name -->
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

            <!-- Sub-competencies section -->
            <div class="sub-competencies">
              <div class="sub-competencies__header">
                <label class="form__label">{{ t("pages.programs.subSkillsLabel") }}</label>
                <button
                  type="button"
                  class="btn btn--small btn--outline"
                  :disabled="isSaving"
                  @click="addSubSkill"
                >
                  + {{ t("pages.programs.addSubSkill") }}
                </button>
              </div>

              <p v-if="subSkills.length === 0" class="sub-competencies__empty">
                {{ t("pages.programs.noSubSkills") }}
              </p>

              <div
                v-for="(sub, idx) in subSkills"
                :key="sub.key"
                class="sub-competencies__row"
              >
                <input
                  v-model="sub.label"
                  class="form__input sub-competencies__input"
                  type="text"
                  :placeholder="t('pages.programs.subSkillPlaceholder')"
                  :maxlength="MAX_LENGTH"
                  :disabled="isSaving"
                />
                <button
                  type="button"
                  class="sub-competencies__remove"
                  :disabled="isSaving"
                  :title="t('global.delete')"
                  @click="removeSubSkill(idx)"
                >
                  &times;
                </button>
              </div>
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
const VALID_PATTERN = /^[\p{L}\p{N}][\p{L}\p{N} \-''().,/&]*$/u;

interface SubSkill {
  key: number;
  label: string;
}

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

const subSkills = ref<SubSkill[]>([]);
let nextKey = 0;

onMounted(() => inputRef.value?.focus());

function addSubSkill() {
  subSkills.value.push({ key: nextKey++, label: "" });
}

function removeSubSkill(idx: number) {
  subSkills.value.splice(idx, 1);
}

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

    const validSubs = subSkills.value.filter((s) => s.label.trim().length > 0);
    if (validSubs.length > 0) {
      await programService.saveCriteriaTemplates(
        skill.id,
        validSubs.map((s) => ({
          label: s.label.trim(),
          defaultTotalValue: 15,
        }))
      );
    }

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
.popup__container--wide {
  max-width: 600px;
}

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

/* Sub-competencies */
.sub-competencies {
  margin-top: 20px;
  border-top: 1px solid #e5e7eb;
  padding-top: 16px;
}

.sub-competencies__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 10px;
}

.sub-competencies__empty {
  font-size: 0.85rem;
  color: #6b7280;
  margin: 0 0 8px;
}

.sub-competencies__row {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
}

.sub-competencies__input {
  flex: 1;
}

.sub-competencies__remove {
  background: transparent;
  border: 1px solid #e5e7eb;
  border-radius: 4px;
  cursor: pointer;
  font-size: 1.2rem;
  line-height: 1;
  padding: 4px 8px;
  color: #c0392b;

  &:hover:not(:disabled) {
    background: #fee2e2;
  }

  &:disabled {
    opacity: 0.4;
    cursor: not-allowed;
  }
}

.btn--small {
  font-size: 0.8rem;
  padding: 4px 12px;
}

.btn--outline {
  background: transparent;
  border: 1px solid #6b7280;
  color: #374151;

  &:hover {
    background: #f3f4f6;
  }
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
