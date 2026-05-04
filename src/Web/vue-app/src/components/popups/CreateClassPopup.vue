<template>
  <Transition name="fade">
    <form class="popup" novalidate @submit.prevent="handleSubmit">
      <div class="popup__bg" @click="emit('close')"></div>
      <div class="popup__container popup__container--create-class">
        <div class="popup__header">
          <p class="popup__title h2-like">{{ t("navigation.addClass") }}</p>
        </div>
        <div class="popup__content">
          <div v-if="!hasSessions" class="popup__block create-class-no-session">
            <p class="create-class-no-session__msg">{{ t("navigation.noSessionWarning") }}</p>
            <button type="button" class="btn" @click="goToSessions">
              {{ t("navigation.goToSessions") }}
            </button>
          </div>
          <div v-else class="popup__block">
            <div class="create-class-grid">
              <!-- LEFT COLUMN: Normal add -->
              <div class="create-class-grid__left">
                <h3 class="create-class-grid__section-title">{{ t("navigation.normalAdd") }}</h3>

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

                <div class="form__group">
                  <label class="form__label" for="class-program">
                    {{ t("pages.classes.program") }}
                  </label>
                  <select
                    id="class-program"
                    v-model="selectedProgramId"
                    class="form__input"
                    :disabled="!!selectedSource"
                  >
                    <option value="">—</option>
                    <option v-for="program in programs" :key="program.id" :value="program.id">
                      {{ program.name }}
                    </option>
                  </select>
                </div>
              </div>

              <!-- RIGHT COLUMN: Copy from existing -->
              <div class="create-class-grid__right" :class="{ 'create-class-grid__right--disabled': !!selectedProgramId }">
                <h3 class="create-class-grid__section-title">{{ t("navigation.copyFromExisting") }}</h3>

                <!-- Filter toggle -->
                <div class="duplicate-filter">
                  <button
                    type="button"
                    class="btn btn--small"
                    :class="{ 'btn--active': sourceFilter === 'mine' }"
                    :disabled="!!selectedProgramId"
                    @click="sourceFilter = 'mine'"
                  >
                    {{ t("navigation.myCourses") }}
                  </button>
                  <button
                    type="button"
                    class="btn btn--small"
                    :class="{ 'btn--active': sourceFilter === 'shared' }"
                    :disabled="!!selectedProgramId"
                    @click="sourceFilter = 'shared'"
                  >
                    {{ t("navigation.sharedCourses") }}
                  </button>
                </div>

                <!-- Source course list -->
                <div v-if="isLoadingSources" class="duplicate-loading">
                  <Loader />
                </div>
                <template v-else-if="filteredSources.length > 0">
                  <div class="duplicate-list">
                    <div
                      v-for="source in filteredSources"
                      :key="source.classId"
                      class="duplicate-item"
                      :class="{ 'duplicate-item--selected': selectedSource?.classId === source.classId, 'duplicate-item--disabled': !!selectedProgramId }"
                      @click="!selectedProgramId && selectSource(source)"
                    >
                      <div class="duplicate-item__info">
                        <strong>{{ source.className }}</strong>
                        <span class="duplicate-item__program">{{ source.programName || t("navigation.noProgram") }}</span>
                        <span class="duplicate-item__session">{{ source.sessionName }}</span>
                        <span v-if="!source.isOwner" class="duplicate-item__prof">{{ source.creatorEmail }}</span>
                      </div>
                      <div class="duplicate-item__counts">
                        <span>{{ source.skills.length }} {{ t("navigation.duplicateSkills") }}</span>
                      </div>
                    </div>
                  </div>
                </template>
                <p v-else class="duplicate-empty">{{ t("navigation.noDuplicationSources") }}</p>

                <!-- Selected source preview -->
                <div v-if="selectedSource" class="duplicate-preview">
                  <div class="duplicate-preview__header">
                    <h4 class="duplicate-preview__title">{{ selectedSource.className }}</h4>
                    <button type="button" class="duplicate-preview__clear" @click="clearSelection">
                      &times;
                    </button>
                  </div>

                  <div v-if="selectedSource.skills.length > 0" class="duplicate-preview__section">
                    <h4>{{ t("navigation.copiedSkills") }}</h4>
                    <ul>
                      <li v-for="skill in selectedSource.skills" :key="skill.id">{{ skill.label }}</li>
                    </ul>
                  </div>

                  <!-- Exams to create -->
                  <div class="duplicate-preview__section">
                    <h4>{{ t("navigation.examsToCreate") }}</h4>
                    <div
                      v-for="(examName, index) in examNames"
                      :key="index"
                      class="duplicate-preview__exam-row"
                    >
                      <input
                        v-model="examNames[index]"
                        class="form__input duplicate-preview__exam-input"
                        type="text"
                        :placeholder="t('navigation.addExamPlaceholder')"
                      />
                      <button type="button" class="duplicate-preview__exam-remove" @click="removeExam(index)">
                        &times;
                      </button>
                    </div>
                    <button type="button" class="duplicate-preview__add-exam" @click="addExam">
                      + {{ t("navigation.addExam") }}
                    </button>
                  </div>
                </div>
              </div>
            </div>

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
import { useRouter } from "vue-router";
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

