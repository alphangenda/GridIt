<template>
  <div class="content-grid">
    <div class="content-grid__header">
      <h1>{{ t("navigation.sessions") }}</h1>
      <div class="content-grid__actions">
        <button type="button" class="btn" @click="onAddSession">
          {{ t("navigation.addSession") }}
        </button>
      </div>
    </div>
    <Card>
      <DataTable
        :headers="sessionHeaders"
        :items="sessionItems"
        @delete="onDeleteSession"
      >
        <template #item-className="item">
          <div class="class-links">
            <template v-if="item.classLinks && item.classLinks.length > 0">
              <template v-for="(classLink, index) in item.classLinks" :key="classLink.id">
                <RouterLink
                  :to="classLink.route"
                  class="class-link"
                >
                  {{ classLink.name }}
                </RouterLink>
                <span v-if="typeof index === 'number' && typeof item.classLinks.length === 'number' && index < item.classLinks.length - 1" class="class-link-separator">, </span>
              </template>
            </template>
            <span v-else>{{ t("navigation.noClass") }}</span>
          </div>
        </template>
      </DataTable>
    </Card>

    <CreateSessionPopup v-if="showCreatePopup" @close="showCreatePopup = false" />
  </div>
</template>
<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { useI18n } from "vue3-i18n";
import { useSessionsStore } from "@/stores/sessionsStore";
import { useClassesStore } from "@/stores/classesStore";
import Card from "@/components/layouts/items/Card.vue";
import DataTable from "@/components/layouts/items/DataTable.vue";
import CreateSessionPopup from "@/components/popups/CreateSessionPopup.vue";
import type { Header } from "vue3-easy-data-table";

const { t } = useI18n();
const router = useRouter();
const sessionsStore = useSessionsStore();
const classesStore = useClassesStore();
const showCreatePopup = ref(false);

onMounted(() => {
  sessionsStore.fetchSessions();
  classesStore.fetchClasses();
});

const sessionHeaders: Header[] = [
  { text: t("navigation.sessionName"), value: "name", sortable: true },
  { text: t("navigation.className"), value: "className", sortable: true },
  { text: t("global.table.actions"), value: "actions", width: 120 },
];

const sessionItems = computed(() =>
  sessionsStore.getSessions.map((s) => {
    const classLinks = s.classIds && s.classIds.length > 0
      ? s.classIds.map(classId => {
          const classItem = classesStore.getClasses.find(c => c.id === classId);
          return {
            id: classId,
            name: classItem?.name || classId,
            route: {
              name: "classes.detail",
              params: { classId: classId }
            }
          };
        })
      : [];

    return {
      id: s.id,
      name: s.name,
      className: classLinks.length > 0
        ? classLinks.map(cl => cl.name).join(", ")
        : t("navigation.noClass"),
      classLinks: classLinks,
      actions: {
        view: { name: "sessions.detail", params: { sessionId: s.id } },
        delete: true,
      },
    };
  })
);

function onAddSession() {
  showCreatePopup.value = true;
}

function onDeleteSession(item: { id: string }) {
  if (window.confirm(t("navigation.deleteSessionConfirm"))) {
    sessionsStore.deleteSession(item.id);
  }
}
</script>

<style scoped lang="scss">
.class-links {
  display: inline-flex;
  flex-wrap: wrap;
  gap: 4px;
}

.class-link {
  color: #528965;
  text-decoration: none;
  font-weight: 500;
  transition: color 0.15s ease;

  &:hover {
    color: #3d6b4d;
    text-decoration: underline;
  }
}

.class-link-separator {
  color: #666;
}
</style>
