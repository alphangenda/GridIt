<template>
  <nav class="side-nav side-nav--exams">
    <h2 class="side-nav__title">{{ t("navigation.exams") }}</h2>
    <template v-if="classId">
      <ul class="side-nav__list">
        <li v-for="exam in exams" :key="exam.id">
          <RouterLink
            v-if="classId && exam.id"
            :to="{ name: 'classes.examDetail', params: { classId, examId: exam.id } }"
            class="side-nav__link"
            active-class="side-nav__link--active"
          >
            {{ exam.name }}
          </RouterLink>
        </li>
      </ul>
      <div class="side-nav__footer">
        <button type="button" class="side-nav__add-btn" @click="onAddExam">
          {{ t("navigation.addExam") }}
        </button>
      </div>
    </template>
    <p v-else class="side-nav__empty">{{ t("navigation.selectClass") }}</p>

    <CreateExamPopup v-if="showCreatePopup && classId" :class-id="classId" @close="showCreatePopup = false" />
  </nav>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { useRoute } from "vue-router";
import { useI18n } from "vue3-i18n";
import { useClassesStore } from "@/stores/classesStore";
import CreateExamPopup from "@/components/popups/CreateExamPopup.vue";

const { t } = useI18n();
const route = useRoute();
const classesStore = useClassesStore();
const showCreatePopup = ref(false);

const classId = computed(() => route.params.classId as string | undefined);
const exams = computed(() =>
  classId.value ? classesStore.getExamsForClass(classId.value) : []
);

function onAddExam() {
  if (!classId.value) return;
  showCreatePopup.value = true;
}
</script>