const router = useRouter();
const { t } = useI18n();
const classesStore = useClassesStore();
const sessionsStore = useSessionsStore();
const programService = useProgramService();
const classService = useClassService();

const hasSessions = computed(() => sessionsStore.getSessions.length > 0);

function goToSessions() {
  emit("close");
  router.push({ name: "sessions.index" });
}

const name = ref("");
const inputRef = ref<HTMLInputElement | null>(null);
const programs = ref<ProgramItem[]>([]);
const selectedProgramId = ref<string>("");

const sourceFilter = ref<"mine" | "shared">("mine");
const sources = ref<IDuplicationSource[]>([]);
const selectedSource = ref<IDuplicationSource | null>(null);
const isLoadingSources = ref(false);
const examNames = ref<string[]>([]);

const filteredSources = computed(() => {
  return sources.value.filter((s) =>
    sourceFilter.value === "mine" ? s.isOwner : !s.isOwner
  );
});

onMounted(async () => {
  await sessionsStore.fetchSessions();
  if (!hasSessions.value) return;
  inputRef.value?.focus();
  await Promise.all([loadPrograms(), loadSources()]);
});

async function loadSources() {
  isLoadingSources.value = true;
  try {
    sources.value = await classService.getDuplicationSources();
  } catch {
    notifyError(t("navigation.duplicateLoadError"));
  } finally {
    isLoadingSources.value = false;
  }
}

watch(selectedProgramId, (val) => {
  if (val) {
    selectedSource.value = null;
    examNames.value = [];
  }
});

function selectSource(source: IDuplicationSource) {
  selectedSource.value = source;
  examNames.value = [];
  selectedProgramId.value = "";
}

function clearSelection() {
  selectedSource.value = null;
  examNames.value = [];
}

function addExam() {
  examNames.value.push("");
}

