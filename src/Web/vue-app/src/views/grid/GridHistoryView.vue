<template>
  <div class="content-grid">
    <div class="content-grid__header">
      <h1>{{ t("grids.title") }}</h1>
    </div>

    <!-- Filters bar -->
    <Card>
      <div class="grids-filters">
        <div class="grids-filters__field">
          <label>{{ t("grids.filters.session") }}</label>
          <select v-model="selectedSession">
            <option value="">{{ t("grids.filters.allSessions") }}</option>
            <option v-for="s in availableSessions" :key="s" :value="s">{{ s }}</option>
          </select>
        </div>

        <div class="grids-filters__field">
          <label>{{ t("grids.filters.course") }}</label>
          <select v-model="selectedCourse">
            <option value="">{{ t("grids.filters.allCourses") }}</option>
            <option v-for="c in availableCourses" :key="c" :value="c">{{ c }}</option>
          </select>
        </div>

        <div class="grids-filters__field">
          <label>{{ t("grids.filters.professor") }}</label>
          <select v-model="selectedProfessor">
            <option value="">{{ t("grids.filters.allProfessors") }}</option>
            <option v-for="p in availableProfessors" :key="p" :value="p">{{ p }}</option>
          </select>
        </div>

        <div class="grids-filters__field">
          <label>{{ t("grids.filters.group") }}</label>
          <select v-model="selectedGroup">
            <option value="">{{ t("grids.filters.allGroups") }}</option>
            <option v-for="g in availableGroups" :key="g" :value="g">{{ g }}</option>
          </select>
        </div>
      </div>
    </Card>

    <!-- Grids table -->
    <Card>
      <DataTable
        :headers="headers"
        :items="tableItems"
        :is-loading="loading"
      >
        <template #item-status="item">
          <div class="status-cell">
            <span class="tag" :class="{ 'tag--public': item.rawIsPublic, 'tag--private': !item.rawIsPublic }">
              {{ item.status }}
            </span>
            <button
              v-if="item.rawIsOwner"
              type="button"
              class="btn-toggle"
              @click.stop="onToggleVisibility(item)"
            >
              {{ item.rawIsPublic ? t("grids.togglePrivate") : t("grids.togglePublic") }}
            </button>
          </div>
        </template>
      </DataTable>
      <p v-if="!loading && tableItems.length === 0" class="grids-empty">
        {{ t("grids.empty") }}
      </p>
    </Card>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from "vue";
import { useI18n } from "vue3-i18n";
import type { Header } from "vue3-easy-data-table";
import Card from "@/components/layouts/items/Card.vue";
import DataTable from "@/components/layouts/items/DataTable.vue";
import { useGridService } from "@/inversify.config";
import type { GridItem } from "@/services/gridService";

const { t } = useI18n();
const gridService = useGridService();

const loading = ref(true);
const grids = ref<GridItem[]>([]);

// Filters use session/course NAMES from the grid data (includes own + public grids from others)
const selectedSession = ref("");
const selectedCourse = ref("");
const selectedProfessor = ref("");
const selectedGroup = ref("");

onMounted(async () => {
  try {
    grids.value = await gridService.getAllGrids();
  } finally {
    loading.value = false;
  }
});

// Reset course filter when session changes
watch(selectedSession, () => {
  selectedCourse.value = "";
});

// Sessions from ALL visible grids (own + public from others)
const availableSessions = computed(() => {
  const sessions = new Set(grids.value.map((g) => g.sessionName));
  return [...sessions].sort();
});

// Courses cascaded from selected session
const availableCourses = computed(() => {
  let source = grids.value;
  if (selectedSession.value) {
    source = source.filter((g) => g.sessionName === selectedSession.value);
  }
  const courses = new Set(source.map((g) => g.courseCode));
  return [...courses].sort();
});

// Professors from ALL visible grids
const availableProfessors = computed(() => {
  const professors = new Set(grids.value.map((g) => g.creatorEmail));
  return [...professors].sort();
});

// Groups cascaded from current filters
const availableGroups = computed(() => {
  let source = grids.value;
  if (selectedSession.value) {
    source = source.filter((g) => g.sessionName === selectedSession.value);
  }
  if (selectedCourse.value) {
    source = source.filter((g) => g.courseCode === selectedCourse.value);
  }
  const groups = new Set(source.flatMap((g) => g.groupNames ?? []));
  return [...groups].sort();
});

