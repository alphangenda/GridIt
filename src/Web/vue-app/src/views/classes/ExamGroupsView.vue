<template>
  <div class="content-grid">
    <div class="content-grid__header">
      <div class="content-grid__back">
        <router-link
          :to="{ name: 'classes.detail', params: { classId } }"
          class="content-grid__back-link"
          aria-label="Retour"
        >
          &lt;
        </router-link>
        <h1>{{ exam?.name ?? t("navigation.exam") }}</h1>
      </div>
      <div class="content-grid__actions">
        <button type="button" class="btn" @click="showImportPopup = true">
          {{ t("navigation.importGroup") }}
        </button>
      </div>
    </div>

    <Card>
      <div v-if="loading" class="groups-empty">{{ t("global.loading") }}</div>
      <div v-else-if="groups.length === 0" class="groups-empty">
        {{ t("navigation.noGroups") }}
      </div>
      <ul v-else class="groups-list">
        <li v-for="group in groups" :key="group.id" class="groups-list__item">
          <router-link
            :to="{ name: 'classes.examDetail', params: { classId, examId }, query: { groupId: group.id } }"
            class="groups-list__link"
          >
            <span class="groups-list__icon">&#128101;</span>
            <div class="groups-list__info">
              <span class="groups-list__name">{{ group.name }}</span>
              <span class="groups-list__count">{{ group.studentCount }} {{ t("navigation.students") }}</span>
            </div>
            <span class="groups-list__arrow">&#8250;</span>
          </router-link>
          <button
            type="button"
            class="groups-list__delete"
            :title="t('navigation.deleteGroupConfirm')"
            @click="onDeleteGroup(group)"
          >
            ✕
          </button>
        </li>
      </ul>
    </Card>

    <ImportGroupPopup
      v-if="showImportPopup"
      :exam-id="examId"
      :class-id="classId"
      @close="onPopupClose"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRoute } from "vue-router";
import { useI18n } from "vue3-i18n";
import { useClassesStore } from "@/stores/classesStore";
import Card from "@/components/layouts/items/Card.vue";
import ImportGroupPopup from "@/components/popups/ImportGroupPopup.vue";

const { t } = useI18n();
const route = useRoute();
const classesStore = useClassesStore();

const classId = computed(() => route.params.classId as string);
const examId = computed(() => route.params.examId as string);

const groups = ref<{ id: string; name: string; studentCount: number }[]>([]);
const loading = ref(true);
const showImportPopup = ref(false);

const exam = computed(() =>
  classesStore.getExamsForClass(classId.value)?.find((e) => e.id === examId.value)
);

async function fetchGroups() {
  loading.value = true;
  try {
    const res = await fetch(`/api/exams/${examId.value}/groups`);
    if (!res.ok) {
      console.error("fetchGroups error:", res.status, await res.text());
      return;
    }
    const data = await res.json();
    groups.value = data.map((g: any) => ({
      id: String(g.id),
      name: String(g.name),
      studentCount: Number(g.studentCount ?? 0),
    }));
  } catch (e) {
    console.error("fetchGroups exception:", e);
  } finally {
    loading.value = false;
  }
}

async function onPopupClose() {
  showImportPopup.value = false;
  await fetchGroups();
}

async function onDeleteGroup(group: { id: string; name: string }) {
  if (!window.confirm(t("navigation.deleteGroupConfirm"))) return;
  await fetch(`/api/exams/${examId.value}/groups/${group.id}`, { method: "DELETE" });
  await fetchGroups();
}

onMounted(async () => {
  await classesStore.fetchExams(classId.value);
  await fetchGroups();
});
</script>

<style scoped>
.groups-empty {
  padding: 32px;
  text-align: center;
  opacity: 0.6;
  font-weight: 600;
}

.groups-list {
  list-style: none;
  margin: 0;
  padding: 0;
}

.groups-list__item {
  display: flex;
  align-items: center;
  border-bottom: 1px solid rgba(0, 0, 0, 0.08);
}

.groups-list__item:last-child {
  border-bottom: none;
}

.groups-list__link {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 16px 20px;
  text-decoration: none;
  color: inherit;
  transition: background-color 0.15s;
}

.groups-list__link:hover {
  background-color: rgba(0, 0, 0, 0.04);
}

.groups-list__icon {
  font-size: 22px;
}

.groups-list__info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.groups-list__name {
  font-weight: 700;
  font-size: 15px;
}

.groups-list__count {
  font-size: 13px;
  opacity: 0.55;
  font-weight: 500;
}

.groups-list__arrow {
  font-size: 22px;
  opacity: 0.35;
}

.groups-list__delete {
  margin-right: 14px;
  width: 32px;
  height: 32px;
  border-radius: 8px;
  border: none;
  background: rgba(0, 0, 0, 0.08);
  cursor: pointer;
  font-size: 13px;
  font-weight: 900;
  opacity: 0.55;
  transition: opacity 0.15s, background-color 0.15s;
}

.groups-list__delete:hover {
  opacity: 1;
  background-color: rgba(220, 50, 50, 0.15);
}
</style>
