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
    </template>
    <p v-else class="side-nav__empty">{{ t("navigation.selectClass") }}</p>
  </nav>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { useRoute } from "vue-router";
import { useI18n } from "vue3-i18n";
import { useClassesStore } from "@/stores/classesStore";

const { t } = useI18n();
const route = useRoute();
const classesStore = useClassesStore();

const classId = computed(() => route.params.classId as string | undefined);
const exams = computed(() =>
  classId.value ? classesStore.getExamsForClass(classId.value) : []
);

</script>