// Filtered grids
const filteredGrids = computed(() => {
  let result = grids.value;
  if (selectedSession.value) {
    result = result.filter((g) => g.sessionName === selectedSession.value);
  }
  if (selectedCourse.value) {
    result = result.filter((g) => g.courseCode === selectedCourse.value);
  }
  if (selectedProfessor.value) {
    result = result.filter((g) => g.creatorEmail === selectedProfessor.value);
  }
  if (selectedGroup.value) {
    result = result.filter((g) => (g.groupNames ?? []).includes(selectedGroup.value));
  }
  return result;
});

const headers: Header[] = [
  { text: t("grids.columns.name"), value: "name", sortable: true },
  { text: t("grids.columns.course"), value: "courseCode", sortable: true },
  { text: t("grids.columns.session"), value: "sessionName", sortable: true },
  { text: t("grids.columns.creator"), value: "creatorEmail", sortable: true },
  { text: t("grids.columns.status"), value: "status" },
  { text: t("grids.columns.date"), value: "date", sortable: true },
];

const tableItems = computed(() =>
  filteredGrids.value.map((g) => ({
    id: g.id,
    name: g.name,
    courseCode: g.courseCode,
    sessionName: g.sessionName,
    creatorEmail: g.creatorEmail,
    status: g.isPublic ? t("grids.statusPublic") : t("grids.statusPrivate"),
    rawIsPublic: g.isPublic,
    rawIsOwner: g.isOwner,
    date: new Date(g.createdAt).toLocaleDateString("fr-CA"),
    actions: {
      view: {
        name: "classes.examDetailDirect",
        params: { classId: g.classId, examId: g.id },
        query: g.isOwner ? { examName: g.name } : { readOnly: "1", examName: g.name, courseName: g.courseCode, professor: g.creatorEmail },
      },
    },
  }))
);

async function onToggleVisibility(item: { id: string; rawIsPublic: boolean }) {
  const newValue = !item.rawIsPublic;
  const success = await gridService.toggleVisibility(item.id, newValue);
  if (success) {
    const grid = grids.value.find((g) => g.id === item.id);
    if (grid) {
      grid.isPublic = newValue;
    }
  }
}
</script>

<style scoped lang="scss">
.grids-filters {
  display: flex;
  flex-wrap: wrap;
  gap: 16px;
  padding: 8px 0;

  &__field {
    display: flex;
    flex-direction: column;
    gap: 4px;
    min-width: 180px;
    flex: 1;

    label {
      font-size: 0.85rem;
      font-weight: 600;
      color: #555;
    }

    select {
      padding: 6px 10px;
      border: 1px solid #ccc;
      border-radius: 4px;
      font-size: 0.9rem;
      background: #fff;
    }

  }
}

.status-cell {
  display: flex;
  align-items: center;
  gap: 8px;
}

.tag {
  display: inline-block;
  padding: 2px 10px;
  border-radius: 12px;
  font-size: 0.8rem;
  font-weight: 600;

  &--public {
    background: #d4edda;
    color: #155724;
  }

  &--private {
    background: #f8d7da;
    color: #721c24;
  }
}

.btn-toggle {
  padding: 2px 8px;
  font-size: 0.75rem;
  border: 1px solid #ccc;
  border-radius: 4px;
  background: #f8f9fa;
  cursor: pointer;
  white-space: nowrap;

  &:hover {
    background: #e2e6ea;
  }
}

.grids-empty {
  text-align: center;
  padding: 24px;
  color: #888;
}
</style>

<style lang="scss">
[data-theme="dark"] {
  .grids-filters__field {
    label {
      color: rgba(255, 255, 255, 0.5);
    }

    select {
      background-color: #2d2d44;
      border-color: rgba(255, 255, 255, 0.1);
      color: rgba(255, 255, 255, 0.9);

      option {
        background-color: #2d2d44;
        color: rgba(255, 255, 255, 0.9);
      }
    }
  }

  .btn-toggle {
    background-color: rgba(255, 255, 255, 0.06);
    border-color: rgba(255, 255, 255, 0.12);
    color: rgba(255, 255, 255, 0.8);

    &:hover {
      background-color: rgba(255, 255, 255, 0.1);
    }
  }

  .grids-empty {
    color: rgba(255, 255, 255, 0.35);
  }

  .tag--public {
    background-color: rgba(16, 185, 129, 0.15);
    color: #6ee7b7;
  }

  .tag--private {
    background-color: rgba(239, 68, 68, 0.15);
    color: #fca5a5;
  }
}
</style>
