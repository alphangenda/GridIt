<template>
  <Transition name="fade">
    <form class="popup" novalidate @submit.prevent="handleSubmit">
      <div class="popup__bg" @click="emit('close')"></div>
      <div class="popup__container" :class="{ 'popup__container--wide': isDuplicating }">
        <div class="popup__header">
          <p class="popup__title h2-like">{{ t("navigation.addClass") }}</p>
        </div>
        <div class="popup__content">
          <div class="popup__block">
            <!-- Course name -->
            <div class="form__group">
              <label class="form__label" for="class-name">{{ t("navigation.className") }}</label>
              <input
                id="class-name"
                ref="inputRef"
                v-model="name"
                class="form__input"
                type="text"
                required
              />
            </div>

            <!-- Program -->
            <div class="form__group">
              <label class="form__label" for="class-program">
                {{ t("pages.classes.program") }}
              </label>
              <select id="class-program" v-model="selectedProgramId" class="form__input">
                <option value="">—</option>
                <option v-for="program in programs" :key="program.id" :value="program.id">
                  {{ program.name }}
                </option>
              </select>
            </div>

            <!-- Toggle duplication -->
            <button
              type="button"
              class="duplicate-trigger"
              :class="{ 'duplicate-trigger--active': isDuplicating }"
              @click="isDuplicating = !isDuplicating"
            >
              <span class="duplicate-trigger__icon">&#128203;</span>
              <span>{{ t("navigation.duplicateFromCourse") }}</span>
            </button>

            <!-- Duplication section -->
            <template v-if="isDuplicating">
              <!-- Filter toggle -->
              <div class="duplicate-filter">
                <button
                  type="button"
                  class="btn btn--small"
                  :class="{ 'btn--active': sourceFilter === 'mine' }"
                  @click="sourceFilter = 'mine'"
                >
                  {{ t("navigation.myCourses") }}
                </button>
                <button
                  type="button"
                  class="btn btn--small"
                  :class="{ 'btn--active': sourceFilter === 'shared' }"
                  @click="sourceFilter = 'shared'"
                >
                  {{ t("navigation.sharedCourses") }}
                </button>
              </div>

              <!-- Source course list -->
              <div v-if="isLoadingSources" class="duplicate-loading">
                <Loader />
              </div>
              <div v-else-if="filteredSources.length > 0" class="duplicate-list">
                <div
                  v-for="source in filteredSources"
                  :key="source.classId"
                  class="duplicate-item"
                  :class="{ 'duplicate-item--selected': selectedSource?.classId === source.classId }"
                  @click="selectSource(source)"
                >
                  <div class="duplicate-item__info">
                    <strong>{{ source.className }}</strong>
                    <span class="duplicate-item__session">{{ source.sessionName }}</span>
                    <span v-if="!source.isOwner" class="duplicate-item__prof">{{ source.creatorEmail }}</span>
                  </div>
                  <div class="duplicate-item__counts">
                    <span>{{ source.skills.length }} {{ t("navigation.duplicateSkills") }}</span>
                    <span>{{ source.exams.length }} {{ t("navigation.duplicateExams") }}</span>
                  </div>
                </div>
              </div>
              <p v-else class="duplicate-empty">{{ t("navigation.noDuplicationSources") }}</p>

              <!-- Preview with editable exam names -->
              <div v-if="selectedSource" class="duplicate-preview">
                <h3 class="duplicate-preview__title">{{ t("navigation.duplicatePreview") }}</h3>

                <div v-if="selectedSource.skills.length > 0" class="duplicate-preview__section">
                  <h4>{{ t("navigation.duplicateSkillsTitle") }}</h4>
                  <ul>
                    <li v-for="skill in selectedSource.skills" :key="skill.id">{{ skill.label }}</li>
                  </ul>
                </div>

                <div v-if="examNames.length > 0" class="duplicate-preview__section">
                  <h4>{{ t("navigation.duplicateExamsTitle") }}</h4>
                  <div
                    v-for="(entry, index) in examNames"
                    :key="entry.sourceExamId"
                    class="duplicate-preview__exam-row"
                  >
                    <input
                      v-model="examNames[index].name"
                      class="form__input duplicate-preview__exam-input"
                      type="text"
                    />
                  </div>
                </div>
              </div>
            </template>

            <div class="form__submit">
              <button class="btn btn--fullscreen" type="submit">{{ t("global.add") }}</button>
              <button class="btn btn--fullscreen btn--red" type="button" @click="emit('close')">{{ t("global.cancel") }}</button>
            </div>
          </div>
        </div>
      </div>
    </form>
  </Transition>
</template>

<script lang="ts" setup>
import { ref, computed, onMounted, watch } from "vue";
import { useI18n } from "vue3-i18n";
import { useClassesStore } from "@/stores/classesStore";
import { useSessionsStore } from "@/stores/sessionsStore";
import { useClassService, useProgramService } from "@/inversify.config";
import { notifyError, notifySuccess } from "@/notify";
import Loader from "@/components/layouts/items/Loader.vue";
import type { IDuplicationSource } from "@/injection/interfaces";

type ProgramItem = {
  id: string;
  name: string;
};

const emit = defineEmits<{
  (event: "close"): void;
}>();

const { t } = useI18n();
const classesStore = useClassesStore();
const sessionsStore = useSessionsStore();
const programService = useProgramService();
const classService = useClassService();

const name = ref("");
const inputRef = ref<HTMLInputElement | null>(null);
const programs = ref<ProgramItem[]>([]);
const selectedProgramId = ref<string>("");

