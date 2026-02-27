<template>
  <div class="content-grid">
    <div class="content-grid__header">
      <h1>{{ session?.name ?? t("navigation.sessions") }}</h1>
      <div class="content-grid__actions">
        <button type="button" class="btn" @click="onAddClasses">
          {{ t("navigation.addClassToSession") }}
        </button>
      </div>
    </div>
    <Card>
      <div class="session-detail">
        <h2 class="session-detail__subtitle">
          {{ t("navigation.classes") }}
        </h2>
        <p v-if="!classLinks.length" class="session-detail__empty">
          {{ t("navigation.noClass") }}
        </p>
        <ul v-else class="session-detail__class-list">
          <li
            v-for="cl in classLinks"
            :key="cl.id"
            class="session-detail__class-item"
          >
            <RouterLink :to="cl.route" class="session-detail__class-link">
              {{ cl.name }}
            </RouterLink>
            <button
              type="button"
              class="session-detail__remove-btn"
              @click="onRemoveClass(cl.id)"
            >
              {{ t("global.delete") }}
            </button>
          </li>
        </ul>
      </div>
    </Card>

    <AddClassToSessionPopup
      v-if="showAddClassesPopup && session?.id"
      :session-id="session.id"
      :current-class-ids="session.classIds || []"
      @close="showAddClassesPopup = false"
    />
  </div>
</template>
<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRoute } from "vue-router";
import { useI18n } from "vue3-i18n";
import { useSessionsStore } from "@/stores/sessionsStore";
import { useClassesStore } from "@/stores/classesStore";
import Card from "@/components/layouts/items/Card.vue";
import AddClassToSessionPopup from "@/components/popups/AddClassToSessionPopup.vue";

const { t } = useI18n();
const route = useRoute();
const sessionsStore = useSessionsStore();
const classesStore = useClassesStore();

const showAddClassesPopup = ref(false);

const sessionId = computed(() => route.params.sessionId as string);

onMounted(async () => {
  if (!sessionsStore.getSessions.length) {
    await sessionsStore.fetchSessions();
  }
  if (!classesStore.getClasses.length) {
    await classesStore.fetchClasses();
  }
});

const session = computed(() =>
  sessionsStore.getSessions.find((s) => s.id === sessionId.value)
);

const classLinks = computed(() => {
  const s = session.value;
  if (!s || !s.classIds || !s.classIds.length) return [];
  return s.classIds.map((classId) => {
    const classItem = classesStore.getClasses.find((c) => c.id === classId);
    return {
      id: classId,
      name: classItem?.name || classId,
      route: {
        name: "classes.detail",
        params: { classId },
      },
    };
  });
});

function onAddClasses() {
  if (!session.value?.id) return;
  showAddClassesPopup.value = true;
}

async function onRemoveClass(classId: string) {
  if (!session.value?.id) return;
  if (!window.confirm(t("navigation.removeClassFromSessionConfirm"))) return;

  const currentIds = session.value.classIds || [];
  const newIds = currentIds.filter((id) => id !== classId);

  await sessionsStore.updateSession(
    session.value.id,
    session.value.name || "",
    newIds
  );
  await sessionsStore.fetchSessions();
}
</script>

<style scoped lang="scss">
.session-detail {
  padding: 1rem 1.25rem 1.25rem;
}

.session-detail__subtitle {
  font-size: 0.95rem;
  font-weight: 600;
  margin-bottom: 0.75rem;
}

.session-detail__empty {
  opacity: 0.8;
  font-size: 0.9rem;
}

.session-detail__class-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.session-detail__class-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}

.session-detail__class-link {
  color: #528965;
  text-decoration: none;
  font-weight: 500;
}

.session-detail__class-link:hover {
  text-decoration: underline;
}

.session-detail__remove-btn {
  padding: 0.15rem 0.6rem;
  border-radius: 4px;
  border: 1px solid rgba(220, 38, 38, 1);
  background-color: rgba(220, 38, 38, 1);
  color: #f9fafb;
  font-size: 0.75rem;
  cursor: pointer;
  white-space: nowrap;
}

.session-detail__remove-btn:hover {
  background-color: rgba(185, 28, 28, 1);
}
</style>