function removeExam(index: number) {
  examNames.value.splice(index, 1);
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

    if (selectedSource.value) {
      const validExamNames = examNames.value
        .map((n) => n.trim())
        .filter((n) => n.length > 0);

      createdClass = await classesStore.duplicateClass({
        name: trimmed,
        programId: selectedProgramId.value || undefined,
        sourceClassId: selectedSource.value.classId,
        examNames: validExamNames,
      });
      notifySuccess(t("navigation.duplicateSuccess"));
    } else {
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

    await router.push({
      name: "classes.detail",
      params: { classId: createdClass.id },
    });
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

.popup__container--create-class {
  max-width: 860px;
}

.create-class-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 32px;

  @media (max-width: 640px) {
    grid-template-columns: 1fr;
  }

  &__left,
  &__right {
    display: flex;
    flex-direction: column;
    gap: 16px;
  }

  &__right--disabled {
    opacity: 0.4;
    pointer-events: none;
  }

  &__section-title {
    font-size: 0.95rem;
    font-weight: 600;
    color: $color-grey-medium;
    padding-bottom: 8px;
    border-bottom: 1px solid $color-border;
    margin: 0;
  }
}

.duplicate-filter {
  display: flex;
  gap: 8px;
}

.btn--active {
  background-color: $color-beige;
  color: $color-white;
  border-color: $color-beige;
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
  max-height: 200px;
  overflow-y: auto;
}

.duplicate-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 14px;
  border: 1px solid $color-border;
  border-radius: $common-border-radius;
  cursor: pointer;
  transition: border-color 0.2s, background-color 0.2s;

  &:hover {
    border-color: $color-beige;
  }

  &--selected {
    border-color: $color-beige;
    background-color: rgba($color-beige, 0.05);
  }

  &--disabled {
    opacity: 0.4;
    pointer-events: none;
    cursor: default;
  }

  &__info {
    display: flex;
    flex-direction: column;
    gap: 2px;
    min-width: 0;

    strong {
      font-size: 0.9rem;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
  }

  &__program {
    font-size: 0.8rem;
    color: $color-beige-dark;
    font-weight: 500;
  }

  &__session {
    font-size: 0.8rem;
    color: $color-grey-medium;
  }

  &__prof {
    font-size: 0.75rem;
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
    flex-shrink: 0;
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
  padding: 14px;
  max-height: 260px;
  overflow-y: auto;

  &__header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 12px;
  }

  &__title {
    font-size: 0.9rem;
    font-weight: 600;
    margin: 0;
  }

  &__clear {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 26px;
    height: 26px;
    border: 1px solid $color-border;
    border-radius: 50%;
    background: none;
    color: $color-grey-medium;
    font-size: 1.1rem;
    cursor: pointer;
    transition: all 0.15s ease;
    flex-shrink: 0;

    &:hover {
      background-color: rgba($color-red, 0.08);
      border-color: $color-red;
      color: $color-red;
    }
  }

  &__section {
    &:not(:last-child) {
      margin-bottom: 12px;
    }

    h4 {
      font-size: 0.8rem;
      font-weight: 600;
      color: $color-grey-medium;
      margin: 0 0 4px;
    }

    ul {
      list-style: none;
      padding: 0;
      margin: 0;

      li {
        font-size: 0.8rem;
        padding: 2px 0;
        padding-left: 12px;
        position: relative;

        &::before {
          content: "•";
          position: absolute;
          left: 0;
          color: $color-beige;
        }
      }
    }
  }

  &__exam-row {
    display: flex;
    align-items: center;
    gap: 6px;

    &:not(:last-child) {
      margin-bottom: 6px;
    }
  }

  &__exam-input {
    font-size: 0.8rem;
    padding: 6px 8px;
    flex: 1;
  }

  &__exam-remove {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 24px;
    height: 24px;
    border: none;
    border-radius: 4px;
    background: none;
    color: $color-grey-medium;
    font-size: 1.1rem;
    cursor: pointer;
    flex-shrink: 0;
    transition: all 0.15s ease;

    &:hover {
      background-color: rgba($color-red, 0.08);
      color: $color-red;
    }
  }

  &__add-exam {
    display: block;
    width: 100%;
    margin-top: 8px;
    padding: 6px 10px;
    font-size: 0.8rem;
    font-weight: 500;
    color: $color-beige-dark;
    background: none;
    border: 1px dashed rgba($color-beige, 0.4);
    border-radius: $common-border-radius;
    cursor: pointer;
    text-align: left;
    transition: all 0.15s ease;

    &:hover {
      background-color: rgba($color-beige, 0.06);
      border-color: $color-beige;
      color: $color-beige;
    }
  }
}

.create-class-no-session {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 20px;
  padding: 32px 16px;
  text-align: center;

  &__msg {
    font-size: 1rem;
    font-weight: 600;
    color: $color-grey-medium;
    margin: 0;
  }
}
</style>