// Duplication state
const isDuplicating = ref(false);
const sourceFilter = ref<"mine" | "shared">("mine");
const sources = ref<IDuplicationSource[]>([]);
const selectedSource = ref<IDuplicationSource | null>(null);
const isLoadingSources = ref(false);
const examNames = ref<Array<{ sourceExamId: string; name: string }>>([]);

const filteredSources = computed(() => {
  return sources.value.filter((s) =>
    sourceFilter.value === "mine" ? s.isOwner : !s.isOwner
  );
});

onMounted(async () => {
  inputRef.value?.focus();
  await loadPrograms();
});

watch(isDuplicating, async (val) => {
  if (val && sources.value.length === 0) {
    isLoadingSources.value = true;
    try {
      sources.value = await classService.getDuplicationSources();
    } catch {
      notifyError(t("navigation.duplicateLoadError"));
    } finally {
      isLoadingSources.value = false;
    }
  }
  if (!val) {
    selectedSource.value = null;
    examNames.value = [];
  }
});

function selectSource(source: IDuplicationSource) {
  selectedSource.value = source;
  examNames.value = source.exams.map((e) => ({
    sourceExamId: e.id,
    name: e.name,
  }));
}

async function loadPrograms() {
  try {
    const data = await programService.getAllPrograms();
    programs.value = data.map((x) => ({ id: String(x.id), name: String(x.name) }));
  } catch (error) {
    console.error(error);
    notifyError(t("pages.classes.programsLoadError"));
  }
}

async function handleSubmit() {
  const trimmed = name.value.trim();
  if (!trimmed) return;

  try {
    let createdClass;

    if (isDuplicating.value && selectedSource.value) {
      // Create with duplication
      createdClass = await classesStore.duplicateClass({
        name: trimmed,
        programId: selectedProgramId.value || undefined,
        sourceClassId: selectedSource.value.classId,
        exams: examNames.value,
      });
      notifySuccess(t("navigation.duplicateSuccess"));
    } else {
      // Normal creation
      createdClass = await classesStore.addClass(
        trimmed,
        [],
        selectedProgramId.value || undefined
      );
    }

    const selectedSession = sessionsStore.getSelectedSession;

    if (selectedSession?.id) {
      const updatedClassIds = [...(selectedSession.classIds ?? []), createdClass.id];

      await sessionsStore.updateSession(
        selectedSession.id,
        selectedSession.name ?? "",
        updatedClassIds
      );
    }

    emit("close");
  } catch (error) {
    console.error(error);
    notifyError(t("pages.classes.addError"));
  }
}
</script>

<style scoped lang="scss">
@use "@/sass/tools" as *;

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

.popup__container--wide {
  max-width: 600px;
}

.duplicate-trigger {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  width: 100%;
  padding: 12px 16px;
  margin-bottom: 8px;
  border: 2px dashed $color-green;
  border-radius: $common-border-radius;
  background-color: rgba($color-green, 0.04);
  color: $color-green;
  font-size: 0.9rem;
  font-weight: 600;
  cursor: pointer;
  transition: background-color 0.2s, border-color 0.2s;

  &:hover {
    background-color: rgba($color-green, 0.1);
  }

  &--active {
    border-style: solid;
    background-color: rgba($color-green, 0.08);
  }

  &__icon {
    font-size: 1.1rem;
  }
}

.duplicate-filter {
  display: flex;
  gap: 8px;
  margin-bottom: 16px;
}

.btn--active {
  background-color: $color-green;
  color: $color-white;
  border-color: $color-green;
}

.duplicate-loading {
  display: flex;
  justify-content: center;
  padding: 24px;
}

.duplicate-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  max-height: 180px;
  overflow-y: auto;
  margin-bottom: 16px;
}

.duplicate-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 16px;
  border: 1px solid $color-border;
  border-radius: $common-border-radius;
  cursor: pointer;
  transition: border-color 0.2s, background-color 0.2s;

  &:hover {
    border-color: $color-green;
  }

  &--selected {
    border-color: $color-green;
    background-color: rgba($color-green, 0.05);
  }

  &__info {
    display: flex;
    flex-direction: column;
    gap: 2px;
  }

  &__session {
    font-size: 0.85rem;
    color: $color-grey-medium;
  }

  &__prof {
    font-size: 0.8rem;
    color: $color-grey;
    font-style: italic;
  }

  &__counts {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    gap: 2px;
    font-size: 0.8rem;
    color: $color-grey-medium;
  }
}

.duplicate-empty {
  text-align: center;
  color: $color-grey-medium;
  padding: 16px;
}

.duplicate-preview {
  border: 1px solid $color-border;
  border-radius: $common-border-radius;
  padding: 16px;
  margin-bottom: 16px;
  max-height: 200px;
  overflow-y: auto;

  &__title {
    font-size: 0.95rem;
    font-weight: 600;
    margin-bottom: 12px;
  }

  &__section {
    &:not(:last-child) {
      margin-bottom: 12px;
    }

    h4 {
      font-size: 0.85rem;
      font-weight: 600;
      color: $color-grey-medium;
      margin-bottom: 4px;
    }

    ul {
      list-style: none;
      padding: 0;
      margin: 0;

      li {
        font-size: 0.85rem;
        padding: 2px 0;
        padding-left: 12px;
        position: relative;

        &::before {
          content: "•";
          position: absolute;
          left: 0;
          color: $color-green;
        }
      }
    }
  }

  &__exam-row {
    &:not(:last-child) {
      margin-bottom: 8px;
    }
  }

  &__exam-input {
    font-size: 0.85rem;
    padding: 8px 10px;
  }
}
</style>
