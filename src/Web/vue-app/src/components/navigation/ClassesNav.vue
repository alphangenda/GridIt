<template>
  <nav class="side-nav side-nav--classes">
    <RouterLink :to="{ name: 'classes.index' }" class="side-nav__title side-nav__title--link">
      {{ t("navigation.classes") }}
    </RouterLink>
    <ul class="side-nav__list">
      <li v-for="cours in filteredClasses" :key="cours.id" class="side-nav__item">
        <button
          v-if="cours.id"
          class="side-nav__link side-nav__link--expandable"
          :class="{
            'side-nav__link--expanded': expandedClassId === cours.id,
          }"
          @click="toggleClass(cours.id!)"
        >
          <span class="side-nav__link-text">{{ cours.name }}</span>
          <span
            class="side-nav__chevron"
            :class="{ 'side-nav__chevron--open': expandedClassId === cours.id }"
          >&#9660;</span>
        </button>
        <Transition name="slide">
          <div v-if="expandedClassId === cours.id" class="side-nav__dropdown">
            <ul class="side-nav__sub-list">
              <li v-for="exam in getExams(cours.id!)" :key="exam.id">
                <RouterLink
                  v-if="exam.id"
                  :to="{ name: 'classes.examDetail', params: { classId: cours.id, examId: exam.id } }"
                  class="side-nav__sub-link"
                  active-class="side-nav__sub-link--active"
                >
                  {{ exam.name }}
                </RouterLink>
              </li>
              <li v-if="getExams(cours.id!).length === 0" class="side-nav__sub-empty">
                {{ t("navigation.noExams") }}
              </li>
            </ul>
            <button
              type="button"
              class="side-nav__add-exam-btn"
              @click.stop="openCreateExam(cours.id!)"
            >
              + {{ t("navigation.addExam") }}
            </button>
          </div>
        </Transition>
      </li>
    </ul>

    <CreateExamPopup
      v-if="showCreatePopup && createExamClassId"
      :class-id="createExamClassId"
      @close="showCreatePopup = false"
    />
  </nav>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { useRoute, useRouter, onBeforeRouteUpdate } from "vue-router";
import { useI18n } from "vue3-i18n";
import { useClassesStore } from "@/stores/classesStore";
import { useSessionsStore } from "@/stores/sessionsStore";
import CreateExamPopup from "@/components/popups/CreateExamPopup.vue";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const classesStore = useClassesStore();
const sessionsStore = useSessionsStore();

const expandedClassId = ref<string | null>(null);
const showCreatePopup = ref(false);
const createExamClassId = ref<string | null>(null);
const initialized = ref(false);

const activeClassId = computed(() => route.params.classId as string | undefined);

const filteredClasses = computed(() => {
  const selectedId = sessionsStore.getSelectedSessionId;
  if (!selectedId) {
    return classesStore.getClasses;
  }

  const selectedSession = sessionsStore.getSelectedSession;
  const classIds = selectedSession?.classIds ?? [];
  if (!classIds.length) {
    return [];
  }

  return classesStore.getClasses.filter(
    (c) => c.id && classIds.includes(c.id)
  );
});

function getExams(classId: string) {
  return classesStore.getExamsForClass(classId);
}

function toggleClass(classId: string) {
  if (expandedClassId.value === classId) {
    expandedClassId.value = null;
  } else {
    expandedClassId.value = classId;
    classesStore.fetchExams(classId);
    router.push({ name: "classes.detail", params: { classId } });
  }
}

function openCreateExam(classId: string) {
  createExamClassId.value = classId;
  showCreatePopup.value = true;
}

// Auto-expand once when classes are first loaded and a classId is in the URL
watch(
  filteredClasses,
  (classes) => {
    if (!initialized.value && classes.length > 0 && activeClassId.value) {
      expandedClassId.value = activeClassId.value;
      classesStore.fetchExams(activeClassId.value);
      initialized.value = true;
    }
  },
  { immediate: true }
);

// Auto-expand when navigating to a different class from elsewhere
onBeforeRouteUpdate((to, from) => {
  const newId = to.params.classId as string | undefined;
  const oldId = from.params.classId as string | undefined;
  if (newId && newId !== oldId) {
    expandedClassId.value = newId;
    classesStore.fetchExams(newId);
  }
});
</script>
