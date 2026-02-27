<template>
  <div class="content-grid">
    <div class="content-grid__header">
      <div class="content-grid__back">
        <router-link :to="{ name: 'classes.index' }" class="content-grid__back-link" aria-label="Retour">
          &lt;
        </router-link>
        <h1>{{ classItem?.name ?? t("navigation.classes") }}</h1>
      </div>
      <div class="content-grid__actions">
        <button type="button" class="btn" @click="onAddExam">
          {{ t("navigation.addExam") }}
        </button>
      </div>
    </div>
    <Card>
      <DataTable
        :headers="examHeaders"
        :items="examItems"
        @delete="onDeleteExam"
      />
    </Card>

    <CreateExamPopup v-if="showCreatePopup" :class-id="classId" @close="showCreatePopup = false" />
  </div>
</template>
<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useI18n } from "vue3-i18n";
import { useClassesStore } from "@/stores/classesStore";
import Card from "@/components/layouts/items/Card.vue";
import DataTable from "@/components/layouts/items/DataTable.vue";
import CreateExamPopup from "@/components/popups/CreateExamPopup.vue";
import type { Header } from "vue3-easy-data-table";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const classesStore = useClassesStore();
const showCreatePopup = ref(false);

const classId = computed(() => route.params.classId as string);

onMounted(() => {
  classesStore.fetchExams(classId.value);
});

watch(classId, (newId) => {
  if (newId) classesStore.fetchExams(newId);
});

const classItem = computed(() =>
  classesStore.getClasses.find((c) => c.id === classId.value)
);

const examHeaders: Header[] = [
  { text: t("navigation.examName"), value: "name", sortable: true },
  { text: t("global.table.actions"), value: "actions", width: 160 },
];

const examItems = computed(() =>
  classesStore.getExamsForClass(classId.value).map((e) => ({
    id: e.id,
    name: e.name,
    actions: {
      view: {
        name: "classes.examDetail",
        params: { classId: classId.value, examId: e.id },
      },
      evaluate: {
        name: "evaluation",
        params: { classId: classId.value, examId: e.id },
      },
      delete: true,
    },
  }))
);

function onAddExam() {
  showCreatePopup.value = true;
}

function onDeleteExam(item: { id: string }) {
  if (window.confirm(t("navigation.deleteExamConfirm"))) {
    classesStore.deleteExam(classId.value, item.id);
  }
}
</script>
