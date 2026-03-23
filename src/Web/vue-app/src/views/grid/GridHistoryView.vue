<template>
  <div class="content-grid">
    <div class="content-grid__header">
      <h1>{{ t("grids.title") }}</h1>
    </div>

    <Card>
      <DataTable
        :headers="headers"
        :items="tableItems"
        :is-loading="loading"
      />
    </Card>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { useI18n } from "vue3-i18n";
import type { Header } from "vue3-easy-data-table";
import Card from "@/components/layouts/items/Card.vue";
import DataTable from "@/components/layouts/items/DataTable.vue";

const { t } = useI18n();

interface GridItem {
  id: string;
  classId: string;
  name: string;
  courseCode: string;
  sessionName: string;
  isPublic: boolean;
  createdAt: string;
}

const loading = ref(true);
const grids = ref<GridItem[]>([]);

onMounted(async () => {
  try {
    const res = await fetch("/api/grids");
    if (res.ok) {
      grids.value = await res.json();
    }
  } finally {
    loading.value = false;
  }
});

const headers: Header[] = [
  { text: t("grids.columns.name"), value: "name", sortable: true },
  { text: t("grids.columns.course"), value: "courseCode", sortable: true },
  { text: t("grids.columns.session"), value: "sessionName", sortable: true },
  { text: t("grids.columns.status"), value: "status" },
  { text: t("grids.columns.date"), value: "date", sortable: true },
  { text: t("grids.columns.actions"), value: "actions", width: 100 },
];

const tableItems = computed(() =>
  grids.value.map((g) => ({
    id: g.id,
    name: g.name,
    courseCode: g.courseCode,
    sessionName: g.sessionName,
    status: g.isPublic ? `${t("grids.statusPublic")}` : `${t("grids.statusPrivate")}`,
    date: new Date(g.createdAt).toLocaleDateString("fr-CA"),
    actions: {
      view: {
        name: "classes.examDetail",
        params: { classId: g.classId, examId: g.id },
      },
    },
  }))
);
</script>
