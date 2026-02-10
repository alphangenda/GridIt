<template>
  <div class="exam-detail">
    <h1 class="exam-detail__title">{{ exam?.name ?? t("navigation.exam") }}</h1>
    <p class="exam-detail__hint">{{ t("navigation.examDetailPlaceholder") }}</p>
  </div>
</template>
<script setup lang="ts">
import { computed } from "vue";
import { useRoute } from "vue-router";
import { useI18n } from "vue3-i18n";
import { useClassesStore } from "@/stores/classesStore";

const { t } = useI18n();
const route = useRoute();
const classesStore = useClassesStore();

const exam = computed(() => {
  const classId = route.params.classId as string;
  const examId = route.params.examId as string;
  return classesStore.getExamsForClass(classId).find((e) => e.id === examId);
});
</script>
