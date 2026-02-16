<template>
  <div class="session-detail">
    <div class="session-detail__header">
      <h1>{{ sessionItem?.name ?? t("navigation.sessions") }}</h1>
    </div>

    <div class="session-detail__panels">
      <!-- Left Panel: Courses -->
      <div class="session-detail__panel session-detail__panel--courses">
        <div class="session-detail__panel-header">
          <h2>{{ t("navigation.classes") }}</h2>
        </div>
        <div class="session-detail__panel-content">
          <div
            v-for="classItem in sessionClasses"
            :key="classItem.id"
            class="session-detail__item"
            :class="{ 'session-detail__item--active': selectedClassId === classItem.id }"
            @click="selectClass(classItem.id as string)"
          >
            {{ classItem.name }}
          </div>
          <div v-if="sessionClasses.length === 0" class="session-detail__empty">
            {{ t("navigation.noClasses") }}
          </div>
        </div>
        <div class="session-detail__panel-footer">
          <button type="button" class="btn btn--fullscreen" @click="onAddClass">
            {{ t("navigation.addClass") }}
          </button>
        </div>
      </div>

      <!-- Right Panel: Exams -->
      <div class="session-detail__panel session-detail__panel--exams">
        <div class="session-detail__panel-header">
          <h2>{{ t("navigation.exams") }}</h2>
        </div>
        <div class="session-detail__panel-content">
          <div
            v-for="exam in selectedClassExams"
            :key="exam.id"
            class="session-detail__item session-detail__item--exam"
            @click="goToExamDetail(exam.id!)"
          >
            {{ exam.name }}
          </div>
          <div v-if="!selectedClassId" class="session-detail__empty">
            {{ t("navigation.selectClass") }}
          </div>
          <div v-else-if="selectedClassExams.length === 0" class="session-detail__empty">
            {{ t("navigation.noExams") }}
          </div>
        </div>
        <div class="session-detail__panel-footer">
          <button
            type="button"
            class="btn btn--fullscreen"
            :disabled="!selectedClassId"
            @click="onAddExam"
          >
            {{ t("navigation.addExam") }}
          </button>
        </div>
      </div>
    </div>
    <AddClassToSessionPopup
      v-if="showAddClassPopup"
      :session-id="sessionId"
      :current-class-ids="sessionItem?.classIds"
      @close="showAddClassPopup = false"
    />
    <CreateExamPopup
      v-if="showCreateExamPopup && selectedClassId"
      :class-id="selectedClassId"
      @close="handleExamPopupClose"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useI18n } from "vue3-i18n";
import { useSessionsStore } from "@/stores/sessionsStore";
import { useClassesStore } from "@/stores/classesStore";
import CreateClassPopup from "@/components/popups/CreateClassPopup.vue";
import CreateExamPopup from "@/components/popups/CreateExamPopup.vue";
import AddClassToSessionPopup from "@/components/popups/AddClassToSessionPopup.vue";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const sessionsStore = useSessionsStore();
const classesStore = useClassesStore();

const sessionId = computed(() => route.params.sessionId as string);
const selectedClassId = ref<string | null>(null);
const showAddClassPopup = ref(false);
const showCreateExamPopup = ref(false);

const sessionItem = computed(() =>
  sessionsStore.getSessions.find((s) => s.id === sessionId.value)
);

const sessionClasses = computed(() => {
  if (!sessionItem.value?.classIds) return [];
  return sessionItem.value.classIds
    .map((classId) => classesStore.getClasses.find((c) => c.id === classId))
    .filter((c) => c !== undefined)
    .map((c) => c!) as typeof classesStore.getClasses;
});

const selectedClassExams = computed(() => {
  if (!selectedClassId.value) return [];
  return classesStore.getExamsForClass(selectedClassId.value);
});

onMounted(() => {
  sessionsStore.fetchSessions();
  classesStore.fetchClasses();

  // Auto-select first class if available
  if (sessionClasses.value.length > 0 && sessionClasses.value[0].id) {
    selectedClassId.value = sessionClasses.value[0].id;
    classesStore.fetchExams(selectedClassId.value);
  }
});

watch(selectedClassId, (newId) => {
  if (newId) {
    classesStore.fetchExams(newId);
  }
});

watch(
  () => sessionItem.value?.classIds,
  () => {
    // Auto-select first class when classes are loaded
    if (sessionClasses.value.length > 0) {
      // If current selection is no longer in session, select first available
      if (
        !selectedClassId.value ||
        !sessionClasses.value.some((c) => c.id === selectedClassId.value)
      ) {
        const firstClassId = sessionClasses.value[0].id;
        if (firstClassId) {
          selectedClassId.value = firstClassId;
          classesStore.fetchExams(firstClassId);
        }
      }
    } else {
      selectedClassId.value = null;
    }
  },
  { deep: true }
);

function selectClass(classId: string) {
  selectedClassId.value = classId;
  classesStore.fetchExams(classId);
}

function goToExamDetail(examId: string) {
  if (selectedClassId.value) {
    router.push({
      name: "classes.examDetail",
      params: { classId: selectedClassId.value, examId: examId },
    });
  }
}

function onAddClass() {
  showAddClassPopup.value = true;
}

function onAddExam() {
  if (selectedClassId.value) {
    showCreateExamPopup.value = true;
  }
}

function handleExamPopupClose() {
  showCreateExamPopup.value = false;
  // Reload exams for the selected class
  if (selectedClassId.value) {
    classesStore.fetchExams(selectedClassId.value);
  }
}
</script>

<style scoped lang="scss">
@use "@/sass/tools" as *;

.session-detail {
  &__header {
    margin-bottom: 32px;

    h1 {
      font-size: rem(24);
      font-weight: 600;
      color: $color-text;
    }
  }

  &__panels {
    display: grid;
    grid-template-columns: 1fr;
    gap: 24px;

    @include breakpoint(md-up) {
      grid-template-columns: 240px 240px;
    }
  }

  &__panel {
    background-color: $color-white;
    border-radius: $common-border-radius;
    border: 1px solid $color-border;
    display: flex;
    flex-direction: column;
    overflow: hidden;

    &-header {
      padding: 16px 20px;
      background-color: $color-grey-dark;
      border-bottom: 1px solid $color-border;

      h2 {
        font-size: rem(13);
        font-weight: 600;
        text-transform: uppercase;
        color: rgba($color-white, 0.85);
        margin: 0;
      }
    }

    &-content {
      flex: 1;
      overflow-y: auto;
      min-height: 300px;
      max-height: 600px;
    }

    &-footer {
      padding: 16px 20px;
      border-top: 1px solid $color-border;
      background-color: $color-grey-lighter;
    }
  }

  &__item {
    padding: 12px 20px;
    cursor: pointer;
    transition: background-color 0.15s ease;
    border-bottom: 1px solid $color-border;
    color: $color-text;

    &:hover {
      background-color: $color-grey-lighter;
    }

    &--active {
      background-color: $color-grey-dark;
      color: $color-white;
      font-weight: 500;
    }

    &--exam {
      color: rgba($color-text, 0.7);

      &:hover {
        background-color: $color-grey-lighter;
        color: $color-text;
      }
    }
  }

  &__empty {
    padding: 24px 20px;
    text-align: center;
    color: rgba($color-text, 0.5);
    font-size: rem(14);
  }
}
</style>
